using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeApi.Api.Data.Configurations;

public class DrinkCategoryConfiguration : IEntityTypeConfiguration<DrinkCategory>
{
    public void Configure(EntityTypeBuilder<DrinkCategory> builder)
    {
        builder.HasKey(c => c.Id);

        builder.Property(c => c.Name).IsRequired().HasMaxLength(100);

        builder.HasIndex(c => c.Name).IsUnique();
    }
}
