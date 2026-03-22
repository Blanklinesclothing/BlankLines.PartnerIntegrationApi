using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class AdminOrderDto
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public required string PartnerName { get; set; }
    public required string PartnerOrderId { get; set; }
    public string? ShopifyOrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public DateTime CreatedAt { get; set; }
}
