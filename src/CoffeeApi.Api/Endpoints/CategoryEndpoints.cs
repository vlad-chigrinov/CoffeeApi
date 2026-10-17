using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

public static class CategoryEndpoints
{
    public static IEndpointRouteBuilder MapCategoryEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/admin/categories")
            .WithTags("Categories")
            .RequireAuthorization(AuthConstants.AdminPolicy);

        group.MapGet("/", async (CoffeeDbContext db, CancellationToken ct) =>
            await db.Categories
                .AsNoTracking()
                .OrderBy(c => c.Name)
                .Select(CategoryResponse.Projection)
                .ToListAsync(ct));

        group.MapGet("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await db.Categories
                .AsNoTracking()
                .Where(c => c.Id == id)
                .Select(CategoryResponse.Projection)
                .FirstOrDefaultAsync(ct) is { } category
                ? Results.Ok(category)
                : Results.NotFound());

        group.MapPost("/", async (CategoryRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var name = request.Name.Trim();
            if (await db.Categories.AnyAsync(c => c.Name == name, ct))
                return NameTaken(name);

            var category = new DrinkCategory { Id = Guid.NewGuid(), Name = name };
            db.Categories.Add(category);
            await db.SaveChangesAsync(ct);

            return Results.Created($"/api/admin/categories/{category.Id}", CategoryResponse.From(category));
        });

        group.MapPut("/{id:guid}", async (Guid id, CategoryRequest request, CoffeeDbContext db, CancellationToken ct) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category is null)
                return Results.NotFound();

            var name = request.Name.Trim();
            if (await db.Categories.AnyAsync(c => c.Name == name && c.Id != id, ct))
                return NameTaken(name);

            category.Name = name;
            await db.SaveChangesAsync(ct);

            return Results.Ok(CategoryResponse.From(category));
        });

        group.MapDelete("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
        {
            var category = await db.Categories.FirstOrDefaultAsync(c => c.Id == id, ct);
            if (category is null)
                return Results.NotFound();

            if (await db.Drinks.AnyAsync(d => d.CategoryId == id, ct))
            {
                return Results.Problem(
                    title: "Category has drinks and cannot be deleted. Move or delete its drinks first.",
                    statusCode: StatusCodes.Status409Conflict);
            }

            db.Categories.Remove(category);
            await db.SaveChangesAsync(ct);

            return Results.NoContent();
        });

        return app;
    }

    private static IResult NameTaken(string name) =>
        Results.Problem(title: $"Category '{name}' already exists.", statusCode: StatusCodes.Status409Conflict);
}
