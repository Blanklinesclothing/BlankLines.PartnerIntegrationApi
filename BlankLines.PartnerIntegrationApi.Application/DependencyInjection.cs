using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace BlankLines.PartnerIntegrationApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPartnerService, PartnerService>();

        return services;
    }
}
