using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.Responses;

public class OrderResponse
{
    public required string PartnerOrderId { get; set; }
    public string? ShopifyOrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public required CustomerDto Customer { get; set; }
    public ShippingAddressDto? ShippingAddress { get; set; }
    public List<OrderFileDto> Files { get; set; } = [];
    public required List<OrderItemResponseDto> Items { get; set; }
}

public class OrderItemResponseDto
{
    public required string PartnerSku { get; set; }
    public required string DesignReference { get; set; }
    public int Quantity { get; set; }
}
