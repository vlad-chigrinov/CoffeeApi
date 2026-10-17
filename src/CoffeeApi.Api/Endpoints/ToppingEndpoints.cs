using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

public static class ToppingEndpoints
{
    public static IEndpointRouteBuilder MapToppingEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/toppings")
            .WithTags("Toppings")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapGet("/", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Toppings
                .AsNoTracking()
                .OrderBy(t => t.Name)
                .Select(ToppingResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await db.Toppings
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(ToppingResponse.Projection)
                .FirstOrDefaultAsync(ct) is { } topping
                ? Results.Ok(topping)
                : Results.NotFound());

        group.MapPost("/", async (ToppingRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await db.Toppings.AnyAsync(t => t.Name == name, ct))
                return NameTaken(name);

            var ingredients = request.Ingredients ?? [];
            if (await Recipes.Validate(db, ingredients, ct) is { } recipeError)
                return recipeError;

            var topping = new Topping
            {
                Id = Guid.NewGuid(),
                Name = name,
                ImageUrl = request.ImageUrl,
                PriceCents = request.PriceCents,
                IsAvailable = request.IsAvailable,
                Ingredients = [.. ingredients.Select(i => new ToppingIngredient { ProductId = i.ProductId, Quantity = i.Quantity })]
            };

            db.Toppings.Add(topping);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/toppings/{topping.Id}", ToppingResponse.From(topping));
        });

        group.MapPut("/{id:guid}", async (Guid id, ToppingRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var topping = await db.Toppings.Include(t => t.Ingredients).FirstOrDefaultAsync(t => t.Id == id, ct);
            if (topping is null)
                return Results.NotFound();

            var name = request.Name.Trim();
            if (await db.Toppings.AnyAsync(t => t.Name == name && t.Id != id, ct))
                return NameTaken(name);

            var ingredients = request.Ingredients ?? [];
            if (await Recipes.Validate(db, ingredients, ct) is { } recipeError)
                return recipeError;

            topping.Name = name;
            topping.ImageUrl = request.ImageUrl;
            topping.PriceCents = request.PriceCents;
            topping.IsAvailable = request.IsAvailable;

            var wanted = ingredients.ToDictionary(i => i.ProductId, i => i.Quantity);
            topping.Ingredients.RemoveAll(i => !wanted.ContainsKey(i.ProductId));
            foreach (var ingredient in topping.Ingredients)
                ingredient.Quantity = wanted[ingredient.ProductId];
            var have = topping.Ingredients.Select(i => i.ProductId).ToHashSet();
            topping.Ingredients.AddRange(
                wanted.Where(kv => !have.Contains(kv.Key))
                    .Select(kv => new ToppingIngredient { ProductId = kv.Key, Quantity = kv.Value }));

            await db.SaveChangesAsync(ct);

            return Results.Ok(ToppingResponse.From(topping));
        });

        group.MapDelete("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            var topping = await db.Toppings.FirstOrDefaultAsync(t => t.Id == id, ct);
            if (topping is null)
                return Results.NotFound();

            // Orders are tenant-scoped by a query filter; the admin must look across all tenants.
            if (await db.OrderItemToppings.IgnoreQueryFilters().AnyAsync(t => t.ToppingId == id, ct))
            {
                return Results.Problem(
                    title: "Topping is used in orders and cannot be deleted. Mark it unavailable instead (IsAvailable = false).",
                    statusCode: StatusCodes.Status409Conflict);
            }

            db.Toppings.Remove(topping);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static IResult NameTaken(string name) =>
        Results.Problem(title: $"Topping '{name}' already exists.", statusCode: StatusCodes.Status409Conflict);
}
