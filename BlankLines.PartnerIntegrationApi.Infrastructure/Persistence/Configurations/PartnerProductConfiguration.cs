using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Persistence.Configurations;

public class PartnerProductConfiguration : IEntityTypeConfiguration<PartnerProduct>
{
    public void Configure(EntityTypeBuilder<PartnerProduct> builder)
    {
        builder.HasKey(pp => pp.Id);

        builder.Property(pp => pp.PartnerId)
            .IsRequired();

        builder.Property(pp => pp.PartnerSku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pp => pp.BaseSku)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(pp => pp.DesignReference)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(pp => new { pp.PartnerId, pp.PartnerSku })
            .IsUnique();
    }
}
