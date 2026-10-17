using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record DrinkRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: StringLength(1000)] string? Description,
    [property: StringLength(500)] string? ImageUrl,
    Guid? CategoryId,
    [property: Range(0d, 100_000_000d)] long PriceCents,
    bool IsAvailable = true,
    List<Guid>? ToppingIds = null,
    List<IngredientDto>? Ingredients = null);

public record DrinkResponse(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    Guid? CategoryId,
    long PriceCents,
    bool IsAvailable,
    DateTime CreatedAt,
    List<Guid> ToppingIds,
    List<IngredientDto> Ingredients)
{
    public static readonly Expression<Func<Drink, DrinkResponse>> Projection =
        d => new DrinkResponse(
            d.Id, d.Name, d.Description, d.ImageUrl, d.CategoryId, d.PriceCents, d.IsAvailable, d.CreatedAt,
            d.DrinkToppings.Select(dt => dt.ToppingId).ToList(),
            d.Ingredients.Select(i => new IngredientDto(i.ProductId, i.Quantity)).ToList());

    /// <summary>Requires <see cref="Drink.DrinkToppings"/> and <see cref="Drink.Ingredients"/> to be loaded.</summary>
    public static DrinkResponse From(Drink d) =>
        new(d.Id, d.Name, d.Description, d.ImageUrl, d.CategoryId, d.PriceCents, d.IsAvailable, d.CreatedAt,
            d.DrinkToppings.Select(dt => dt.ToppingId).ToList(),
            d.Ingredients.Select(i => new IngredientDto(i.ProductId, i.Quantity)).ToList());
}
