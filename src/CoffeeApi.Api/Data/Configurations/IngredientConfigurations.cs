using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeApi.Api.Data.Configurations;

public class DrinkIngredientConfiguration : IEntityTypeConfiguration<DrinkIngredient>
{
    public void Configure(EntityTypeBuilder<DrinkIngredient> builder)
    {
        builder.HasKey(i => new { i.DrinkId, i.ProductId });

        builder.HasOne(i => i.Drink)
            .WithMany(d => d.Ingredients)
            .HasForeignKey(i => i.DrinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class ToppingIngredientConfiguration : IEntityTypeConfiguration<ToppingIngredient>
{
    public void Configure(EntityTypeBuilder<ToppingIngredient> builder)
    {
        builder.HasKey(i => new { i.ToppingId, i.ProductId });

        builder.HasOne(i => i.Topping)
            .WithMany(t => t.Ingredients)
            .HasForeignKey(i => i.ToppingId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}

public class OrderIngredientConfiguration : IEntityTypeConfiguration<OrderIngredient>
{
    public void Configure(EntityTypeBuilder<OrderIngredient> builder)
    {
        builder.HasKey(i => new { i.OrderId, i.ProductId });

        builder.HasOne(i => i.Order)
            .WithMany(o => o.Ingredients)
            .HasForeignKey(i => i.OrderId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(i => i.Product)
            .WithMany()
            .HasForeignKey(i => i.ProductId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
