using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record CategoryRequest(
    [property: Required, StringLength(100, MinimumLength = 1)] string Name);

public record CategoryResponse(Guid Id, string Name)
{
    public static readonly Expression<Func<DrinkCategory, CategoryResponse>> Projection =
        c => new CategoryResponse(c.Id, c.Name);

    public static CategoryResponse From(DrinkCategory c) => new(c.Id, c.Name);
}
