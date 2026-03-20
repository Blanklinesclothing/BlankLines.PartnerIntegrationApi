using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Security.Cryptography;
using System.Text;

namespace BlankLines.PartnerIntegrationApi.Application.Services;

public class PartnerService : IPartnerService
{
    private readonly IApplicationDbContext _context;

    public PartnerService(IApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<Partner?> GetPartnerByApiKeyAsync(string apiKey)
    {
        var hashedKey = HashApiKey(apiKey);
        return await _context.Partners
            .FirstOrDefaultAsync(p => p.ApiKey == hashedKey);
    }

    public async Task<(Partner Partner, string PlainTextApiKey)> CreatePartnerAsync(string name)
    {
        var plainTextKey = GenerateApiKey();

        var partner = new Partner
        {
            Id = Guid.NewGuid(),
            Name = name,
            ApiKey = HashApiKey(plainTextKey),
            CreatedAt = DateTime.UtcNow
        };

        _context.Partners.Add(partner);
        await _context.SaveChangesAsync();

        return (partner, plainTextKey);
    }

    public static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }

    private static string GenerateApiKey()
    {
        var bytes = RandomNumberGenerator.GetBytes(32);
        return Convert.ToBase64String(bytes)
            .Replace("+", "-")
            .Replace("/", "_")
            .TrimEnd('=');
    }
}
