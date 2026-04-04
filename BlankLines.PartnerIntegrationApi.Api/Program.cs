using BlankLines.PartnerIntegrationApi.Api.Middleware;
using BlankLines.PartnerIntegrationApi.Api.OpenApi;
using BlankLines.PartnerIntegrationApi.Api.Options;
using BlankLines.PartnerIntegrationApi.Application;
using BlankLines.PartnerIntegrationApi.Infrastructure;
using BlankLines.PartnerIntegrationApi.Infrastructure.Data;
using BlankLines.PartnerIntegrationApi.Infrastructure.Persistence;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddEndpointsApiExplorer();

// Up to 10 files (5 design + 5 vector) at 10 MB each = 110 MB ceiling
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = 110 * 1024 * 1024;
});

// Rate limiting - 10 requests per minute per partner (keyed by API key header)
builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("PerPartner", httpContext =>
        RateLimitPartition.GetFixedWindowLimiter(
            partitionKey: httpContext.Request.Headers["X-API-KEY"].ToString(),
            factory: _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 10,
                Window = TimeSpan.FromMinutes(1),
                QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                QueueLimit = 0
            }));

    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = async (context, ct) =>
    {
        context.HttpContext.Response.StatusCode = StatusCodes.Status429TooManyRequests;
        await context.HttpContext.Response.WriteAsJsonAsync(
            new { error = "Too many requests. Please slow down and try again shortly." }, ct);
    };
});

builder.Services.AddOpenApi(options =>
{
    options.AddOperationTransformer<XmlDocumentationTransformer>();

    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "BlankLines Partner Integration API",
            Version = "v1",
            Description = """
                The BlankLines Partner Integration API allows approved partners to submit and manage
                fulfilment orders processed through the BlankLines Shopify store.

                All `/api/*` endpoints require an `X-API-KEY` header. Contact hello@blanklines.com
                to request credentials.
                """,
            Contact = new OpenApiContact
            {
                Name = "BlankLines",
                Email = "hello@blanklines.com"
            }
        };

        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes["ApiKey"] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = "X-API-KEY",
            Description = "Your partner API key"
        };

        var securityRequirement = new OpenApiSecurityRequirement
        {
            {
                new OpenApiSecurityScheme
                {
                    Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "ApiKey" }
                },
                Array.Empty<string>()
            }
        };

        foreach (var path in document.Paths.Values)
        {
            foreach (var operation in path.Operations.Values)
            {
                operation.Security.Add(securityRequirement);
            }
        }

        return Task.CompletedTask;
    });
});

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.Configure<AdminOptions>(builder.Configuration.GetSection("Admin"));
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.MapOpenApi();
app.MapScalarApiReference(options =>
{
    options.Title = "BlankLines Partner API";
    options.Theme = ScalarTheme.Purple;
    options.DefaultHttpClient = new(ScalarTarget.Shell, ScalarClient.Curl);
    options.AddApiKeyAuthentication("ApiKey", auth =>
    {
        auth.Name = "X-API-KEY";
        auth.Description = "Your partner API key";
    });
});

using var scope = app.Services.CreateScope();
var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
await context.Database.MigrateAsync();

if (app.Environment.IsDevelopment() || app.Environment.IsEnvironment("Sandbox"))
{
    await DatabaseSeeder.SeedAsync(context);
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseRateLimiter();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    pipeline => pipeline.UseMiddleware<ApiKeyMiddleware>());

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/admin"),
    pipeline => pipeline.UseMiddleware<AdminKeyMiddleware>());

app.MapControllers()
    .RequireRateLimiting("PerPartner");

app.UseHealthChecks("/health");

app.Run();
