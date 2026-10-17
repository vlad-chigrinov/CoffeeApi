namespace CoffeeApi.Api.Entities;

/// <summary>Order owned by exactly one tenant.</summary>
public class Order
{
    public const int CustomerNameMinLength = 2;
    public const int CustomerNameMaxLength = 10;

    public Guid Id { get; set; }

    public Guid TenantId { get; set; }
    public Tenant Tenant { get; set; } = null!;

    /// <summary>Sequential number, unique within the tenant.</summary>
    public int Number { get; set; }

    public OrderStatus Status { get; set; } = OrderStatus.InProgress;
    public long TotalPriceCents { get; set; }

    /// <summary>Who the order is for (2-10 characters).</summary>
    public string CustomerName { get; set; } = null!;

    /// <summary>Free-form note to the order, optional.</summary>
    public string? Comment { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    public List<OrderItem> Items { get; set; } = [];
    public List<OrderIngredient> Ingredients { get; set; } = [];
}
