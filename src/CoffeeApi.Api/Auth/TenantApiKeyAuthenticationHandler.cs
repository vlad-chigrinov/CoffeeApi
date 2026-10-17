using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoffeeApi.Api.Auth;

/// <summary>
/// Authenticates a tenant by <c>X-Tenant-Name</c> + <c>X-Api-Key</c>: the key must belong to the named, active tenant.
/// </summary>
public class TenantApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    CoffeeDbContext db)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    // Same message for every failure so the response does not reveal which tenant names exist.
    private const string InvalidCredentials = "Invalid tenant name or API key.";

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var hasName = Request.Headers.TryGetValue(AuthConstants.TenantNameHeader, out var name) && name.Count > 0;
        var hasKey = Request.Headers.TryGetValue(AuthConstants.TenantKeyHeader, out var key) && key.Count > 0;

        if (!hasName && !hasKey)
            return AuthenticateResult.NoResult();
        if (!hasName || !hasKey)
            return AuthenticateResult.Fail(InvalidCredentials);

        var tenant = await db.Tenants
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.Name == name.ToString(), Context.RequestAborted);

        // Hash even when the tenant does not exist, so unknown names and wrong keys take similar time.
        var providedHash = ApiKeyService.Hash(key.ToString());
        var expectedHash = tenant?.ApiKeyHash ?? new string('0', providedHash.Length);

        var keyMatches = CryptographicOperations.FixedTimeEquals(
            Encoding.UTF8.GetBytes(providedHash),
            Encoding.UTF8.GetBytes(expectedHash));

        if (tenant is null || !keyMatches || !tenant.IsActive)
            return AuthenticateResult.Fail(InvalidCredentials);

        var identity = new ClaimsIdentity(
            [
                new Claim(ClaimTypes.NameIdentifier, tenant.Id.ToString()),
                new Claim(ClaimTypes.Name, tenant.Name),
                new Claim(ClaimTypes.Role, AuthConstants.TenantRole)
            ],
            Scheme.Name);

        return AuthenticateResult.Success(new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name));
    }
}
