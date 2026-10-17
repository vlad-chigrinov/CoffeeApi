namespace CoffeeApi.Api.Data.Seeding;

/// <summary>Configuration section <c>Seed</c>: demo data put into an empty database on startup.</summary>
public class SeedOptions
{
    public const string SectionName = "Seed";

    /// <summary>Turn the demo data off (e.g. <c>Seed__Enabled=false</c>) when the database must start truly empty.</summary>
    public bool Enabled { get; set; } = true;

    /// <summary>
    /// Optional demo tenant. When both name and key are set, the seed also creates this tenant, so a fresh
    /// database comes with a working tenant plus a warehouse and orders. Without it only the shared catalog
    /// (categories, ingredients, toppings, drinks) is seeded, because a tenant's key cannot be invented here:
    /// it is shown once, when the admin creates the tenant.
    /// </summary>
    public SeedTenantOptions? Tenant { get; set; }
}

public class SeedTenantOptions
{
    public string? Name { get; set; }

    /// <summary>The plain key; only its hash is stored, as for every tenant.</summary>
    public string? ApiKey { get; set; }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(Name) && !string.IsNullOrWhiteSpace(ApiKey);
}
