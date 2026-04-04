using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Domain.Entities;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IPartnerAdminService
{
    Task<Partner?> GetPartnerByApiKeyAsync(string apiKey);

    Task<(Partner Partner, string PlainTextApiKey)> CreatePartnerAsync(string name);

    Task<IEnumerable<PartnerDto>> GetAllPartnersAsync();

    Task<IEnumerable<AdminOrderDto>> GetAllOrdersAsync();

    Task RevokePartnerAsync(Guid partnerId);
}