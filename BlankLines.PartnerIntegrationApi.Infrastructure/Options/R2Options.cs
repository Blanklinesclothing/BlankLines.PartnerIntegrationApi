namespace BlankLines.PartnerIntegrationApi.Infrastructure.Options;

public class R2Options
{
    public required string AccountId { get; set; }
    public required string AccessKeyId { get; set; }
    public required string SecretAccessKey { get; set; }
    public required string BucketName { get; set; }
    public required string PublicUrlBase { get; set; }
    public string UploadFolder { get; set; } = "test-partner-designs";
}
