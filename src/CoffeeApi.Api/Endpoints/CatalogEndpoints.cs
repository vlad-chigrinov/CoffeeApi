using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

/// <summary>Read-only catalog and account endpoints for tenants (X-Tenant-Name + X-Api-Key).</summary>
public static class CatalogEndpoints
{
    public static IEndpointRouteBuilder MapCatalogEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api")
            .WithTags("Catalog")
            .RequireAuthorization(AuthConstants.TenantPolicy);

        group.MapGet("/me", async (ICurrentTenant tenant, CoffeeDbContext db, CancellationToken ct) =>
            await db.Tenants
                .AsNoTracking()
                .Where(t => t.Id == tenant.Id)
                .Select(t => new MeResponse(t.Id, t.Name, t.BudgetCents))
                .FirstAsync(ct));

        group.MapGet("/categories", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(CategoryResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/drinks", async (
            CoffeeDbContext db,
            CancellationToken ct,
            Guid? categoryId,
            int page = 1,
            int pageSize = Paging.DefaultPageSize) =>
        {
            if (Paging.Validate(page, pageSize) is { } invalid)
                return invalid;

            var query = db.Drinks.AsNoTracking().Where(d => d.IsAvailable);
            if (categoryId is not null)
                query = query.Where(d => d.CategoryId == categoryId);

            var result = await query
                .OrderBy(d => d.Name)
                .Select(DrinkListItem.Projection)
                .ToPagedAsync(page, pageSize, ct);

            return Results.Ok(result);
        });

        group.MapGet("/drinks/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await db.Drinks
                .AsNoTracking()
                .Where(d => d.Id == id && d.IsAvailable)
                .Select(DrinkDetails.Projection)
                .FirstOrDefaultAsync(ct) is { } drink
                ? Results.Ok(drink)
                : Results.NotFound());

        group.MapGet("/drinks/{id:guid}/toppings", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            if (!await db.Drinks.AnyAsync(d => d.Id == id && d.IsAvailable, ct))
                return Results.NotFound();

            var toppings = await db.DrinkToppings
                .AsNoTracking()
                .Where(dt => dt.DrinkId == id && dt.Topping.IsAvailable)
                .Select(dt => dt.Topping)
                .OrderBy(t => t.Name)
                .Select(ToppingResponse.Projection)
                .ToListAsync(ct);

            return Results.Ok(toppings);
        });

        return app;
    }
}
