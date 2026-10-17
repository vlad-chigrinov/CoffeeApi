using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record ProductRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    [property: Required, StringLength(20, MinimumLength = 1)] string Unit,
    [property: Range(1, 1_000_000)] int PackSize,
    [property: Range(0d, 100_000_000d)] long PackPriceCents);

public record ProductResponse(Guid Id, string Name, string Unit, int PackSize, long PackPriceCents)
{
    public static readonly Expression<Func<Product, ProductResponse>> Projection =
        p => new ProductResponse(p.Id, p.Name, p.Unit, p.PackSize, p.PackPriceCents);

    public static ProductResponse From(Product p) => new(p.Id, p.Name, p.Unit, p.PackSize, p.PackPriceCents);
}
