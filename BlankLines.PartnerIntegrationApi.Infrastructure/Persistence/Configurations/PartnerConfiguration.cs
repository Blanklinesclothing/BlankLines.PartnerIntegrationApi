using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Persistence.Configurations;

public class PartnerConfiguration : IEntityTypeConfiguration<Partner>
{
    public void Configure(EntityTypeBuilder<Partner> builder)
    {
        builder.HasKey(p => p.Id);

        builder.Property(p => p.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(p => p.ApiKey)
            .IsRequired()
            .HasMaxLength(100);

        builder.HasIndex(p => p.ApiKey)
            .IsUnique();

        builder.Property(p => p.CreatedAt)
            .IsRequired();
    }
}
