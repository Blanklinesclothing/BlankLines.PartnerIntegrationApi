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

    public async Task<string> CreateOrderAsync(Guid partnerId, CreateOrderRequest request)
    {
        ValidateCreateOrderRequest(request);

        await EnsureOrderIsUniqueAsync(partnerId, request.PartnerOrderId);

        var partnerProducts = await _context.PartnerProducts
            .Where(pp => pp.PartnerId == partnerId)
            .ToListAsync();

        var orderItems = await BuildOrderItemsAsync(request.Items, partnerProducts);

        var order = BuildOrder(partnerId, request, orderItems);

        await PersistOrderAsync(order);

        await UploadDesignFileAsync(order, request.DesignFile);

        await SubmitToShopifyAsync(order, request.PartnerOrderId);

        return order.Id.ToString();
    }

    private async Task EnsureOrderIsUniqueAsync(Guid partnerId, string partnerOrderId)
    {
        var exists = await _context.Orders
            .AnyAsync(o => o.PartnerId == partnerId && o.PartnerOrderId == partnerOrderId);

        if (exists)
        {
            throw new InvalidOperationException($"Order '{partnerOrderId}' already exists");
        }
    }

    private async Task<List<OrderItem>> BuildOrderItemsAsync(
        List<OrderItemDto> requestItems,
        List<PartnerProduct> partnerProducts)
    {
        var orderItems = new List<OrderItem>();

        foreach (var item in requestItems)
        {
            var partnerProduct = partnerProducts
                .FirstOrDefault(pp => pp.PartnerSku == item.PartnerSku)
                ?? throw new InvalidOperationException($"Partner SKU '{item.PartnerSku}' not found");

            if (partnerProduct.ShopifyVariantId.HasValue)
            {
                await CheckInventoryAsync(partnerProduct.ShopifyVariantId.Value, item.PartnerSku, item.Quantity);
            }

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

        return orderItems;
    }

    private async Task CheckInventoryAsync(long variantId, string partnerSku, int requestedQuantity)
    {
        int available;
        try
        {
            available = await _shopifyService.GetInventoryQuantityAsync(variantId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Inventory check failed for variant {VariantId} (PartnerSku: {PartnerSku})", variantId, partnerSku);
            throw new UpstreamServiceException("Shopify", $"Could not verify inventory for '{partnerSku}'. Please try again.", ex);
        }

        if (available < requestedQuantity)
        {
            throw new InvalidOperationException(
                $"Insufficient stock for '{partnerSku}': {available} available, {requestedQuantity} requested.");
        }
    }

    private static Order BuildOrder(Guid partnerId, CreateOrderRequest request, List<OrderItem> orderItems)
    {
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

        return order;
    }

    private async Task PersistOrderAsync(Order order)
    {
        _context.Orders.Add(order);
        await _context.SaveChangesAsync();
        _logger.LogInformation("Order {PartnerOrderId} created for partner {PartnerId}", order.PartnerOrderId, order.PartnerId);
    }

    private async Task UploadDesignFileAsync(Order order, DesignFileDto? designFile)
    {
        if (designFile == null)
        {
            return;
        }

        try
        {
            order.DesignFileUrl = await _storageService.UploadDesignAsync(
                order.PartnerOrderId,
                designFile.Content,
                designFile.ContentType,
                designFile.Extension);

            await _context.SaveChangesAsync();
            _logger.LogInformation("Design file uploaded for order {PartnerOrderId}", order.PartnerOrderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Design file upload failed for order {PartnerOrderId} - order saved, Shopify submission will proceed without design URL", order.PartnerOrderId);
        }
    }

    private async Task SubmitToShopifyAsync(Order order, string partnerOrderId)
    {
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
            _logger.LogError(ex, "Shopify submission failed for order {PartnerOrderId} - order remains in Received status for retry", order.PartnerOrderId);
            throw new UpstreamServiceException("Shopify", $"Failed to submit order '{partnerOrderId}' to Shopify. The order has been saved and can be retried.", ex);
        }
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

    private static void ValidateCreateOrderRequest(CreateOrderRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PartnerOrderId))
        {
            throw new InvalidOperationException("PartnerOrderId is required.");
        }

        ValidateCustomer(request.Customer);

        if (request.Items == null || request.Items.Count == 0)
        {
            throw new InvalidOperationException("Order must contain at least one item.");
        }

        foreach (var item in request.Items)
        {
            if (string.IsNullOrWhiteSpace(item.PartnerSku))
            {
                throw new InvalidOperationException("Each order item must have a PartnerSku.");
            }

            if (item.Quantity <= 0)
            {
                throw new InvalidOperationException($"Invalid quantity for '{item.PartnerSku}': quantity must be greater than zero.");
            }
        }

        var duplicateSku = request.Items
            .GroupBy(i => i.PartnerSku, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(g => g.Count() > 1);

        if (duplicateSku != null)
        {
            throw new InvalidOperationException($"Duplicate PartnerSku '{duplicateSku.Key}' in order items. Combine quantities into a single line item.");
        }

        if (request.DeliveryMethod == DeliveryMethod.Shipping && request.ShippingAddress == null)
        {
            throw new InvalidOperationException("A shipping address is required when delivery method is Shipping.");
        }

        if (request.ShippingAddress != null)
        {
            ValidateShippingAddress(request.ShippingAddress);
        }
    }

    private static void ValidateCustomer(CustomerDto customer)
    {
        if (string.IsNullOrWhiteSpace(customer.FirstName))
        {
            throw new InvalidOperationException("Customer first name is required.");
        }

        if (string.IsNullOrWhiteSpace(customer.LastName))
        {
            throw new InvalidOperationException("Customer last name is required.");
        }

        if (string.IsNullOrWhiteSpace(customer.Email) || !customer.Email.Contains('@'))
        {
            throw new InvalidOperationException("A valid customer email address is required.");
        }
    }

    private static void ValidateShippingAddress(ShippingAddressDto address)
    {
        if (string.IsNullOrWhiteSpace(address.Address1))
        {
            throw new InvalidOperationException("Shipping address line 1 is required.");
        }

        if (string.IsNullOrWhiteSpace(address.City))
        {
            throw new InvalidOperationException("Shipping city is required.");
        }

        if (string.IsNullOrWhiteSpace(address.Country) || address.Country.Trim().Length != 2)
        {
            throw new InvalidOperationException("Shipping country must be a 2-letter ISO country code (e.g. AU, US, GB).");
        }

        if (string.IsNullOrWhiteSpace(address.Zip))
        {
            throw new InvalidOperationException("Shipping postal code is required.");
        }
    }
}
