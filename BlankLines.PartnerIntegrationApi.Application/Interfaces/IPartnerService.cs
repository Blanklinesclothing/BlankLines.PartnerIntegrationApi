using BlankLines.PartnerIntegrationApi.Domain.Entities;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IPartnerService
{
    Task<Partner?> GetPartnerByApiKeyAsync(string apiKey);
}
