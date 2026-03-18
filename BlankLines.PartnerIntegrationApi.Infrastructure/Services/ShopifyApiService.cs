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

    public async Task<string> CreateOrderAsync(Domain.Entities.Order order)
    {
        var orderService = _orderServiceFactory.Create(Credentials);

        var shopifyOrder = new Order
        {
            LineItems = order.Items.Select(item => new LineItem
            {
                VariantId = item.ShopifyVariantId,
                Quantity = item.Quantity
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
            Note = $"Partner Order ID: {order.PartnerOrderId}" +
                   string.Concat(order.Items.Select(i => $"\n{i.PartnerSku}: {i.DesignReference}")),
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
}