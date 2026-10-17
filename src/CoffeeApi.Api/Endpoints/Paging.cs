using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

public record PagedResponse<T>(List<T> Items, int Page, int PageSize, int TotalCount, int TotalPages);

public static class Paging
{
    public const int DefaultPageSize = 20;
    public const int MaxPageSize = 100;

    /// <summary>Returns a validation problem for out-of-range paging parameters, otherwise null.</summary>
    public static IResult? Validate(int page, int pageSize)
    {
        var errors = new Dictionary<string, string[]>();
        if (page < 1)
            errors["page"] = ["page must be 1 or greater."];
        if (pageSize is < 1 or > MaxPageSize)
            errors["pageSize"] = [$"pageSize must be between 1 and {MaxPageSize}."];

        return errors.Count == 0 ? null : Results.ValidationProblem(errors);
    }

    /// <summary>The query must already be ordered, otherwise pages are not stable.</summary>
    public static async Task<PagedResponse<T>> ToPagedAsync<T>(
        this IQueryable<T> query, int page, int pageSize, CancellationToken ct)
    {
        var total = await query.CountAsync(ct);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return new PagedResponse<T>(items, page, pageSize, total, (int)Math.Ceiling(total / (double)pageSize));
    }
}
