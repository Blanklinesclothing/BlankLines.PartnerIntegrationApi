using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class Order
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public required string PartnerOrderId { get; set; }
    public string? ShopifyOrderId { get; set; }
    public OrderStatus Status { get; set; }
    public DeliveryMethod DeliveryMethod { get; set; }
    public DateTime CreatedAt { get; set; }
    public required string CustomerFirstName { get; set; }
    public required string CustomerLastName { get; set; }
    public required string CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public string? ShippingAddress1 { get; set; }
    public string? ShippingAddress2 { get; set; }
    public string? ShippingCity { get; set; }
    public string? ShippingProvince { get; set; }
    public string? ShippingCountry { get; set; }
    public string? ShippingZip { get; set; }
    public string? ShippingPhone { get; set; }
    public ICollection<OrderFile> Files { get; set; } = new List<OrderFile>();
    public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
}
