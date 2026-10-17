using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CoffeeApi.Api.Data.Configurations;

public class OrderItemToppingConfiguration : IEntityTypeConfiguration<OrderItemTopping>
{
    public void Configure(EntityTypeBuilder<OrderItemTopping> builder)
    {
        builder.HasKey(t => new { t.OrderItemId, t.ToppingId });

        builder.HasOne(t => t.OrderItem)
            .WithMany(i => i.Toppings)
            .HasForeignKey(t => t.OrderItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Topping)
            .WithMany()
            .HasForeignKey(t => t.ToppingId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
