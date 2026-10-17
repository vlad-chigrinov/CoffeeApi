using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

/// <summary>Tenant view of the current tenant, including the budget.</summary>
public record MeResponse(Guid Id, string Name, long BudgetCents);

public record DrinkListItem(
    Guid Id,
    string Name,
    string? ImageUrl,
    long PriceCents,
    Guid? CategoryId,
    string? CategoryName)
{
    public static readonly Expression<Func<Drink, DrinkListItem>> Projection =
        d => new DrinkListItem(
            d.Id, d.Name, d.ImageUrl, d.PriceCents, d.CategoryId, d.Category == null ? null : d.Category.Name);
}

public record DrinkDetails(
    Guid Id,
    string Name,
    string? Description,
    string? ImageUrl,
    long PriceCents,
    Guid? CategoryId,
    string? CategoryName,
    DateTime CreatedAt)
{
    public static readonly Expression<Func<Drink, DrinkDetails>> Projection =
        d => new DrinkDetails(
            d.Id, d.Name, d.Description, d.ImageUrl, d.PriceCents,
            d.CategoryId, d.Category == null ? null : d.Category.Name, d.CreatedAt);
}
