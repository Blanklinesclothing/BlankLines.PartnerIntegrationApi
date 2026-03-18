namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class OrderItem
{
    public Guid Id { get; set; }
    public Guid OrderId { get; set; }
    public required string PartnerSku { get; set; }
    public required string BaseSku { get; set; }
    public required string DesignReference { get; set; }
    public long? ShopifyVariantId { get; set; }
    public int Quantity { get; set; }
}
