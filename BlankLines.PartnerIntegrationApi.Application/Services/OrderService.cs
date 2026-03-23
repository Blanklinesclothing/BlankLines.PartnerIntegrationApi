using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Responses;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using BlankLines.PartnerIntegrationApi.Domain.Enums;
using BlankLines.PartnerIntegrationApi.Domain.Exceptions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BlankLines.PartnerIntegrationApi.Application.Services;

public class OrderService(
    IApplicationDbContext context,
    IShopifyApiService shopifyService,
    IStorageService storageService,
    ILogger<OrderService> logger) : IOrderService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IShopifyApiService _shopifyService = shopifyService;
    private readonly IStorageService _storageService = storageService;
    private readonly ILogger<OrderService> _logger = logger;

    private static readonly HashSet<string> AllowedImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/jpeg", "image/png", "image/webp", "image/gif"
    };

    public async Task<string> CreateOrderAsync(Guid partnerId, CreateOrderRequest request)
    {
        if (request.DesignFile != null && !AllowedImageTypes.Contains(request.DesignFile.ContentType))
        {
            throw new InvalidOperationException("Design file must be an image (JPEG, PNG, WebP, or GIF)");
        }

        var existingOrder = await _context.Orders
            .FirstOrDefaultAsync(o => o.PartnerId == partnerId && o.PartnerOrderId == request.PartnerOrderId);

        if (existingOrder != null)
        {
            throw new InvalidOperationException($"Order '{request.PartnerOrderId}' already exists");
        }

        var partnerProducts = await _context.PartnerProducts
            .Where(pp => pp.PartnerId == partnerId)
            .ToListAsync();

        var orderItems = new List<OrderItem>();

        foreach (var item in request.Items)
        {
            var partnerProduct = partnerProducts
                .FirstOrDefault(pp => pp.PartnerSku == item.PartnerSku) 
                ?? throw new InvalidOperationException($"Partner SKU '{item.PartnerSku}' not found");
            
            orderItems.Add(new OrderItem
            {
                Id = Guid.NewGuid(),
                PartnerSku = item.PartnerSku,
                BaseSku = partnerProduct.BaseSku,
                DesignReference = partnerProduct.DesignReference,
                ShopifyVariantId = partnerProduct.ShopifyVariantId,
                Quantity = item.Quantity
            });
        }

        var order = new Order
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            PartnerOrderId = request.PartnerOrderId,
            Status = OrderStatus.Received,
            DeliveryMethod = request.DeliveryMethod,
            CreatedAt = DateTime.UtcNow,
            CustomerFirstName = request.Customer.FirstName,
            CustomerLastName = request.Customer.LastName,
            CustomerEmail = request.Customer.Email,
            CustomerPhone = request.Customer.Phone,
            ShippingAddress1 = request.ShippingAddress?.Address1,
            ShippingAddress2 = request.ShippingAddress?.Address2,
            ShippingCity = request.ShippingAddress?.City,
            ShippingProvince = request.ShippingAddress?.Province,
            ShippingCountry = request.ShippingAddress?.Country,
            ShippingZip = request.ShippingAddress?.Zip,
            ShippingPhone = request.ShippingAddress?.Phone,
            Items = orderItems
        };

        foreach (var item in orderItems)
        {
            item.OrderId = order.Id;
        }

        // Step 1 — persist the order immediately so it is never lost
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Order {PartnerOrderId} created for partner {PartnerId}", order.PartnerOrderId, partnerId);

        // Step 2 — upload design file if provided
        if (request.DesignFile != null)
        {
            try
            {
                order.DesignFileUrl = await _storageService.UploadDesignAsync(
                    request.PartnerOrderId,
                    request.DesignFile.Content,
                    request.DesignFile.ContentType,
                    request.DesignFile.Extension);

                await _context.SaveChangesAsync();
                _logger.LogInformation("Design file uploaded for order {PartnerOrderId}", order.PartnerOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Design file upload failed for order {PartnerOrderId} — order saved, Shopify submission will proceed without design URL", order.PartnerOrderId);
            }
        }

        // Step 3 — submit to Shopify and update the order record
        try
        {
            var shopifyOrderId = await _shopifyService.CreateOrderAsync(order);
            order.ShopifyOrderId = shopifyOrderId;
            order.Status = OrderStatus.Processing;
            await _context.SaveChangesAsync();
            _logger.LogInformation("Order {PartnerOrderId} submitted to Shopify as {ShopifyOrderId}", order.PartnerOrderId, shopifyOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Shopify submission failed for order {PartnerOrderId} — order remains in Received status for retry", order.PartnerOrderId);
            throw new UpstreamServiceException("Shopify", $"Failed to submit order '{request.PartnerOrderId}' to Shopify. The order has been saved and can be retried.", ex);
        }

        return order.Id.ToString();
    }

    public async Task<OrderResponse> GetOrderAsync(Guid partnerId, string partnerOrderId)
    {
        var order = await _context.Orders
            .Include(o => o.Items)
            .FirstOrDefaultAsync(o => o.PartnerId == partnerId && o.PartnerOrderId == partnerOrderId)
            ?? throw new KeyNotFoundException($"Order '{partnerOrderId}' not found");

        return new OrderResponse
        {
            PartnerOrderId = order.PartnerOrderId,
            ShopifyOrderId = order.ShopifyOrderId,
            Status = order.Status,
            DeliveryMethod = order.DeliveryMethod,
            CreatedAt = order.CreatedAt,
            Customer = new CustomerDto
            {
                FirstName = order.CustomerFirstName,
                LastName = order.CustomerLastName,
                Email = order.CustomerEmail,
                Phone = order.CustomerPhone
            },
            ShippingAddress = order.ShippingAddress1 != null ? new ShippingAddressDto
            {
                FirstName = order.CustomerFirstName,
                LastName = order.CustomerLastName,
                Address1 = order.ShippingAddress1,
                Address2 = order.ShippingAddress2,
                City = order.ShippingCity!,
                Province = order.ShippingProvince,
                Country = order.ShippingCountry!,
                Zip = order.ShippingZip!,
                Phone = order.ShippingPhone
            } : null,
            DesignFileUrl = order.DesignFileUrl,
            Items = order.Items.Select(i => new OrderItemResponseDto
            {
                PartnerSku = i.PartnerSku,
                Quantity = i.Quantity
            }).ToList()
        };
    }

    public async Task CancelOrderAsync(Guid partnerId, CancelOrderRequest request)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PartnerId == partnerId && o.PartnerOrderId == request.PartnerOrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order '{request.PartnerOrderId}' not found");
        }

        if ((DateTime.UtcNow - order.CreatedAt).TotalHours > 24)
        {
            throw new InvalidOperationException("Order cannot be cancelled after 24 hours");
        }

        if (order.ShopifyOrderId != null)
        {
            if (!long.TryParse(order.ShopifyOrderId, out var shopifyOrderId))
            {
                throw new InvalidOperationException($"Order '{request.PartnerOrderId}' has an invalid Shopify order ID");
            }

            try
            {
                await _shopifyService.CancelOrderAsync(shopifyOrderId);
                _logger.LogInformation("Order {PartnerOrderId} cancelled in Shopify (ShopifyOrderId: {ShopifyOrderId})", request.PartnerOrderId, order.ShopifyOrderId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cancel order {PartnerOrderId} in Shopify", request.PartnerOrderId);
                throw new UpstreamServiceException("Shopify", $"Failed to cancel order '{request.PartnerOrderId}' in Shopify.", ex);
            }
        }

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
        _logger.LogInformation("Order {PartnerOrderId} cancelled by partner {PartnerId}", request.PartnerOrderId, partnerId);
    }
}