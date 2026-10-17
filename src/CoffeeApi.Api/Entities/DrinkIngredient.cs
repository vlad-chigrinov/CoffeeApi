namespace CoffeeApi.Api.Entities;

/// <summary>Recipe line: how much of a product one serving of the drink uses.</summary>
public class DrinkIngredient
{
    public Guid DrinkId { get; set; }
    public Drink Drink { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>In the product's base units.</summary>
    public int Quantity { get; set; }
}
