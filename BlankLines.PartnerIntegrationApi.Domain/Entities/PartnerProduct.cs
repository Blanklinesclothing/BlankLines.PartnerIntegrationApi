namespace BlankLines.PartnerIntegrationApi.Domain.Entities;

public class PartnerProduct
{
    public Guid Id { get; set; }
    public Guid PartnerId { get; set; }
    public required string PartnerSku { get; set; }
    public required string BaseSku { get; set; }
    public required string DesignReference { get; set; }
    public long? ShopifyVariantId { get; set; }
}
