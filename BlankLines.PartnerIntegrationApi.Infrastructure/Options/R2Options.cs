using System.ComponentModel.DataAnnotations;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Options;

public class R2Options
{
    [Required]
    public required string AccountId { get; set; }

    [Required]
    public required string AccessKeyId { get; set; }

    [Required]
    public required string SecretAccessKey { get; set; }

    [Required]
    public required string BucketName { get; set; }

    [Required]
    public required string PublicUrlBase { get; set; }

    [Required]
    public required string UploadFolder { get; set; }
}
