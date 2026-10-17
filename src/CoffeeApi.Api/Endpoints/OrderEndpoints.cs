using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Entities;
using CoffeeApi.Api.Services;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Endpoints;

/// <summary>
/// Order endpoints for tenants (X-Tenant-Name + X-Api-Key). A tenant only ever sees its own orders:
/// that is enforced by the global query filter in <see cref="CoffeeDbContext"/>.
/// An order is InProgress until it is completed or cancelled; both are final.
/// Creating an order takes its ingredients from the tenant's warehouse, cancelling it puts them back.
/// </summary>
public static class OrderEndpoints
{
    private const int NumberRetries = 5;

    private record Shortage(Guid ProductId, string ProductName, string Unit, long Required, long Available);

    public static IEndpointRouteBuilder MapOrderEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/orders")
            .WithTags("Orders")
            .RequireAuthorization(AuthConstants.TenantPolicy);

        group.MapGet("/", async (
            CoffeeDbContext db,
            CancellationToken ct,
            OrderStatus? status,
            int page = 1,
            int pageSize = Paging.DefaultPageSize) =>
        {
            if (Paging.Validate(page, pageSize) is { } invalid)
                return invalid;

            var query = db.Orders.AsNoTracking();
            if (status is not null)
                query = query.Where(o => o.Status == status);

            var result = await query
                .OrderByDescending(o => o.CreatedAt)
                .ThenByDescending(o => o.Number)
                .Select(OrderSummary.Projection)
                .ToPagedAsync(page, pageSize, ct);

            return Results.Ok(result);
        });

        group.MapGet("/{id:guid}", async (Guid id, CoffeeDbContext db, CancellationToken ct) =>
            await LoadOrder(db, id, ct) is { } order ? Results.Ok(order) : Results.NotFound());

        group.MapPost("/", CreateOrder);
        group.MapPost("/{id:guid}/complete", Complete);
        group.MapPost("/{id:guid}/cancel", Cancel);

