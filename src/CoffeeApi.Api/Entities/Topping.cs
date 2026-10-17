namespace CoffeeApi.Api.Entities;

/// <summary>Topping from the catalog shared between all tenants.</summary>
public class Topping
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public long PriceCents { get; set; }
    public bool IsAvailable { get; set; } = true;

    public List<DrinkTopping> DrinkToppings { get; set; } = [];
    public List<ToppingIngredient> Ingredients { get; set; } = [];
}
