using System.Text.Json;

namespace CoffeeApi.Tenant.Api;

/// <summary>An error response from the Coffee API, with the useful parts of its ProblemDetails body.</summary>
public class ApiException(int statusCode, string message, List<Shortage> shortages) : Exception(message)
{
    public int StatusCode { get; } = statusCode;

    /// <summary>Filled when an order was rejected because the warehouse lacks ingredients.</summary>
    public List<Shortage> Shortages { get; } = shortages;

    public static async Task<ApiException> FromResponseAsync(HttpResponseMessage response, JsonSerializerOptions json)
    {
        var message = response.StatusCode switch
        {
            System.Net.HttpStatusCode.Unauthorized => "Сервер не принял имя тенанта или ключ — проверьте настройки CoffeeApi в appsettings.",
            _ => $"Ошибка {(int)response.StatusCode}"
        };
        var shortages = new List<Shortage>();

        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
            var root = document.RootElement;

            if (root.TryGetProperty("errors", out var errors) && errors.ValueKind == JsonValueKind.Object)
            {
                message = string.Join(" ", errors.EnumerateObject()
                    .SelectMany(e => e.Value.EnumerateArray().Select(v => v.GetString())));
            }
            else if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String
                     && response.StatusCode != System.Net.HttpStatusCode.Unauthorized)
            {
                message = title.GetString() ?? message;
            }

            if (root.TryGetProperty("shortages", out var list) && list.ValueKind == JsonValueKind.Array)
                shortages = list.Deserialize<List<Shortage>>(json) ?? [];
        }
        catch (JsonException)
        {
            // not a JSON body: keep the generic message
        }

        return new ApiException((int)response.StatusCode, message, shortages);
    }
}
