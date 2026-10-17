namespace CoffeeApi.Tenant.Api;

// Mirrors the JSON contract of the tenant endpoints of the Coffee API.

public enum OrderStatus
{
    InProgress,
    Completed,
    Cancelled
}

public record Me(Guid Id, string Name, long BudgetCents);

public record Category(Guid Id, string Name);

public record Paged<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public record DrinkListItem(
    Guid Id, string Name, string? ImageUrl, long PriceCents, Guid? CategoryId, string? CategoryName);

public record DrinkDetails(
    Guid Id, string Name, string? Description, string? ImageUrl, long PriceCents,
    Guid? CategoryId, string? CategoryName, DateTime CreatedAt);

public record Ingredient(Guid ProductId, int Quantity);

public record Topping(Guid Id, string Name, string? ImageUrl, long PriceCents, bool IsAvailable, List<Ingredient> Ingredients);

public record OrderSummary(
    Guid Id, int Number, OrderStatus Status, long TotalPriceCents, string CustomerName, string? Comment,
    int ItemCount, DateTime CreatedAt, DateTime UpdatedAt);

public record OrderItemTopping(Guid ToppingId, string Name, string? ImageUrl, long PriceCents);

public record OrderItem(
    Guid Id, Guid DrinkId, string DrinkName, string? DrinkImageUrl, int Quantity, long UnitPriceCents, List<OrderItemTopping> Toppings);

public record Order(
    Guid Id, int Number, OrderStatus Status, long TotalPriceCents, string CustomerName, string? Comment,
    DateTime CreatedAt, DateTime UpdatedAt, List<OrderItem> Items);

public record NewOrderItem(Guid DrinkId, int Quantity, List<Guid> ToppingIds);

public record NewOrder(List<NewOrderItem> Items, string CustomerName, string? Comment);

public record Product(Guid Id, string Name, string Unit, int PackSize, long PackPriceCents);

public record StockItem(Guid ProductId, string ProductName, string Unit, long Quantity);

public record Purchase(
    Guid Id, Guid ProductId, string ProductName, int Packs, long QuantityAdded, long TotalPriceCents, DateTime CreatedAt);

public record PurchaseResult(Purchase Purchase, long BudgetCents);

/// <summary>One missing ingredient, reported when an order cannot be placed.</summary>
public record Shortage(Guid ProductId, string ProductName, string Unit, long Required, long Available);
