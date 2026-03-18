using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Infrastructure.Options;
using BlankLines.PartnerIntegrationApi.Infrastructure.Persistence;
using BlankLines.PartnerIntegrationApi.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ShopifySharp;
using ShopifySharp.Extensions.DependencyInjection;

namespace BlankLines.PartnerIntegrationApi.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("DefaultConnection")));

        services.AddScoped<IApplicationDbContext>(provider =>
            provider.GetRequiredService<ApplicationDbContext>());

        services.Configure<ShopifyOptions>(configuration.GetSection("Shopify"));

        services.AddShopifySharp<LeakyBucketExecutionPolicy>();

        services.AddScoped<IShopifyApiService, ShopifyApiService>();

        return services;
    }
}