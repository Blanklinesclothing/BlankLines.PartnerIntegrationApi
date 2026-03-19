using BlankLines.PartnerIntegrationApi.Api.Middleware;
using BlankLines.PartnerIntegrationApi.Application;
using BlankLines.PartnerIntegrationApi.Infrastructure;
using BlankLines.PartnerIntegrationApi.Infrastructure.Data;
using BlankLines.PartnerIntegrationApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddOpenApi(options =>
{
    options.AddDocumentTransformer((document, context, ct) =>
    {
        document.Info = new OpenApiInfo
        {
            Title = "BlankLines Partner Integration API",
            Version = "v1",
            Description = """
                The BlankLines Partner Integration API allows approved partners to submit and manage
                fulfilment orders processed through the BlankLines Shopify store.

                All `/api/*` endpoints require an `X-API-KEY` header. Contact integrations@blanklines.co.uk
                to request credentials.
                """,
            Contact = new OpenApiContact
            {
                Name = "BlankLines Integrations",
                Email = "integrations@blanklines.co.uk"
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

if (app.Environment.IsDevelopment())
{
    await DatabaseSeeder.SeedAsync(context);
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    pipeline => pipeline.UseMiddleware<ApiKeyMiddleware>());

app.MapControllers();

app.UseHealthChecks("/health");

app.Run();