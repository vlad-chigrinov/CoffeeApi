using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record ToppingRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: StringLength(500)] string? ImageUrl,
    [property: Range(0d, 100_000_000d)] long PriceCents,
    bool IsAvailable = true,
    List<IngredientDto>? Ingredients = null);

public record ToppingResponse(
    Guid Id,
    string Name,
    string? ImageUrl,
    long PriceCents,
    bool IsAvailable,
    List<IngredientDto> Ingredients)
{
    public static readonly Expression<Func<Topping, ToppingResponse>> Projection =
        t => new ToppingResponse(
            t.Id, t.Name, t.ImageUrl, t.PriceCents, t.IsAvailable,
            t.Ingredients.Select(i => new IngredientDto(i.ProductId, i.Quantity)).ToList());

    /// <summary>Requires <see cref="Topping.Ingredients"/> to be loaded.</summary>
    public static ToppingResponse From(Topping t) =>
        new(t.Id, t.Name, t.ImageUrl, t.PriceCents, t.IsAvailable,
            t.Ingredients.Select(i => new IngredientDto(i.ProductId, i.Quantity)).ToList());
}
