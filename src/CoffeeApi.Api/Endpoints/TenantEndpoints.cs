using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using CoffeeApi.Api.Services;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

public static class TenantEndpoints
{
    public static IEndpointRouteBuilder MapTenantEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/tenants")
            .WithTags("Tenants")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapGet("/", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Tenants
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(t => new TenantResponse(t.Id, t.Name, t.ApiKeyPrefix, t.BudgetCents, t.IsActive, t.CreatedAt))
                .ToListAsync(ct));

        group.MapGet("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await db.Tenants.AsNoTracking().FirstOrDefaultAsync(t => t.Id == id, ct) is { } tenant
                ? Results.Ok(TenantResponse.From(tenant))
                : Results.NotFound());

        group.MapPost("/", async (CreateTenantRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await db.Tenants.AnyAsync(t => t.Name == name, ct))
                return NameTaken(name);

            var apiKey = ApiKeyService.Generate();
            var tenant = new Tenant
            {
                Id = Guid.NewGuid(),
                Name = name,
                ApiKeyHash = apiKey.Hash,
                ApiKeyPrefix = apiKey.Prefix,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };

            db.Tenants.Add(tenant);
            await db.SaveChangesAsync(ct);

            return Results.Created(
                $"/api/admin/tenants/{tenant.Id}",
                new TenantWithKeyResponse(TenantResponse.From(tenant), apiKey.Key));
        });

        group.MapPut("/{id:guid}", async (Guid id, UpdateTenantRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tenant is null)
                return Results.NotFound();

            var name = request.Name.Trim();
            if (await db.Tenants.AnyAsync(t => t.Name == name && t.Id != id, ct))
                return NameTaken(name);

            tenant.Name = name;
            tenant.IsActive = request.IsActive;
            await db.SaveChangesAsync(ct);

            return Results.Ok(TenantResponse.From(tenant));
        });

        group.MapPost("/{id:guid}/regenerate-key", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tenant is null)
                return Results.NotFound();

            var apiKey = ApiKeyService.Generate();
            tenant.ApiKeyHash = apiKey.Hash;
            tenant.ApiKeyPrefix = apiKey.Prefix;
            await db.SaveChangesAsync(ct);

            return Results.Ok(new TenantWithKeyResponse(TenantResponse.From(tenant), apiKey.Key));
        });

        group.MapPost("/{id:guid}/budget/top-up", async (Guid id, TopUpBudgetRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            // Atomic increment in SQL, so it cannot race with the budget changes made by orders and purchases.
            var updated = await db.Tenants
                .Where(t => t.Id == id)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.BudgetCents, t => t.BudgetCents + request.AmountCents), ct);
            if (updated == 0)
                return Results.NotFound();

            var tenant = await db.Tenants.AsNoTracking().FirstAsync(t => t.Id == id, ct);
            return Results.Ok(TenantResponse.From(tenant));
        });

        group.MapDelete("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            var tenant = await db.Tenants.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (tenant is null)
                return Results.NotFound();

            // Orders, stock and purchases are tenant-scoped by query filters; the admin must look across all tenants.
            if (await db.Orders.IgnoreQueryFilters().AnyAsync(o => o.TenantId == id, ct)
                || await db.StockItems.IgnoreQueryFilters().AnyAsync(s => s.TenantId == id, ct)
                || await db.Purchases.IgnoreQueryFilters().AnyAsync(p => p.TenantId == id, ct))
            {
                return Results.Problem(
                    title: "Tenant has orders, stock or purchases and cannot be deleted. Deactivate it instead (IsActive = false).",
                    statusCode: StatusCodes.Status409Conflict);
            }

            db.Tenants.Remove(tenant);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static IResult NameTaken(string name) =>
        Results.Problem(title: $"Tenant '{name}' already exists.", statusCode: StatusCodes.Status409Conflict);
}
