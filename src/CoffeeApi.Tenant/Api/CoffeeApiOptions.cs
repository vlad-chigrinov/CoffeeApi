using System.ComponentModel.DataAnnotations;

namespace CoffeeApi.Tenant.Api;

/// <summary>Connection to the Coffee API, bound from the "CoffeeApi" section of appsettings.</summary>
public class CoffeeApiOptions
{
    public const string SectionName = "CoffeeApi";

    [Required, Url]
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>The tenant's name, sent as X-Tenant-Name.</summary>
    [Required]
    public string TenantName { get; set; } = string.Empty;

    /// <summary>The tenant's API key, sent as X-Api-Key. It stays on the server: the browser never sees it.</summary>
    [Required]
    public string TenantKey { get; set; } = string.Empty;
}
