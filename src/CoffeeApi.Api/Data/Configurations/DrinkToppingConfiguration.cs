using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeApi.Api.Data.Configurations;

public class DrinkToppingConfiguration : IEntityTypeConfiguration<DrinkTopping>
{
    public void Configure(EntityTypeBuilder<DrinkTopping> builder)
    {
        builder.HasKey(dt => new { dt.DrinkId, dt.ToppingId });

        builder.HasOne(dt => dt.Drink)
            .WithMany(d => d.DrinkToppings)
            .HasForeignKey(dt => dt.DrinkId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(dt => dt.Topping)
            .WithMany(t => t.DrinkToppings)
            .HasForeignKey(dt => dt.ToppingId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
