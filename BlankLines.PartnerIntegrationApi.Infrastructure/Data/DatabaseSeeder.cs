using BlankLines.PartnerIntegrationApi.Application.Services;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using BlankLines.PartnerIntegrationApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Data;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Partners.AnyAsync())
        {
            return;
        }

        var partner1 = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "Test Partner 1",
            ApiKey = PartnerService.HashApiKey("test-api-key-123"),
            CreatedAt = DateTime.UtcNow
        };

        var partner2 = new Partner
        {
            Id = Guid.NewGuid(),
            Name = "Test Partner 2",
            ApiKey = PartnerService.HashApiKey("test-api-key-456"),
            CreatedAt = DateTime.UtcNow
        };

        context.Partners.AddRange(partner1, partner2);

        var partnerProducts = new List<PartnerProduct>
        {
            new PartnerProduct
            {
                Id = Guid.NewGuid(),
                PartnerId = partner1.Id,
                PartnerSku = "PARTNER1-TANK-BLACK",
                BaseSku = "M106GV100-S",
                DesignReference = "Design-001",
                ShopifyVariantId = 49420784042305
            },
            new PartnerProduct
            {
                Id = Guid.NewGuid(),
                PartnerId = partner1.Id,
                PartnerSku = "PARTNER1-TANK-WHITE",
                BaseSku = "M106GV900-S",
                DesignReference = "Design-002",
                ShopifyVariantId = 50366105354561
            },
            new PartnerProduct
            {
                Id = Guid.NewGuid(),
                PartnerId = partner2.Id,
                PartnerSku = "PARTNER2-TRACKPANT-BLACK",
                BaseSku = "W312JG100-XS",
                DesignReference = "Design-003",
                ShopifyVariantId = 50387741671745
            }
        };

        context.PartnerProducts.AddRange(partnerProducts);

        await context.SaveChangesAsync();
    }
}
