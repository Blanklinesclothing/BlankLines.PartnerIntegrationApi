using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Domain.Entities;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IPartnerService
{
    Task<Partner?> GetPartnerByApiKeyAsync(string apiKey);

    Task<(Partner Partner, string PlainTextApiKey)> CreatePartnerAsync(string name);

    Task<IEnumerable<PartnerDto>> GetAllPartnersAsync();

    Task<IEnumerable<AdminOrderDto>> GetAllOrdersAsync();

    Task RevokePartnerAsync(Guid partnerId);

    Task<IEnumerable<PartnerProductDto>> GetPartnerProductsAsync(Guid partnerId);

    Task<PartnerProductDto> CreatePartnerProductAsync(Guid partnerId, CreatePartnerProductRequest request);

    Task DeletePartnerProductAsync(Guid partnerId, Guid productId);
}