namespace CoffeeApi.Api.Entities;

public class OrderItem
{
    public Guid Id { get; set; }

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid DrinkId { get; set; }
    public Drink Drink { get; set; } = null!;

    public int Quantity { get; set; }

    /// <summary>Drink price at the time of ordering.</summary>
    public long UnitPriceCents { get; set; }

    public List<OrderItemTopping> Toppings { get; set; } = [];
}
