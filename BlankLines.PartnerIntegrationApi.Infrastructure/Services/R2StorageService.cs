using Amazon.Runtime;
using Amazon.S3;
using Amazon.S3.Model;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Infrastructure.Options;
using Microsoft.Extensions.Options;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Services;

public class R2StorageService : IStorageService
{
    private readonly R2Options _options;
    private readonly AmazonS3Client _client;

    public R2StorageService(IOptions<R2Options> options)
    {
        _options = options.Value;
        _client = CreateClient();
    }

    public async Task<string> UploadDesignAsync(string partnerOrderId, Stream fileStream, string contentType, string fileExtension)
    {
        var key = $"{_options.UploadFolder}/{partnerOrderId}{fileExtension}";

        var request = new PutObjectRequest
        {
            BucketName = _options.BucketName,
            Key = key,
            InputStream = fileStream,
            ContentType = contentType,
            DisablePayloadSigning = true
        };

        await _client.PutObjectAsync(request);

        return $"{_options.PublicUrlBase.TrimEnd('/')}/{key}";
    }

    private AmazonS3Client CreateClient()
    {
        var credentials = new BasicAWSCredentials(_options.AccessKeyId, _options.SecretAccessKey);

        var config = new AmazonS3Config
        {
            ServiceURL = $"https://{_options.AccountId}.r2.cloudflarestorage.com",
            ForcePathStyle = true
        };

        return new AmazonS3Client(credentials, config);
    }
}
