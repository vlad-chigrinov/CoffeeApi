using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using CoffeeApi.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

/// <summary>The tenant's warehouse: product catalog, stock and purchases (X-Tenant-Name + X-Api-Key).</summary>
public static class WarehouseEndpoints
{
    public static IEndpointRouteBuilder MapWarehouseEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Warehouse")
            .RequireAuthorization(AuthConstants.TenantPolicy);

        group.MapGet("/products", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(ProductResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/stock", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.StockItems
                .AsNoTracking()
                .OrderBy(s => s.Product.Name)
                .Select(StockItemResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/purchases", async (
            CoffeeDbContext db,
            CancellationToken ct,
            int page = 1,
            int pageSize = Paging.DefaultPageSize) =>
        {
            if (Paging.Validate(page, pageSize) is { } invalid)
                return invalid;

            var result = await db.Purchases
                .AsNoTracking()
                .OrderByDescending(p => p.CreatedAt)
                .Select(PurchaseResponse.Projection)
                .ToPagedAsync(page, pageSize, ct);

            return Results.Ok(result);
        });

        group.MapPost("/purchases", Purchase);

        return app;
    }

    private static async Task<IResult> Purchase(
        PurchaseRequest request, ICurrentTenant currentTenant, CoffeeDbContext db, CancellationToken ct)
    {
        var product = await db.Products.AsNoTracking().FirstOrDefaultAsync(p => p.Id == request.ProductId, ct);
        if (product is null)
        {
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(PurchaseRequest.ProductId)] = ["Product does not exist."]
            });
        }

        var tenantId = currentTenant.Id!.Value;
        var quantityAdded = (long)request.Packs * product.PackSize;
        var cost = request.Packs * product.PackPriceCents;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Guarded in SQL: the budget is only reduced if it covers the cost, even with parallel requests.
        var paid = await db.Tenants
            .Where(t => t.Id == tenantId && t.BudgetCents >= cost)
            .ExecuteUpdateAsync(s => s.SetProperty(t => t.BudgetCents, t => t.BudgetCents - cost), ct);

        if (paid == 0)
        {
            await transaction.RollbackAsync(ct);
            var budget = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.BudgetCents).FirstAsync(ct);
            return Results.Problem(
                title: "Not enough budget for this purchase.",
                statusCode: StatusCodes.Status409Conflict,
                extensions: new Dictionary<string, object?> { ["budgetCents"] = budget, ["requiredCents"] = cost });
        }

        await StockService.AddAsync(db, tenantId, product.Id, quantityAdded, ct);

        var purchase = new Purchase
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            ProductId = product.Id,
            Packs = request.Packs,
            QuantityAdded = quantityAdded,
            TotalPriceCents = cost,
            CreatedAt = DateTime.UtcNow
        };
        db.Purchases.Add(purchase);
        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        var budgetLeft = await db.Tenants.AsNoTracking().Where(t => t.Id == tenantId).Select(t => t.BudgetCents).FirstAsync(ct);
        var response = new PurchaseResponse(
            purchase.Id, product.Id, product.Name, purchase.Packs, purchase.QuantityAdded, purchase.TotalPriceCents, purchase.CreatedAt);

        return Results.Created($"/api/purchases/{purchase.Id}", new PurchaseResult(response, budgetLeft));
    }
}