        return app;
    }

    private static async Task<IResult> CreateOrder(
        CreateOrderRequest request, ICurrentTenant currentTenant, CoffeeDbContext db, CancellationToken ct)
    {
        var tenantId = currentTenant.Id!.Value;

        var drinkIds = request.Items.Select(i => i.DrinkId).Distinct().ToList();
        var drinks = await db.Drinks
            .AsNoTracking()
            .Include(d => d.DrinkToppings)
            .Include(d => d.Ingredients)
            .Where(d => drinkIds.Contains(d.Id))
            .ToDictionaryAsync(d => d.Id, ct);

        var toppingIds = request.Items.SelectMany(i => i.ToppingIds ?? new List<Guid>()).Distinct().ToList();
        var toppings = await db.Toppings
            .AsNoTracking()
            .Include(t => t.Ingredients)
            .Where(t => toppingIds.Contains(t.Id))
            .ToDictionaryAsync(t => t.Id, ct);

        var errors = ValidateItems(request.Items, drinks, toppings);
        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        // Prices are copied into the order, so later catalog price changes do not touch existing orders.
        var items = request.Items.Select(i => new OrderItem
        {
            Id = Guid.NewGuid(),
            DrinkId = i.DrinkId,
            Quantity = i.Quantity,
            UnitPriceCents = drinks[i.DrinkId].PriceCents,
            Toppings = [.. (i.ToppingIds ?? []).Select(tid => new OrderItemTopping
            {
                ToppingId = tid,
                PriceCents = toppings[tid].PriceCents
            })]
        }).ToList();

        // What the whole order takes from the warehouse: every serving's drink recipe plus its toppings' recipes.
        var required = new Dictionary<Guid, long>();
        foreach (var item in request.Items)
        {
            var perServing = drinks[item.DrinkId].Ingredients
                .Select(i => (i.ProductId, i.Quantity))
                .Concat((item.ToppingIds ?? []).SelectMany(tid => toppings[tid].Ingredients.Select(i => (i.ProductId, i.Quantity))));

            foreach (var (productId, quantity) in perServing)
                required[productId] = required.GetValueOrDefault(productId) + (long)item.Quantity * quantity;
        }

        if (await FindShortages(db, required, ct) is { Count: > 0 } shortages)
            return ShortageProblem(shortages);

        var now = DateTime.UtcNow;
        var order = new Order
        {
            Id = Guid.NewGuid(),
            TenantId = tenantId,
            Status = OrderStatus.InProgress,
            TotalPriceCents = items.Sum(i => i.Quantity * (i.UnitPriceCents + i.Toppings.Sum(t => t.PriceCents))),
            Comment = string.IsNullOrWhiteSpace(request.Comment) ? null : request.Comment.Trim(),
            CreatedAt = now,
            UpdatedAt = now,
            Items = items,
            Ingredients = [.. required.Select(kv => new OrderIngredient { ProductId = kv.Key, Quantity = kv.Value })]
        };

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        // Each take is guarded in SQL, so two parallel orders can never use the same stock twice.
        foreach (var (productId, quantity) in required)
        {
            if (await StockService.TryTakeAsync(db, tenantId, productId, quantity, ct))
                continue;

            await transaction.RollbackAsync(ct);
            return await FindShortages(db, required, ct) is { Count: > 0 } missing
                ? ShortageProblem(missing)
                : Results.Problem(
                    title: "The warehouse changed while the order was being placed. Try again.",
                    statusCode: StatusCodes.Status409Conflict);
        }

        // The number is tenant-sequential; if two orders race for the same number the unique index rejects one, so retry.
        for (var attempt = 1; ; attempt++)
        {
            order.Number = (await db.Orders.MaxAsync(o => (int?)o.Number, ct) ?? 0) + 1;
            db.Orders.Add(order);
            try
            {
                await db.SaveChangesAsync(ct);
                break;
            }
            catch (DbUpdateException e) when (attempt < NumberRetries && e.InnerException is SqliteException { SqliteErrorCode: 19 })
            {
                db.ChangeTracker.Clear();
            }
        }

        await transaction.CommitAsync(ct);

        return Results.Created($"/api/orders/{order.Id}", await LoadOrder(db, order.Id, ct));
    }

    private static Dictionary<string, string[]> ValidateItems(
        List<OrderItemRequest> items, Dictionary<Guid, Drink> drinks, Dictionary<Guid, Topping> toppings)
    {
        var errors = new Dictionary<string, string[]>();

        for (var n = 0; n < items.Count; n++)
        {
            var item = items[n];

            if (!drinks.TryGetValue(item.DrinkId, out var drink) || !drink.IsAvailable)
            {
                errors[$"Items[{n}].DrinkId"] = ["Drink does not exist or is not available."];
                continue;
            }

            var ids = item.ToppingIds ?? [];
            var problems = new List<string>();

            if (ids.Distinct().Count() != ids.Count)
                problems.Add("Toppings must not repeat; use quantity to order several drinks.");

            foreach (var id in ids.Distinct())
            {
                if (!toppings.TryGetValue(id, out var topping) || !topping.IsAvailable)
                    problems.Add($"Topping {id} does not exist or is not available.");
                else if (drink.DrinkToppings.All(dt => dt.ToppingId != id))
                    problems.Add($"Topping {id} is not allowed for this drink.");
            }

            if (problems.Count > 0)
                errors[$"Items[{n}].ToppingIds"] = [.. problems];
        }

        return errors;
    }

    /// <summary>Compares what an order needs with the tenant's stock; empty if everything is available.</summary>
    private static async Task<List<Shortage>> FindShortages(
        CoffeeDbContext db, Dictionary<Guid, long> required, CancellationToken ct)
    {
        if (required.Count == 0)
            return [];

        var productIds = required.Keys.ToList();
        var available = await db.StockItems
            .AsNoTracking()
            .Where(s => productIds.Contains(s.ProductId))
            .ToDictionaryAsync(s => s.ProductId, s => s.Quantity, ct);

        var missing = required.Where(kv => available.GetValueOrDefault(kv.Key) < kv.Value).Select(kv => kv.Key).ToList();
        if (missing.Count == 0)
            return [];

        var products = await db.Products.AsNoTracking().Where(p => missing.Contains(p.Id)).ToDictionaryAsync(p => p.Id, ct);

        return [.. missing.Select(id => new Shortage(
            id, products[id].Name, products[id].Unit, required[id], available.GetValueOrDefault(id)))];
    }

    private static IResult ShortageProblem(List<Shortage> shortages) =>
        Results.Problem(
            title: "Not enough ingredients in the warehouse.",
            statusCode: StatusCodes.Status409Conflict,
            extensions: new Dictionary<string, object?> { ["shortages"] = shortages });

    private static async Task<IResult> Complete(Guid id, CoffeeDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            return Results.NotFound();
        if (order.Status != OrderStatus.InProgress)
            return NotInProgress(order.Status, "completed");

        order.Status = OrderStatus.Completed;
        order.UpdatedAt = DateTime.UtcNow;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            // Status is a concurrency token, so a parallel request that already completed or cancelled the order
            // makes this fail instead of, say, completing the same order twice.
            await db.SaveChangesAsync(ct);

            // Atomic increment in SQL: concurrent completions of different orders must not lose updates.
            var total = order.TotalPriceCents;
            await db.Tenants
                .Where(t => t.Id == order.TenantId)
                .ExecuteUpdateAsync(s => s.SetProperty(t => t.BudgetCents, t => t.BudgetCents + total), ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ChangedByAnotherRequest();
        }

        return Results.Ok(await LoadOrder(db, id, ct));
    }

    private static async Task<IResult> Cancel(Guid id, CoffeeDbContext db, CancellationToken ct)
    {
        var order = await db.Orders.Include(o => o.Ingredients).FirstOrDefaultAsync(o => o.Id == id, ct);
        if (order is null)
            return Results.NotFound();
        if (order.Status != OrderStatus.InProgress)
            return NotInProgress(order.Status, "cancelled");

        order.Status = OrderStatus.Cancelled;
        order.UpdatedAt = DateTime.UtcNow;

        await using var transaction = await db.Database.BeginTransactionAsync(ct);
        try
        {
            await db.SaveChangesAsync(ct);

            // Give back exactly what this order took (the snapshot), not whatever the recipes say now.
            foreach (var ingredient in order.Ingredients)
                await StockService.AddAsync(db, order.TenantId, ingredient.ProductId, ingredient.Quantity, ct);

            await transaction.CommitAsync(ct);
        }
        catch (DbUpdateConcurrencyException)
        {
            return ChangedByAnotherRequest();
        }

        return Results.Ok(await LoadOrder(db, id, ct));
    }

    private static IResult NotInProgress(OrderStatus status, string action) =>
        Results.Problem(
            title: $"The order is {status}; only an InProgress order can be {action}.",
            statusCode: StatusCodes.Status409Conflict);

    private static IResult ChangedByAnotherRequest() =>
        Results.Problem(
            title: "The order was changed by another request. Reload it and try again.",
            statusCode: StatusCodes.Status409Conflict);

    private static Task<OrderResponse?> LoadOrder(CoffeeDbContext db, Guid id, CancellationToken ct) =>
        db.Orders
            .AsNoTracking()
            .Where(o => o.Id == id)
            .Select(OrderResponse.Projection)
            .FirstOrDefaultAsync(ct);
}
