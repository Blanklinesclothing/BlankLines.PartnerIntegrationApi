using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Validators;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BlankLines.PartnerIntegrationApi.Application.Services;

public class PartnerService(
    IApplicationDbContext context,
    IShopifyApiService shopifyService,
    IRequestValidator<CreatePartnerProductRequest> createPartnerProductRequestValidator) : IPartnerService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IShopifyApiService _shopifyService = shopifyService;
    private readonly IRequestValidator<CreatePartnerProductRequest> _createPartnerProductRequestValidator = createPartnerProductRequestValidator;

    public async Task<Partner?> GetPartnerByApiKeyAsync(string apiKey)
    {
        var hashedKey = HashApiKey(apiKey);
        return await _context.Partners
            .FirstOrDefaultAsync(p => p.ApiKey == hashedKey);
    }

    public async Task<(Partner Partner, string PlainTextApiKey)> CreatePartnerAsync(string name)
    {
        var plainTextKey = GenerateApiKey();

        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = name,
            ApiKey = HashApiKey(plainTextKey),
            CreatedAt = DateTime.UtcNow
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return (partner, plainTextKey);
    }

    public async Task<IEnumerable<PartnerDto>> GetAllPartnersAsync()
    {
        return await _context.Partners
            .OrderBy(p => p.Name)
            .Select(p => new PartnerDto
            {
                Id = p.Id,
                Name = p.Name,
                CreatedAt = p.CreatedAt
            })
            .ToListAsync();
    }

    public async Task<IEnumerable<AdminOrderDto>> GetAllOrdersAsync()
    {
        return await _context.Orders
            .Join(_context.Partners,
                o => o.PartnerId,
                p => p.Id,
                (o, p) => new AdminOrderDto
                {
                    Id = o.Id,
                    PartnerId = o.PartnerId,
                    PartnerName = p.Name,
                    PartnerOrderId = o.PartnerOrderId,
                    ShopifyOrderId = o.ShopifyOrderId,
                    Status = o.Status,
                    DeliveryMethod = o.DeliveryMethod,
                    CreatedAt = o.CreatedAt
                })
            .OrderByDescending(o => o.CreatedAt)
            .ToListAsync();
    }

    public async Task RevokePartnerAsync(Guid partnerId)
    {
        var partner = await _context.Partners
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(p => p.Id == partnerId);

        if (partner == null)
        {
            throw new KeyNotFoundException($"Partner '{partnerId}' not found");
        }

        if (partner.IsRevoked)
        {
            throw new InvalidOperationException($"Partner '{partner.Name}' is already revoked");
        }

        partner.IsRevoked = true;
        partner.RevokedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();
    }

    public async Task<IEnumerable<PartnerProductDto>> GetPartnerProductsAsync(Guid partnerId)
    {
        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new KeyNotFoundException($"Partner '{partnerId}' not found");

        return await _context.PartnerProducts
            .Where(pp => pp.PartnerId == partnerId)
            .OrderBy(pp => pp.PartnerSku)
            .Select(pp => new PartnerProductDto
            {
                Id = pp.Id,
                PartnerSku = pp.PartnerSku,
                BaseSku = pp.BaseSku,
                DesignReference = pp.DesignReference,
                ShopifyVariantId = pp.ShopifyVariantId
            })
            .ToListAsync();
    }

    public async Task<PartnerProductDto> CreatePartnerProductAsync(Guid partnerId, CreatePartnerProductRequest request)
    {
        _createPartnerProductRequestValidator.Validate(request);

        var partner = await _context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new KeyNotFoundException($"Partner '{partnerId}' not found");

        var exists = await _context.PartnerProducts
            .AnyAsync(pp => pp.PartnerId == partnerId && pp.PartnerSku == request.PartnerSku);

        if (exists)
        {
            throw new InvalidOperationException($"Partner SKU '{request.PartnerSku}' is already registered for this partner");
        }

        var variantId = await _shopifyService.ValidateBaseSkuAsync(request.BaseSku)
            ?? throw new InvalidOperationException($"BaseSku '{request.BaseSku}' does not match any active product in Shopify");

        var product = new PartnerProduct
        {
            Id = Guid.NewGuid(),
            PartnerId = partnerId,
            PartnerSku = request.PartnerSku,
            BaseSku = request.BaseSku,
            DesignReference = request.DesignReference,
            ShopifyVariantId = variantId
        };

        _context.PartnerProducts.Add(product);
        await _context.SaveChangesAsync();

        return new PartnerProductDto
        {
            Id = product.Id,
            PartnerSku = product.PartnerSku,
            BaseSku = product.BaseSku,
            DesignReference = product.DesignReference,
            ShopifyVariantId = product.ShopifyVariantId
        };
    }

    public async Task DeletePartnerProductAsync(Guid partnerId, Guid productId)
    {
        var product = await _context.PartnerProducts
            .FirstOrDefaultAsync(pp => pp.Id == productId && pp.PartnerId == partnerId)
            ?? throw new KeyNotFoundException($"Product '{productId}' not found for partner '{partnerId}'");

        _context.PartnerProducts.Remove(product);
        await _context.SaveChangesAsync();
    }

    public static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}