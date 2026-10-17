namespace CoffeeApi.Api.Entities;

/// <summary>Drink category from the catalog shared between all tenants.</summary>
public class DrinkCategory
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    public List<Drink> Drinks { get; set; } = [];
}
