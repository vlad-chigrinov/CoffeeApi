namespace CoffeeApi.Api.Entities;

/// <summary>
/// What an order took from the warehouse, summed per product. Stored as a snapshot so cancelling returns
/// exactly this amount even if recipes changed after the order was placed.
/// </summary>
public class OrderIngredient
{
    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public long Quantity { get; set; }
}
