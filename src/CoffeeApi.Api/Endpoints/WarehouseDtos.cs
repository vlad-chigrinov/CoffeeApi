using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record StockItemResponse(Guid ProductId, string ProductName, string Unit, long Quantity)
{
    public static readonly Expression<Func<StockItem, StockItemResponse>> Projection =
        s => new StockItemResponse(s.ProductId, s.Product.Name, s.Product.Unit, s.Quantity);
}

public record PurchaseRequest(
    Guid ProductId,
    [property: Range(1, 10_000)] int Packs);

public record PurchaseResponse(
    Guid Id,
    Guid ProductId,
    string ProductName,
    int Packs,
    long QuantityAdded,
    long TotalPriceCents,
    DateTime CreatedAt)
{
    public static readonly Expression<Func<Purchase, PurchaseResponse>> Projection =
        p => new PurchaseResponse(
            p.Id, p.ProductId, p.Product.Name, p.Packs, p.QuantityAdded, p.TotalPriceCents, p.CreatedAt);
}

/// <summary>A completed purchase together with what is left of the budget.</summary>
public record PurchaseResult(PurchaseResponse Purchase, long BudgetCents);
