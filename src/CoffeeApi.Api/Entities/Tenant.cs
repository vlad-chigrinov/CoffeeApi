namespace CoffeeApi.Api.Entities;

public class Tenant
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;

    /// <summary>SHA-256 hash of the API key (hex). The key itself is never stored.</summary>
    public string ApiKeyHash { get; set; } = string.Empty;

    /// <summary>First characters of the API key, used to identify a tenant in logs.</summary>
    public string ApiKeyPrefix { get; set; } = string.Empty;

    /// <summary>Grows by the total of every order the tenant completes.</summary>
    public long BudgetCents { get; set; }

    public bool IsActive { get; set; } = true;
    public DateTime CreatedAt { get; set; }

    public List<Order> Orders { get; set; } = [];
}
