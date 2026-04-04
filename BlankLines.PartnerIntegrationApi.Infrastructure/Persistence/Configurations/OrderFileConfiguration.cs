using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Persistence.Configurations;

public class OrderFileConfiguration : IEntityTypeConfiguration<OrderFile>
{
    public void Configure(EntityTypeBuilder<OrderFile> builder)
    {
        builder.HasKey(f => f.Id);

        builder.Property(f => f.OrderId)
            .IsRequired();

        builder.Property(f => f.FileType)
            .IsRequired();

        builder.Property(f => f.FileName)
            .IsRequired()
            .HasMaxLength(255);

        builder.Property(f => f.ObjectKey)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(f => f.ContentType)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(f => f.UploadedAt)
            .IsRequired();
    }
}
