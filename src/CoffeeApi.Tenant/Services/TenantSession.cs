using CoffeeApi.Tenant.Api;

namespace CoffeeApi.Tenant.Services;

/// <summary>
/// The signed-in tenant as seen by one browser tab (a Blazor circuit). The header shows the budget from here,
/// and any page that changes it (purchase, completed order) calls <see cref="RefreshAsync"/>.
/// </summary>
public class TenantSession(CoffeeApiClient api)
{
    public Me? Me { get; private set; }

    /// <summary>Why <see cref="Me"/> could not be loaded, e.g. wrong key or the API is down.</summary>
    public string? Error { get; private set; }

    public event Action? Changed;

    public async Task RefreshAsync()
    {
        try
        {
            Me = await api.GetMeAsync();
            Error = null;
        }
        catch (ApiException e)
        {
            Me = null;
            Error = e.Message;
        }

        Changed?.Invoke();
    }
}
