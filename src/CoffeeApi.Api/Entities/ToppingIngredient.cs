namespace CoffeeApi.Api.Entities;

/// <summary>Recipe line: how much of a product one portion of the topping uses.</summary>
public class ToppingIngredient
{
    public Guid ToppingId { get; set; }
    public Topping Topping { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>In the product's base units.</summary>
    public int Quantity { get; set; }
}
