using CoffeeApi.Tenant.Api;

namespace CoffeeApi.Tenant.Services;

/// <summary>One line of the cart: a drink, how many, and the chosen toppings (priced at the time they were picked).</summary>
public sealed record CartLine(Guid DrinkId, string DrinkName, long UnitPriceCents, int Quantity, List<Topping> Toppings)
{
    public long Total => Quantity * (UnitPriceCents + Toppings.Sum(t => t.PriceCents));
}

/// <summary>
/// The order being put together. It lives in the circuit, not in a page, so the cart survives moving
/// to the warehouse to buy missing ingredients and coming back.
/// </summary>
public class CartState
{
    public IReadOnlyList<CartLine> Lines { get; private set; } = [];

    public string? Comment { get; set; }

    public long Total => Lines.Sum(l => l.Total);

    public void Add(CartLine line) => Lines = [.. Lines, line];

    public void Remove(CartLine line) => Lines = Lines.Where(l => l != line).ToList();

    public void Clear()
    {
        Lines = [];
        Comment = null;
    }
}
