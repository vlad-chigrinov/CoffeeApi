namespace CoffeeApi.Api.Entities;

/// <summary>
/// Ingredient from the catalog shared between all tenants. Stock and recipes are counted in base units
/// (<see cref="Unit"/>, e.g. g, ml, pcs); tenants buy it in packs of <see cref="PackSize"/> base units.
/// </summary>
public class Product
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public int PackSize { get; set; }
    public long PackPriceCents { get; set; }
}
