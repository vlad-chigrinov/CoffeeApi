using System.Security.Claims;

namespace CoffeeApi.Api.Auth;

/// <summary>The tenant authenticated for the current request, or null (e.g. for admin requests).</summary>
public interface ICurrentTenant
{
    Guid? Id { get; }
}

public class HttpContextCurrentTenant(IHttpContextAccessor accessor) : ICurrentTenant
{
    public Guid? Id =>
        accessor.HttpContext?.User.IsInRole(AuthConstants.TenantRole) == true
        && Guid.TryParse(accessor.HttpContext.User.FindFirstValue(ClaimTypes.NameIdentifier), out var id)
            ? id
            : null;
}
