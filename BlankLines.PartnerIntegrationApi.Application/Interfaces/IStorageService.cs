namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IStorageService
{
    Task<string> UploadDesignAsync(string partnerOrderId, Stream fileStream, string contentType, string fileExtension);
}
