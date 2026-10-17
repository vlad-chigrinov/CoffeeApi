namespace CoffeeApi.Api.Entities;

/// <summary>A tenant buying packs of a product: paid from the tenant's budget, added to its warehouse.</summary>
public class Purchase
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int Packs { get; set; }

    /// <summary>Base units added to the warehouse (packs × pack size at the time of purchase).</summary>
    public long QuantityAdded { get; set; }

    /// <summary>Amount taken from the budget (packs × pack price at the time of purchase).</summary>
    public long TotalPriceCents { get; set; }

    public DateTime CreatedAt { get; set; }
}
