namespace BlankLines.PartnerIntegrationApi.Application.DTOs;

public class PartnerProductDto
{
    public Guid Id { get; set; }
    public required string PartnerSku { get; set; }
    public required string BaseSku { get; set; }
    public required string DesignReference { get; set; }
    public long? ShopifyVariantId { get; set; }
}
