using BlankLines.PartnerIntegrationApi.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlankLines.PartnerIntegrationApi.Infrastructure.Persistence.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        builder.Property(o => o.PartnerId)
            .IsRequired();

        builder.Property(o => o.PartnerOrderId)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.ShopifyOrderId)
            .HasMaxLength(100);

        builder.Property(o => o.Status)
            .IsRequired();

        builder.Property(o => o.DeliveryMethod)
            .IsRequired();

        builder.Property(o => o.CreatedAt)
            .IsRequired();

        builder.Property(o => o.CustomerFirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CustomerLastName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(o => o.CustomerEmail)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(o => o.CustomerPhone)
            .HasMaxLength(50);

        builder.Property(o => o.ShippingAddress1)
            .HasMaxLength(200);

        builder.Property(o => o.ShippingAddress2)
            .HasMaxLength(200);

        builder.Property(o => o.ShippingCity)
            .HasMaxLength(100);

        builder.Property(o => o.ShippingProvince)
            .HasMaxLength(100);

        builder.Property(o => o.ShippingCountry)
            .HasMaxLength(100);

        builder.Property(o => o.ShippingZip)
            .HasMaxLength(20);

        builder.Property(o => o.ShippingPhone)
            .HasMaxLength(50);

        builder.HasMany(o => o.Items)
            .WithOne()
            .HasForeignKey(oi => oi.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(o => new { o.PartnerId, o.PartnerOrderId })
            .IsUnique();
    }
}
