using CoffeeApi.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Services;

/// <summary>
/// Atomic warehouse operations. Both run as a single SQL statement, so concurrent orders and purchases
/// cannot oversell stock or lose updates. Call them inside the caller's transaction.
/// </summary>
public static class StockService
{
    /// <summary>Takes <paramref name="quantity"/> from the tenant's stock; false (nothing changed) if there is not enough.</summary>
    public static async Task<bool> TryTakeAsync(
        CoffeeDbContext db, Guid tenantId, Guid productId, long quantity, CancellationToken ct) =>
        await db.StockItems
            .Where(s => s.TenantId == tenantId && s.ProductId == productId && s.Quantity >= quantity)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.Quantity, x => x.Quantity - quantity), ct) == 1;

    /// <summary>Adds <paramref name="quantity"/> to the tenant's stock, creating the line on first use.</summary>
    public static Task AddAsync(
        CoffeeDbContext db, Guid tenantId, Guid productId, long quantity, CancellationToken ct) =>
        db.Database.ExecuteSqlInterpolatedAsync(
            $"""
             INSERT INTO "StockItems" ("TenantId", "ProductId", "Quantity")
             VALUES ({tenantId}, {productId}, {quantity})
             ON CONFLICT ("TenantId", "ProductId") DO UPDATE SET "Quantity" = "Quantity" + excluded."Quantity"
             """,
            ct);
}
