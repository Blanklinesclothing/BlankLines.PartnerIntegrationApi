using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Domain.Entities;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IShopifyApiService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<string> CreateOrderAsync(Order order);
}
