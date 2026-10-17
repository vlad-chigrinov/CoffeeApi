using System.ComponentModel.DataAnnotations;
using System.Linq.Expressions;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record OrderItemRequest(
    Guid DrinkId,
    [property: Range(1, 100)] int Quantity,
    List<Guid>? ToppingIds = null);

public record CreateOrderRequest(
    [property: Required, MinLength(1), MaxLength(50)] List<OrderItemRequest> Items,
    [property: Required, StringLength(Order.CustomerNameMaxLength, MinimumLength = Order.CustomerNameMinLength)] string CustomerName,
    [property: StringLength(500)] string? Comment = null);

public record OrderSummary(
    Guid Id,
    int Number,
    OrderStatus Status,
    long TotalPriceCents,
    string CustomerName,
    string? Comment,
    int ItemCount,
    DateTime CreatedAt,
    DateTime UpdatedAt)
{
    public static readonly Expression<Func<Order, OrderSummary>> Projection =
        o => new OrderSummary(
            o.Id, o.Number, o.Status, o.TotalPriceCents, o.CustomerName, o.Comment,
            o.Items.Sum(i => i.Quantity), o.CreatedAt, o.UpdatedAt);
}

public record OrderItemToppingResponse(Guid ToppingId, string Name, string? ImageUrl, long PriceCents);

public record OrderItemResponse(
    Guid Id,
    Guid DrinkId,
    string DrinkName,
    string? DrinkImageUrl,
    int Quantity,
    long UnitPriceCents,
    List<OrderItemToppingResponse> Toppings);

public record OrderResponse(
    Guid Id,
    int Number,
    OrderStatus Status,
    long TotalPriceCents,
    string CustomerName,
    string? Comment,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    List<OrderItemResponse> Items)
{
    public static readonly Expression<Func<Order, OrderResponse>> Projection =
        o => new OrderResponse(
            o.Id, o.Number, o.Status, o.TotalPriceCents, o.CustomerName, o.Comment, o.CreatedAt, o.UpdatedAt,
            o.Items
                .OrderBy(i => i.Drink.Name)
                .Select(i => new OrderItemResponse(
                    i.Id, i.DrinkId, i.Drink.Name, i.Drink.ImageUrl, i.Quantity, i.UnitPriceCents,
                    i.Toppings
                        .OrderBy(t => t.Topping.Name)
                        .Select(t => new OrderItemToppingResponse(t.ToppingId, t.Topping.Name, t.Topping.ImageUrl, t.PriceCents))
                        .ToList()))
                .ToList());
}
