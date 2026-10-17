using CoffeeApi.Api.Entities;
using CoffeeApi.Api.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace CoffeeApi.Api.Data.Seeding;

/// <summary>
/// Fills an empty database with demo data on startup (after the migrations). "Empty" means no catalog and no
/// tenants at all, so a database someone has already started to fill is never touched. Everything goes in
/// one transaction: a failed seed leaves the database empty and the next start tries again.
/// </summary>
public class DatabaseSeeder(CoffeeDbContext db, IOptions<SeedOptions> options, ILogger<DatabaseSeeder> logger)
{
    public async Task SeedAsync(CancellationToken ct = default)
    {
        var seed = options.Value;
        if (!seed.Enabled)
        {
            logger.LogInformation("Seeding is disabled (Seed:Enabled = false).");
            return;
        }

        if (await db.Categories.AnyAsync(ct) || await db.Products.AnyAsync(ct) || await db.Drinks.AnyAsync(ct)
            || await db.Toppings.AnyAsync(ct) || await db.Tenants.AnyAsync(ct))
        {
            logger.LogDebug("The database is not empty, nothing to seed.");
            return;
        }

        await using var transaction = await db.Database.BeginTransactionAsync(ct);

        var now = DateTime.UtcNow;
        var catalog = AddCatalog(now);

        if (seed.Tenant is { IsConfigured: true } tenant)
            AddTenantData(tenant, catalog, now);

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        logger.LogInformation(
            "Seeded the demo catalog ({Drinks} drinks, {Toppings} toppings){TenantInfo}.",
            catalog.Drinks.Count, catalog.Toppings.Count,
            seed.Tenant is { IsConfigured: true } ? $" and the demo tenant \"{seed.Tenant.Name}\"" : "");
    }

    private record Catalog(
        Dictionary<string, Product> Products,
        Dictionary<string, Topping> Toppings,
        Dictionary<string, Drink> Drinks);

    private Catalog AddCatalog(DateTime now)
    {
        var categories = DemoData.Categories.ToDictionary(
            name => name, name => new DrinkCategory { Id = Guid.NewGuid(), Name = name });

        var products = DemoData.Products.ToDictionary(p => p.Name, p => new Product
        {
            Id = Guid.NewGuid(),
            Name = p.Name,
            Unit = p.Unit,
            PackSize = p.PackSize,
            PackPriceCents = p.PackPriceCents
        });

        var toppings = DemoData.Toppings.ToDictionary(t => t.Name, t => new Topping
        {
            Id = Guid.NewGuid(),
            Name = t.Name,
            ImageUrl = t.ImageUrl,
            PriceCents = t.PriceCents,
            IsAvailable = t.IsAvailable,
            Ingredients = [.. t.Recipe.Select(i => new ToppingIngredient
            {
                ProductId = products[i.Product].Id,
                Quantity = i.Quantity
            })]
        });

        var drinks = DemoData.Drinks.ToDictionary(d => d.Name, d => new Drink
        {
            Id = Guid.NewGuid(),
            Name = d.Name,
            Description = d.Description,
            ImageUrl = d.ImageUrl,
            CategoryId = categories[d.Category].Id,
            PriceCents = d.PriceCents,
            IsAvailable = d.IsAvailable,
            CreatedAt = now,
            DrinkToppings = [.. d.Toppings.Select(name => new DrinkTopping { ToppingId = toppings[name].Id })],
            Ingredients = [.. d.Recipe.Select(i => new DrinkIngredient
            {
                ProductId = products[i.Product].Id,
                Quantity = i.Quantity
            })]
        });

        db.Categories.AddRange(categories.Values);
        db.Products.AddRange(products.Values);
        db.Toppings.AddRange(toppings.Values);
        db.Drinks.AddRange(drinks.Values);

        return new Catalog(products, toppings, drinks);
    }

