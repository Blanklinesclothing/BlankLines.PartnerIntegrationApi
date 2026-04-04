using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Infrastructure.Options;
using Microsoft.Extensions.Options;
using ShopifySharp;
using ShopifySharp.Credentials;
using ShopifySharp.Factories;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Services;

public class ShopifyApiService : IShopifyApiService
{
    private readonly IGraphServiceFactory _graphServiceFactory;
    private readonly IOrderServiceFactory _orderServiceFactory;
    private readonly ShopifyOptions _shopifyOptions;

    public ShopifyApiService(
        IGraphServiceFactory graphServiceFactory,
        IOrderServiceFactory orderServiceFactory,
        IOptions<ShopifyOptions> options)
    {
        _graphServiceFactory = graphServiceFactory;
        _orderServiceFactory = orderServiceFactory;
        _shopifyOptions = options.Value;
    }

    private ShopifyApiCredentials Credentials =>
        new(_shopifyOptions.StoreUrl, _shopifyOptions.AccessToken);

    public async Task<IEnumerable<ProductDto>> GetProductsAsync()
    {
        var graphService = _graphServiceFactory.Create(Credentials);

        var request = new GraphRequest
        {
            Query = @"
                query listProducts($limit: Int!, $query: String!) {
                    products(first: $limit, query: $query) {
                        pageInfo {
                            hasNextPage
                            endCursor
                        }
                        nodes {
                            id
                            legacyResourceId
                            title
                            variants(first: 1) {
                                nodes {
                                    legacyResourceId
                                    sku
                                    inventoryQuantity
                                }
                            }
                        }
                    }
                }",
            Variables = new Dictionary<string, object>
            {
                { "limit", 250 },
                { "query", "status:ACTIVE" }
            }
        };

        try
        {
            var response = await graphService.PostAsync<ProductsQueryResult>(request);

            if (response?.Data?.Products?.Nodes == null)
            {
                return Enumerable.Empty<ProductDto>();
            }

            return response.Data.Products.Nodes
                .Where(p => p.legacyResourceId != null)
                .Select(p => new ProductDto
                {
                    Id = p.legacyResourceId?.ToString() ?? string.Empty,
                    Title = p.title ?? string.Empty,
                    Sku = p.variants?.nodes?.FirstOrDefault()?.sku ?? string.Empty,
                    VariantId = p.variants?.nodes?.FirstOrDefault()?.legacyResourceId,
                    InventoryQuantity = p.variants?.nodes?.FirstOrDefault()?.inventoryQuantity ?? 0
                });
        }
        catch (ShopifyGraphErrorsException ex)
        {
            throw new InvalidOperationException(
                $"GraphQL query error: {string.Join(", ", ex.GraphErrors.Select(e => e.Message))}",
                ex);
        }
    }

    public async Task<long?> ValidateBaseSkuAsync(string sku)
    {
        var graphService = _graphServiceFactory.Create(Credentials);

        var request = new GraphRequest
        {
            Query = @"
                query findVariantBySku($query: String!) {
                    productVariants(first: 1, query: $query) {
                        nodes {
                            legacyResourceId
                            sku
                        }
                    }
                }",
            Variables = new Dictionary<string, object>
            {
                { "query", $"sku:{sku}" }
            }
        };

        try
        {
            var response = await graphService.PostAsync<VariantSkuQueryResult>(request);

            var variant = response?.Data?.ProductVariants?.Nodes?.FirstOrDefault();

            if (variant == null || !string.Equals(variant.sku, sku, StringComparison.Ordinal))
            {
                return null;
            }

            return variant.legacyResourceId;
        }
        catch (ShopifyGraphErrorsException ex)
        {
            throw new InvalidOperationException(
                $"GraphQL query error: {string.Join(", ", ex.GraphErrors.Select(e => e.Message))}",
                ex);
        }
    }

