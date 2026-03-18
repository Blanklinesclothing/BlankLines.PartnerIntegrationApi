using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Responses;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using BlankLines.PartnerIntegrationApi.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BlankLines.PartnerIntegrationApi.Application.Services;

public class OrderService : IOrderService
{
    private readonly IApplicationDbContext _context;
    private readonly IShopifyApiService _shopifyService;

    public OrderService(IApplicationDbContext context, IShopifyApiService shopifyService)
    {
        _context = context;
        _shopifyService = shopifyService;
    }

    public async Task<string> CreateOrderAsync(Guid partnerId, CreateOrderRequest request)
    {
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
                .FirstOrDefault(pp => pp.PartnerSku == item.PartnerSku);

            if (partnerProduct == null)
            {
                throw new InvalidOperationException($"Partner SKU '{item.PartnerSku}' not found");
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

        var shopifyOrderId = await _shopifyService.CreateOrderAsync(order);
        order.ShopifyOrderId = shopifyOrderId;

        _context.Orders.Add(order);
        await _context.SaveChangesAsync();

        return order.Id.ToString();
    }

    public async Task<OrderResponse> GetOrderAsync(Guid partnerId, string partnerOrderId)
    {
        var order = await _context.Orders
            .FirstOrDefaultAsync(o => o.PartnerId == partnerId && o.PartnerOrderId == partnerOrderId);

        if (order == null)
        {
            throw new KeyNotFoundException($"Order '{partnerOrderId}' not found");
        }

        return new OrderResponse
        {
            PartnerOrderId = order.PartnerOrderId,
            ShopifyOrderId = order.ShopifyOrderId,
            Status = order.Status,
            CreatedAt = order.CreatedAt
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

        order.Status = OrderStatus.Cancelled;
        await _context.SaveChangesAsync();
    }
}
