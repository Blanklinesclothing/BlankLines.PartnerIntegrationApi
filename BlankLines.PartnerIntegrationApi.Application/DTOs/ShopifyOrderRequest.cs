using BlankLines.PartnerIntegrationApi.Domain.Enums;

namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class ShopifyOrderRequest
{
    public required string PartnerOrderId { get; init; }
    public DeliveryMethod DeliveryMethod { get; init; }
    public required string CustomerFirstName { get; init; }
    public required string CustomerLastName { get; init; }
    public required string CustomerEmail { get; init; }
    public string? CustomerPhone { get; init; }
    public string? ShippingAddress1 { get; init; }
    public string? ShippingAddress2 { get; init; }
    public string? ShippingCity { get; init; }
    public string? ShippingProvince { get; init; }
    public string? ShippingCountry { get; init; }
    public string? ShippingZip { get; init; }
    public string? ShippingPhone { get; init; }
    public required IReadOnlyCollection<ShopifyOrderLineItem> LineItems { get; init; }
}

public class ShopifyOrderLineItem
{
    public required string PartnerSku { get; init; }
    public required string DesignReference { get; init; }
    public long? ShopifyVariantId { get; init; }
    public int Quantity { get; init; }
    public required IReadOnlyCollection<ShopifyOrderFile> Files { get; init; }
}

public class ShopifyOrderFile
{
    public OrderFileType FileType { get; init; }
    public required string ObjectKey { get; init; }
}
