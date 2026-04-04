using BlankLines.PartnerIntegrationApi.Application.DTOs;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IShopifyApiService
{
    Task<IEnumerable<ProductDto>> GetProductsAsync();
    Task<long?> ValidateBaseSkuAsync(string sku);
    Task<int> GetInventoryQuantityAsync(long variantId);
    Task<string> CreateOrderAsync(ShopifyOrderRequest request);
    Task CancelOrderAsync(long shopifyOrderId);
}
