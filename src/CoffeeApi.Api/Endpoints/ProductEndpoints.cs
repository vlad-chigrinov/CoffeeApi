using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

public static class ProductEndpoints
{
    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/products")
            .WithTags("Products")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapGet("/", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Products
                .AsNoTracking()
                .OrderBy(p => p.Name)
                .Select(ProductResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await db.Products
                .AsNoTracking()
                .Where(p => p.Id == id)
                .Select(ProductResponse.Projection)
                .FirstOrDefaultAsync(ct) is { } product
                ? Results.Ok(product)
                : Results.NotFound());

        group.MapPost("/", async (ProductRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await db.Products.AnyAsync(p => p.Name == name, ct))
                return NameTaken(name);

            var product = new Product
            {
                Id = Guid.NewGuid(),
                Name = name,
                Unit = request.Unit.Trim(),
                PackSize = request.PackSize,
                PackPriceCents = request.PackPriceCents
            };

            db.Products.Add(product);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/products/{product.Id}", ProductResponse.From(product));
        });

        group.MapPut("/{id:guid}", async (Guid id, ProductRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product is null)
                return Results.NotFound();

            var name = request.Name.Trim();
            if (await db.Products.AnyAsync(p => p.Name == name && p.Id != id, ct))
                return NameTaken(name);

            product.Name = name;
            product.Unit = request.Unit.Trim();
            product.PackSize = request.PackSize;
            product.PackPriceCents = request.PackPriceCents;
            await db.SaveChangesAsync(ct);

            return Results.Ok(ProductResponse.From(product));
        });

        group.MapDelete("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            var product = await db.Products.FirstOrDefaultAsync(p => p.Id == id, ct);
            if (product is null)
                return Results.NotFound();

            // Stock, orders and purchases are tenant-scoped by query filters; the admin must look across all tenants.
            var inUse =
                await db.DrinkIngredients.AnyAsync(i => i.ProductId == id, ct)
                || await db.ToppingIngredients.AnyAsync(i => i.ProductId == id, ct)
                || await db.StockItems.IgnoreQueryFilters().AnyAsync(s => s.ProductId == id, ct)
                || await db.OrderIngredients.IgnoreQueryFilters().AnyAsync(i => i.ProductId == id, ct)
                || await db.Purchases.IgnoreQueryFilters().AnyAsync(p => p.ProductId == id, ct);

            if (inUse)
            {
                return Results.Problem(
                    title: "Product is used in recipes, warehouses or order history and cannot be deleted.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            db.Products.Remove(product);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static IResult NameTaken(string name) =>
        Results.Problem(title: $"Product '{name}' already exists.", statusCode: StatusCodes.Status409Conflict);
}
