namespace CoffeeApi.Api.Entities;

public class OrderItemTopping
{
    public Guid OrderItemId { get; set; }
    public OrderItem OrderItem { get; set; } = null!;

    public Guid ToppingId { get; set; }
    public Topping Topping { get; set; } = null!;

    /// <summary>Topping price at the time of ordering.</summary>
    public long PriceCents { get; set; }
}
