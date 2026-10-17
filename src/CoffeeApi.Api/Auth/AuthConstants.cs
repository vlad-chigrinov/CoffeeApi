namespace CoffeeApi.Api.Auth;

public static class AuthConstants
{
    public const string AdminScheme = "AdminApiKey";
    public const string AdminPolicy = "Admin";
    public const string AdminRole = "Admin";
    public const string AdminHeader = "X-Admin-Key";

    public const string TenantScheme = "TenantApiKey";
    public const string TenantPolicy = "Tenant";
    public const string TenantRole = "Tenant";
    public const string TenantNameHeader = "X-Tenant-Name";
    public const string TenantKeyHeader = "X-Api-Key";
}
