using BlankLines.PartnerIntegrationApi.Application.DTOs;
using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Validators;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlankLines.PartnerIntegrationApi.Application.Services;

public class PartnerProductService(
    IApplicationDbContext context,
    IShopifyApiService shopifyService,
    IRequestValidator<CreatePartnerProductRequest> createPartnerProductRequestValidator) : IPartnerProductService
{
    private readonly IApplicationDbContext _context = context;
    private readonly IShopifyApiService _shopifyService = shopifyService;
    private readonly IRequestValidator<CreatePartnerProductRequest> _validator = createPartnerProductRequestValidator;

    public async Task<IEnumerable<PartnerProductDto>> GetPartnerProductsAsync(Guid partnerId)
    {
        _ = await _context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId)
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
        _validator.Validate(request);

        _ = await _context.Partners.FirstOrDefaultAsync(p => p.Id == partnerId)
            ?? throw new KeyNotFoundException($"Partner '{partnerId}' not found");

        var exists = await _context.PartnerProducts
            .AnyAsync(pp => pp.PartnerId == partnerId && pp.PartnerSku == request.PartnerSku);

        if (exists)
            throw new InvalidOperationException($"Partner SKU '{request.PartnerSku}' is already registered for this partner");

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
}
