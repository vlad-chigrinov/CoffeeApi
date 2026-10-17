namespace CoffeeApi.Api.Entities;

/// <summary>Toppings allowed for a drink (many-to-many join).</summary>
public class DrinkTopping
{
    public Guid DrinkId { get; set; }
    public Drink Drink { get; set; } = null!;

    public Guid ToppingId { get; set; }
    public Topping Topping { get; set; } = null!;
}
