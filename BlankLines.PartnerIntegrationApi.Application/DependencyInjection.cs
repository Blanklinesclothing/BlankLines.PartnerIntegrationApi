using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Application.Requests;
using BlankLines.PartnerIntegrationApi.Application.Services;
using BlankLines.PartnerIntegrationApi.Application.Validators;
using Microsoft.Extensions.DependencyInjection;

namespace BlankLines.PartnerIntegrationApi.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IOrderService, OrderService>();
        services.AddScoped<IPartnerService, PartnerService>();

        services.AddScoped<IRequestValidator<CreateOrderRequest>, CreateOrderRequestValidator>();
        services.AddScoped<IRequestValidator<CancelOrderRequest>, CancelOrderRequestValidator>();
        services.AddScoped<IRequestValidator<CreatePartnerProductRequest>, CreatePartnerProductRequestValidator>();

        return services;
    }
}
