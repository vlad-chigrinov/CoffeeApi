namespace CoffeeApi.Api.Entities;

/// <summary>Order owned by exactly one tenant.</summary>
public class Order
{
    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Sequential number, unique within the tenant.</summary>
    public int Number { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.InProgress;
    public long TotalPriceCents { get; set; }
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
    public List<OrderIngredient> Ingredients { get; set; } = [];
}
