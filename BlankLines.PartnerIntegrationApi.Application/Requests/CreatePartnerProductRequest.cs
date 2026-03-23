namespace BlankLines.PartnerIntegrationApi.Application.Requests;

public class CreatePartnerProductRequest
{
    public required string PartnerSku { get; set; }
    public required string BaseSku { get; set; }
    public required string DesignReference { get; set; }
    public long? ShopifyVariantId { get; set; }
}
