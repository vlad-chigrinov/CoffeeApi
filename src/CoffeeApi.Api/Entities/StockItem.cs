namespace CoffeeApi.Api.Entities;

/// <summary>One line of a tenant's warehouse. Each tenant has its own stock of every product.</summary>
public class StockItem
{
    public Guid TenantId { get; set; }

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    /// <summary>In the product's base units. Never negative.</summary>
    public long Quantity { get; set; }
}
