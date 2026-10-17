namespace CoffeeApi.Api.Entities;

/// <summary>Drink from the catalog shared between all tenants.</summary>
public class Drink
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public Guid? CategoryId { get; set; }
    public DrinkCategory? Category { get; set; }

    public long PriceCents { get; set; }
    public bool IsAvailable { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<DrinkTopping> DrinkToppings { get; set; } = [];
    public List<DrinkIngredient> Ingredients { get; set; } = [];
}
