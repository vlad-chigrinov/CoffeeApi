using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeApi.Api.Data.Configurations;

public class OrderConfiguration : IEntityTypeConfiguration<Order>
{
    public void Configure(EntityTypeBuilder<Order> builder)
    {
        builder.HasKey(o => o.Id);

        // Concurrency token: two simultaneous status changes cannot both succeed (e.g. completing an order twice).
        builder.Property(o => o.Status).HasConversion<string>().HasMaxLength(20).IsConcurrencyToken();
        builder.Property(o => o.CustomerName).IsRequired().HasMaxLength(Order.CustomerNameMaxLength);
        builder.Property(o => o.Comment).HasMaxLength(500);

        builder.HasOne(o => o.Tenant)
            .WithMany(t => t.Orders)
            .HasForeignKey(o => o.TenantId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(o => new { o.TenantId, o.Number }).IsUnique();
        builder.HasIndex(o => new { o.TenantId, o.CreatedAt });
    }
}