    /// <summary>
    /// A tenant with a stocked warehouse and orders in every status. The numbers are worked out here the way the
    /// endpoints would leave them: stock = purchases minus what the orders took (cancelled ones give it back),
    /// budget = income of the completed orders (the purchases are taken as paid from an initial top-up).
    /// </summary>
    private void AddTenantData(SeedTenantOptions options, Catalog catalog, DateTime now)
    {
        var key = options.ApiKey!.Trim();
        var tenant = new Tenant
        {
            Id = Guid.NewGuid(),
            Name = options.Name!.Trim(),
            ApiKeyHash = ApiKeyService.Hash(key),
            ApiKeyPrefix = key[..Math.Min(key.Length, 8)],
            IsActive = true,
            CreatedAt = now.AddDays(-2)
        };

        var stock = new Dictionary<Guid, long>();

        for (var round = 0; round < DemoData.PurchaseRounds.Length; round++)
        {
            foreach (var (productName, packs) in DemoData.PurchaseRounds[round])
            {
                var product = catalog.Products[productName];
                var quantity = (long)packs * product.PackSize;
                stock[product.Id] = stock.GetValueOrDefault(product.Id) + quantity;

                db.Purchases.Add(new Purchase
                {
                    Id = Guid.NewGuid(),
                    TenantId = tenant.Id,
                    ProductId = product.Id,
                    Packs = packs,
                    QuantityAdded = quantity,
                    TotalPriceCents = packs * product.PackPriceCents,
                    CreatedAt = now.AddDays(-2 + round)
                });
            }
        }

        var orders = DemoData.Orders;
        for (var n = 0; n < orders.Length; n++)
        {
            var seed = orders[n];

            // Oldest first, an hour and a half apart, the last one placed about that long ago.
            var createdAt = now.AddMinutes(-(orders.Length - n) * 90);

            var items = seed.Lines.Select(line =>
            {
                var drink = catalog.Drinks[line.Drink];
                return new OrderItem
                {
                    Id = Guid.NewGuid(),
                    DrinkId = drink.Id,
                    Quantity = line.Quantity,
                    UnitPriceCents = drink.PriceCents,
                    Toppings = [.. line.Toppings.Select(name => new OrderItemTopping
                    {
                        ToppingId = catalog.Toppings[name].Id,
                        PriceCents = catalog.Toppings[name].PriceCents
                    })]
                };
            }).ToList();

            // What the order takes from the warehouse: every serving's recipe plus its toppings' recipes.
            var required = new Dictionary<Guid, long>();
            foreach (var line in seed.Lines)
            {
                var perServing = catalog.Drinks[line.Drink].Ingredients
                    .Select(i => (i.ProductId, i.Quantity))
                    .Concat(line.Toppings.SelectMany(name => catalog.Toppings[name].Ingredients.Select(i => (i.ProductId, i.Quantity))));

                foreach (var (productId, quantity) in perServing)
                    required[productId] = required.GetValueOrDefault(productId) + (long)line.Quantity * quantity;
            }

            foreach (var (productId, quantity) in required)
            {
                if (stock.GetValueOrDefault(productId) < quantity)
                    throw new InvalidOperationException($"The demo purchases do not cover order {n + 1} ({seed.Customer}).");

                // A cancelled order has given its ingredients back, so only the other two keep them taken.
                if (seed.Status != OrderStatus.Cancelled)
                    stock[productId] -= quantity;
            }

            var total = items.Sum(i => i.Quantity * (i.UnitPriceCents + i.Toppings.Sum(t => t.PriceCents)));
            if (seed.Status == OrderStatus.Completed)
                tenant.BudgetCents += total;

            db.Orders.Add(new Order
            {
                Id = Guid.NewGuid(),
                TenantId = tenant.Id,
                Number = n + 1,
                Status = seed.Status,
                TotalPriceCents = total,
                CustomerName = seed.Customer,
                Comment = seed.Comment,
                CreatedAt = createdAt,
                UpdatedAt = seed.Status == OrderStatus.InProgress ? createdAt : createdAt.AddMinutes(10),
                Items = items,
                Ingredients = [.. required.Select(kv => new OrderIngredient { ProductId = kv.Key, Quantity = kv.Value })]
            });
        }

        db.Tenants.Add(tenant);
        db.StockItems.AddRange(stock.Select(kv => new StockItem
        {
            TenantId = tenant.Id,
            ProductId = kv.Key,
            Quantity = kv.Value
        }));
    }
}
