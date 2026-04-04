using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Requests;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IPartnerProductService
{
    Task<IEnumerable<PartnerProductDto>> GetPartnerProductsAsync(Guid partnerId);

    Task<PartnerProductDto> CreatePartnerProductAsync(Guid partnerId, CreatePartnerProductRequest request);

    Task DeletePartnerProductAsync(Guid partnerId, Guid productId);
}