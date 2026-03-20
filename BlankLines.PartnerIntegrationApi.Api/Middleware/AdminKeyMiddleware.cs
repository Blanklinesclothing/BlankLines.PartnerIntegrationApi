using BlankLines.PartnerIntegrationApi.Api.Options;
using Microsoft.Extensions.Options;

namespace BlankLines.PartnerIntegrationApi.Api.Middleware;

public class AdminKeyMiddleware
{
    private readonly RequestDelegate _next;
    private const string AdminKeyHeaderName = "X-ADMIN-KEY";

    public AdminKeyMiddleware(RequestDelegate next)
    {
        _next = next;
    }

    public async Task InvokeAsync(HttpContext context, IOptions<AdminOptions> options)
    {
        if (!context.Request.Headers.TryGetValue(AdminKeyHeaderName, out var adminKey)
            || string.IsNullOrWhiteSpace(adminKey))
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Admin key is missing" });
            return;
        }

        if (adminKey != options.Value.AdminKey)
        {
            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            await context.Response.WriteAsJsonAsync(new { error = "Invalid admin key" });
            return;
        }

        await _next(context);
    }
}
