using System.Globalization;

namespace CoffeeApi.Tenant.Services;

/// <summary>The API counts money in cents; the UI shows whole units with two decimals.</summary>
public static class Money
{
    public static string Format(long cents) => (cents / 100m).ToString("F2", CultureInfo.InvariantCulture);
}
