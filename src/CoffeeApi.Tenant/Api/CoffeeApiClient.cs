using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CoffeeApi.Tenant.Api;

/// <summary>Typed client for the tenant endpoints. Authentication headers are added once, in <c>Program.cs</c>.</summary>
public class CoffeeApiClient(HttpClient http)
{
    private static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public Task<Me> GetMeAsync(CancellationToken ct = default) => GetAsync<Me>("/api/me", ct);

    public Task<List<Category>> GetCategoriesAsync(CancellationToken ct = default) =>
        GetAsync<List<Category>>("/api/categories", ct);

    public Task<Paged<DrinkListItem>> GetDrinksAsync(Guid? categoryId, int page, int pageSize, CancellationToken ct = default) =>
        GetAsync<Paged<DrinkListItem>>(
            $"/api/drinks?page={page}&pageSize={pageSize}" + (categoryId is null ? "" : $"&categoryId={categoryId}"), ct);

    public Task<DrinkDetails> GetDrinkAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<DrinkDetails>($"/api/drinks/{id}", ct);

    public Task<List<Topping>> GetDrinkToppingsAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<List<Topping>>($"/api/drinks/{id}/toppings", ct);

    public Task<Paged<OrderSummary>> GetOrdersAsync(OrderStatus? status, int page, int pageSize, CancellationToken ct = default) =>
        GetAsync<Paged<OrderSummary>>(
            $"/api/orders?page={page}&pageSize={pageSize}" + (status is null ? "" : $"&status={status}"), ct);

    public Task<Order> GetOrderAsync(Guid id, CancellationToken ct = default) =>
        GetAsync<Order>($"/api/orders/{id}", ct);

    public Task<Order> CreateOrderAsync(NewOrder order, CancellationToken ct = default) =>
        PostAsync<Order>("/api/orders", order, ct);

    public Task<Order> CompleteOrderAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<Order>($"/api/orders/{id}/complete", null, ct);

    public Task<Order> CancelOrderAsync(Guid id, CancellationToken ct = default) =>
        PostAsync<Order>($"/api/orders/{id}/cancel", null, ct);

    public Task<List<Product>> GetProductsAsync(CancellationToken ct = default) =>
        GetAsync<List<Product>>("/api/products", ct);

    public Task<List<StockItem>> GetStockAsync(CancellationToken ct = default) =>
        GetAsync<List<StockItem>>("/api/stock", ct);

    public Task<PurchaseResult> PurchaseAsync(Guid productId, int packs, CancellationToken ct = default) =>
        PostAsync<PurchaseResult>("/api/purchases", new { productId, packs }, ct);

    public Task<Paged<Purchase>> GetPurchasesAsync(int page, int pageSize, CancellationToken ct = default) =>
        GetAsync<Paged<Purchase>>($"/api/purchases?page={page}&pageSize={pageSize}", ct);

    private async Task<T> GetAsync<T>(string url, CancellationToken ct)
    {
        using var response = await SendAsync(() => http.GetAsync(url, ct));
        return await ReadAsync<T>(response, ct);
    }

    private async Task<T> PostAsync<T>(string url, object? body, CancellationToken ct)
    {
        using var response = await SendAsync(() => http.PostAsJsonAsync(url, body, Json, ct));
        return await ReadAsync<T>(response, ct);
    }

    private static async Task<HttpResponseMessage> SendAsync(Func<Task<HttpResponseMessage>> send)
    {
        HttpResponseMessage response;
        try
        {
            response = await send();
        }
        catch (HttpRequestException e)
        {
            throw new ApiException(0, $"Не удалось связаться с сервером API: {e.Message}", []);
        }

        if (!response.IsSuccessStatusCode)
        {
            var error = await ApiException.FromResponseAsync(response, Json);
            response.Dispose();
            throw error;
        }

        return response;
    }

    private static async Task<T> ReadAsync<T>(HttpResponseMessage response, CancellationToken ct) =>
        await response.Content.ReadFromJsonAsync<T>(Json, ct)
        ?? throw new ApiException((int)response.StatusCode, "Пустой ответ сервера.", []);
}
