using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.Extensions.Options;

namespace CoffeeApi.Api.Auth;

/// <summary>
/// Authenticates the admin by the <c>X-Admin-Key</c> header against <c>Admin:ApiKey</c> from configuration.
/// Fails closed: if the key is not configured, nobody is authenticated.
/// </summary>
public class AdminApiKeyAuthenticationHandler(
    IOptionsMonitor<AuthenticationSchemeOptions> options,
    ILoggerFactory logger,
    UrlEncoder encoder,
    IConfiguration configuration)
    : AuthenticationHandler<AuthenticationSchemeOptions>(options, logger, encoder)
{
    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue(AuthConstants.AdminHeader, out var provided) || provided.Count == 0)
            return Task.FromResult(AuthenticateResult.NoResult());

        var expected = configuration["Admin:ApiKey"];
        if (string.IsNullOrEmpty(expected) || !FixedTimeEquals(expected, provided.ToString()))
            return Task.FromResult(AuthenticateResult.Fail("Invalid admin key."));

        var identity = new ClaimsIdentity(
            [new Claim(ClaimTypes.Name, "admin"), new Claim(ClaimTypes.Role, AuthConstants.AdminRole)],
            Scheme.Name);
        var ticket = new AuthenticationTicket(new ClaimsPrincipal(identity), Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    private static bool FixedTimeEquals(string a, string b) =>
        CryptographicOperations.FixedTimeEquals(
            SHA256.HashData(Encoding.UTF8.GetBytes(a)),
            SHA256.HashData(Encoding.UTF8.GetBytes(b)));
}
