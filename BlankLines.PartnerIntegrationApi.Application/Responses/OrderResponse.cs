using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.Responses;

public class OrderResponse
{
    public required string PartnerOrderId { get; set; }
    public string? ShopifyOrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DateTime CreatedAt { get; set; }
}
