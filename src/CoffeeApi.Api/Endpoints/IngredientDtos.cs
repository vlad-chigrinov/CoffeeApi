using System.ComponentModel.DataAnnotations;
using CoffeeApi.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

/// <summary>One recipe line: a product and how much of it (in the product's base units) one serving uses.</summary>
public record IngredientDto(
    Guid ProductId,
    [property: Range(1, 1_000_000)] int Quantity);

public static class Recipes
{
    /// <summary>Returns a validation problem if a product repeats or does not exist, otherwise null.</summary>
    public static async Task<IResult?> Validate(CoffeeDbContext db, List<IngredientDto> ingredients, CancellationToken ct)
    {
        if (ingredients.Count == 0)
            return null;

        var ids = ingredients.Select(i => i.ProductId).Distinct().ToList();
        string? error = null;

        if (ids.Count != ingredients.Count)
            error = "A product can appear in a recipe only once.";
        else if (await db.Products.CountAsync(p => ids.Contains(p.Id), ct) != ids.Count)
            error = "One or more products do not exist.";

        return error is null
            ? null
            : Results.ValidationProblem(new Dictionary<string, string[]> { ["Ingredients"] = [error] });
    }
}
