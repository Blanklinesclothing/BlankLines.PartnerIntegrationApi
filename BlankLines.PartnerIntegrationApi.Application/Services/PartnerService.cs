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

    public static string HashApiKey(string apiKey)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(apiKey));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
