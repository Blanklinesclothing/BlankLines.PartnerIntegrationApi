using BlankLines.PartnerIntegrationApi.Application.Interfaces;

namespace BlankLines.PartnerIntegrationApi.Api.Middleware;

public class ApiKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string ApiKeyHeaderName = "X-API-KEY";

    public ApiKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IPartnerAdminService partnerAdminService)
    {
        if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var apiKey) || string.IsNullOrWhiteSpace(apiKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "API key is missing" });
            return;
        }

        var partner = await partnerAdminService.GetPartnerByApiKeyAsync(apiKey!);

        if (partner == null)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid API key" });
            return;
        }

        context.Items["PartnerId"] = partner.Id;

        await _next(context);
    }
}
