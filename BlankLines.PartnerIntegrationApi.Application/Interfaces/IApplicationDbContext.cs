using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BlankLines.PartnerIntegrationApi.Application.Interfaces;

public interface IApplicationDbContext
{
    DbSet<Partner> Partners { get; }
    DbSet<PartnerProduct> PartnerProducts { get; }
    DbSet<Order> Orders { get; }
    DbSet<OrderItem> OrderItems { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
