using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeApi.Api.Data.Configurations;

public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
{
    public void Configure(EntityTypeBuilder<Tenant> builder)
    {
        builder.HasKey(t => t.Id);

        builder.Property(t => t.Name).IsRequired().HasMaxLength(200);
        builder.Property(t => t.ApiKeyHash).IsRequired().HasMaxLength(64);
        builder.Property(t => t.ApiKeyPrefix).IsRequired().HasMaxLength(16);

        builder.HasIndex(t => t.Name).IsUnique();
        builder.HasIndex(t => t.ApiKeyHash).IsUnique();
    }
}
