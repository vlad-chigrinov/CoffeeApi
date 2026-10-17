using System.ComponentModel.DataAnnotations;
using CoffeeApi.Api.Entities;

namespace CoffeeApi.Api.Endpoints;

public record CreateTenantRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name);

public record UpdateTenantRequest(
    [property: Required, StringLength(200, MinimumLength = 1)] string Name,
    bool IsActive);

public record TopUpBudgetRequest(
    [property: Range(1d, 100_000_000_000d)] long AmountCents);

public record TenantResponse(
    Guid Id,
    string Name,
    string ApiKeyPrefix,
    long BudgetCents,
    bool IsActive,
    DateTime CreatedAt)
{
    public static TenantResponse From(Tenant t) =>
        new(t.Id, t.Name, t.ApiKeyPrefix, t.BudgetCents, t.IsActive, t.CreatedAt);
}

/// <summary>Returned only on create and key regeneration: the only time the plaintext key is available.</summary>
public record TenantWithKeyResponse(TenantResponse Tenant, string ApiKey);
