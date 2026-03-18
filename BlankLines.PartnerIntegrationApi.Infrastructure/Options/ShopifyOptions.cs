namespace BlankLines.PartnerIntegrationApi.Infrastructure.Options;

public class ShopifyOptions
{
    public required string StoreUrl { get; set; }
    public required string AccessToken { get; set; }
    public required string ApiVersion { get; set; }
}
