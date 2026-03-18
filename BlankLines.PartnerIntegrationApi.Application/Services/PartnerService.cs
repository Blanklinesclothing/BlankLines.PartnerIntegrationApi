using BlankLines.PartnerIntegrationApi.Application.Interfaces;
using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
        return await _context.Partners
            .FirstOrDefaultAsync(p => p.ApiKey == apiKey);
    }
}
