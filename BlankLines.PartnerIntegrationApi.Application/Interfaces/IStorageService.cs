namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadFileAsync(string objectKey, Stream fileStream, string contentType);
    Task<string> GeneratePresignedUrlAsync(string objectKey, TimeSpan expiry);
}