    public async Task<int> GetInventoryQuantityAsync(long variantId)
    {
        var graphService = _graphServiceFactory.Create(Credentials);

        var gid = $"gid://shopify/ProductVariant/{variantId}";

        var request = new GraphRequest
        {
            Query = @"
                query getVariantInventory($id: ID!) {
                    productVariant(id: $id) {
                        inventoryQuantity
                    }
                }",
            Variables = new Dictionary<string, object>
            {
                { "id", gid }
            }
        };

        try
        {
            var response = await graphService.PostAsync<VariantInventoryQueryResult>(request);
            return response?.Data?.ProductVariant?.inventoryQuantity ?? 0;
        }
        catch (ShopifyGraphErrorsException ex)
        {
            throw new InvalidOperationException(
                $"GraphQL query error: {string.Join(", ", ex.GraphErrors.Select(e => e.Message))}",
                ex);
        }
    }

    public async Task<string> CreateOrderAsync(Domain.Entities.Order order)
    {
        var orderService = _orderServiceFactory.Create(Credentials);

        var shopifyOrder = new Order
        {
            LineItems = order.Items.Select(item => new LineItem
            {
                VariantId = item.ShopifyVariantId,
                Quantity = item.Quantity,
                Properties = BuildLineItemProperties(item, order.DesignFileUrl)
            }).ToList(),
            Customer = new Customer
            {
                FirstName = order.CustomerFirstName,
                LastName = order.CustomerLastName,
                Email = order.CustomerEmail,
                Phone = order.CustomerPhone
            },
            ShippingAddress = order.ShippingAddress1 != null ? new Address
            {
                FirstName = order.CustomerFirstName,
                LastName = order.CustomerLastName,
                Address1 = order.ShippingAddress1,
                Address2 = order.ShippingAddress2,
                City = order.ShippingCity,
                Province = order.ShippingProvince,
                Country = order.ShippingCountry,
                Zip = order.ShippingZip,
                Phone = order.ShippingPhone
            } : null,
            Note = $"Partner Order ID: {order.PartnerOrderId}",
            Tags = order.DeliveryMethod.ToString(),
            FinancialStatus = "paid"
        };

        var createdOrder = await orderService.CreateAsync(shopifyOrder);

        if (createdOrder?.Id == null)
        {
            throw new InvalidOperationException("Failed to create order in Shopify");
        }

        return createdOrder.Id.Value.ToString();
    }

    private static List<LineItemProperty> BuildLineItemProperties(Domain.Entities.OrderItem item, string? designFileUrl)
    {
        var properties = new List<LineItemProperty>
        {
            new() { Name = "Partner SKU", Value = item.PartnerSku },
            new() { Name = "Design Reference", Value = item.DesignReference }
        };

        if (!string.IsNullOrWhiteSpace(designFileUrl))
        {
            properties.Add(new() { Name = "Design File", Value = designFileUrl });
        }

        return properties;
    }

    public async Task CancelOrderAsync(long shopifyOrderId)
    {
        var orderService = _orderServiceFactory.Create(Credentials);
        await orderService.CancelAsync(shopifyOrderId);
    }

    private record ProductsQueryResult
    {
        public required ProductConnection Products { get; set; }
    }

    private record ProductConnection
    {
        public PageInfo? pageInfo { get; set; }
        public List<ProductNode>? Nodes { get; set; }
    }

    private record PageInfo
    {
        public bool hasNextPage { get; set; }
        public string? endCursor { get; set; }
    }

    private record ProductNode
    {
        public string? id { get; set; }
        public long? legacyResourceId { get; set; }
        public string? title { get; set; }
        public VariantConnection? variants { get; set; }
    }

    private record VariantConnection
    {
        public List<VariantNode>? nodes { get; set; }
    }

    private record VariantNode
    {
        public long? legacyResourceId { get; set; }
        public string? sku { get; set; }
        public int? inventoryQuantity { get; set; }
    }

    private record VariantSkuQueryResult
    {
        public required VariantSkuConnection ProductVariants { get; set; }
    }

    private record VariantSkuConnection
    {
        public List<VariantSkuNode>? Nodes { get; set; }
    }

    private record VariantSkuNode
    {
        public long? legacyResourceId { get; set; }
        public string? sku { get; set; }
    }

    private record VariantInventoryQueryResult
    {
        public VariantInventoryNode? ProductVariant { get; set; }
    }

    private record VariantInventoryNode
    {
        public int? inventoryQuantity { get; set; }
    }
}
