using BlankLines.PartnerIntegrationApi.Api.Middleware;
using BlankLines.PartnerIntegrationApi.Application;
using BlankLines.PartnerIntegrationApi.Infrastructure;
using BlankLines.PartnerIntegrationApi.Infrastructure.Data;
using BlankLines.PartnerIntegrationApi.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddOpenApi();

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddHealthChecks();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
    app.MapScalarApiReference();
}

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await context.Database.MigrateAsync();

    if (app.Environment.IsDevelopment())
    {
        await DatabaseSeeder.SeedAsync(context);
    }
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseWhen(
    ctx => ctx.Request.Path.StartsWithSegments("/api"),
    pipeline => pipeline.UseMiddleware<ApiKeyMiddleware>());

app.MapControllers();

app.UseHealthChecks("/health");

app.Run();