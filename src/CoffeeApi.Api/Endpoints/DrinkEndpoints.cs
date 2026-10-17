using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

public static class DrinkEndpoints
{
    public static IEndpointRouteBuilder MapDrinkEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/drinks")
            .WithTags("Drinks")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapGet("/", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Drinks
                .AsNoTracking()
                .OrderBy(d => d.Name)
                .Select(DrinkResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await db.Drinks
                .AsNoTracking()
                .Where(d => d.Id == id)
                .Select(DrinkResponse.Projection)
                .FirstOrDefaultAsync(ct) is { } drink
                ? Results.Ok(drink)
                : Results.NotFound());

        group.MapPost("/", async (DrinkRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await db.Drinks.AnyAsync(d => d.Name == name, ct))
                return NameTaken(name);

            var toppingIds = request.ToppingIds?.Distinct().ToList() ?? [];
            if (await MissingCategory(db, request.CategoryId, ct) is { } categoryError)
                return categoryError;
            if (await MissingToppings(db, toppingIds, ct) is { } error)
                return error;
            var ingredients = request.Ingredients ?? [];
            if (await Recipes.Validate(db, ingredients, ct) is { } recipeError)
                return recipeError;

            var drink = new Drink
            {
                Id = Guid.NewGuid(),
                Name = name,
                Description = request.Description,
                ImageUrl = request.ImageUrl,
                CategoryId = request.CategoryId,
                PriceCents = request.PriceCents,
                IsAvailable = request.IsAvailable,
                CreatedAt = DateTime.UtcNow,
                DrinkToppings = [.. toppingIds.Select(tid => new DrinkTopping { ToppingId = tid })],
                Ingredients = [.. ingredients.Select(i => new DrinkIngredient { ProductId = i.ProductId, Quantity = i.Quantity })]
            };

            db.Drinks.Add(drink);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/drinks/{drink.Id}", DrinkResponse.From(drink));
        });

        group.MapPut("/{id:guid}", async (Guid id, DrinkRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var drink = await db.Drinks
                .Include(d => d.DrinkToppings)
                .Include(d => d.Ingredients)
                .FirstOrDefaultAsync(d => d.Id == id, ct);
            if (drink is null)
                return Results.NotFound();

            var name = request.Name.Trim();
            if (await db.Drinks.AnyAsync(d => d.Name == name && d.Id != id, ct))
                return NameTaken(name);

            var toppingIds = request.ToppingIds?.Distinct().ToList() ?? [];
            if (await MissingCategory(db, request.CategoryId, ct) is { } categoryError)
                return categoryError;
            if (await MissingToppings(db, toppingIds, ct) is { } error)
                return error;
            var ingredients = request.Ingredients ?? [];
            if (await Recipes.Validate(db, ingredients, ct) is { } recipeError)
                return recipeError;

            drink.Name = name;
            drink.Description = request.Description;
            drink.ImageUrl = request.ImageUrl;
            drink.CategoryId = request.CategoryId;
            drink.PriceCents = request.PriceCents;
            drink.IsAvailable = request.IsAvailable;

            drink.DrinkToppings.RemoveAll(dt => !toppingIds.Contains(dt.ToppingId));
            var existing = drink.DrinkToppings.Select(dt => dt.ToppingId).ToHashSet();
            drink.DrinkToppings.AddRange(
                toppingIds.Where(tid => !existing.Contains(tid)).Select(tid => new DrinkTopping { ToppingId = tid }));

            var wanted = ingredients.ToDictionary(i => i.ProductId, i => i.Quantity);
            drink.Ingredients.RemoveAll(i => !wanted.ContainsKey(i.ProductId));
            foreach (var ingredient in drink.Ingredients)
                ingredient.Quantity = wanted[ingredient.ProductId];
            var have = drink.Ingredients.Select(i => i.ProductId).ToHashSet();
            drink.Ingredients.AddRange(
                wanted.Where(kv => !have.Contains(kv.Key))
                    .Select(kv => new DrinkIngredient { ProductId = kv.Key, Quantity = kv.Value }));

            await db.SaveChangesAsync(ct);

            return Results.Ok(DrinkResponse.From(drink));
        });

        group.MapDelete("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            var drink = await db.Drinks.FirstOrDefaultAsync(d => d.Id == id, ct);
            if (drink is null)
                return Results.NotFound();

            // Orders are tenant-scoped by a query filter; the admin must look across all tenants.
            if (await db.OrderItems.IgnoreQueryFilters().AnyAsync(i => i.DrinkId == id, ct))
            {
                return Results.Problem(
                    title: "Drink is used in orders and cannot be deleted. Mark it unavailable instead (IsAvailable = false).",
                    statusCode: StatusCodes.Status409Conflict);
            }

            db.Drinks.Remove(drink);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static IResult NameTaken(string name) =>
        Results.Problem(title: $"Drink '{name}' already exists.", statusCode: StatusCodes.Status409Conflict);

    /// <summary>Returns a validation problem if the category id is set but does not exist, otherwise null.</summary>
    private static async Task<IResult?> MissingCategory(CoffeeDbContext db, Guid? categoryId, CancellationToken ct) =>
        categoryId is null || await db.Categories.AnyAsync(c => c.Id == categoryId, ct)
            ? null
            : Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(DrinkRequest.CategoryId)] = ["Category does not exist."]
            });

    /// <summary>Returns a validation problem if any of the topping ids does not exist, otherwise null.</summary>
    private static async Task<IResult?> MissingToppings(CoffeeDbContext db, List<Guid> toppingIds, CancellationToken ct)
    {
        if (toppingIds.Count == 0)
            return null;

        var found = await db.Toppings.CountAsync(t => toppingIds.Contains(t.Id), ct);
        return found == toppingIds.Count
            ? null
            : Results.ValidationProblem(new Dictionary<string, string[]>
            {
                [nameof(DrinkRequest.ToppingIds)] = ["One or more toppings do not exist."]
            });
    }
}
