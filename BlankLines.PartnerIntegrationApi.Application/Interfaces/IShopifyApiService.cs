using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Domain.Entities;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IShopifyApiService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<long?> ValidateBaseSkuAsync(string sku);
    Task<string> CreateOrderAsync(Order order);
    Task CancelOrderAsync(long shopifyOrderId);
}
