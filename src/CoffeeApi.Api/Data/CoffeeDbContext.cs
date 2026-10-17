using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace CoffeeApi.Api.Data;

public class CoffeeDbContext(DbContextOptions<CoffeeDbContext> options, ICurrentTenant currentTenant) : DbContext(options)
{
    public DbSet<Tenant> Tenants => Set<Tenant>();
    public DbSet<DrinkCategory> Categories => Set<DrinkCategory>();
    public DbSet<Drink> Drinks => Set<Drink>();
    public DbSet<Topping> Toppings => Set<Topping>();
    public DbSet<DrinkTopping> DrinkToppings => Set<DrinkTopping>();
    public DbSet<Order> Orders => Set<Order>();
    public DbSet<OrderItem> OrderItems => Set<OrderItem>();
    public DbSet<OrderItemTopping> OrderItemToppings => Set<OrderItemTopping>();
    public DbSet<Product> Products => Set<Product>();
    public DbSet<DrinkIngredient> DrinkIngredients => Set<DrinkIngredient>();
    public DbSet<ToppingIngredient> ToppingIngredients => Set<ToppingIngredient>();
    public DbSet<StockItem> StockItems => Set<StockItem>();
    public DbSet<OrderIngredient> OrderIngredients => Set<OrderIngredient>();
    public DbSet<Purchase> Purchases => Set<Purchase>();

    /// <summary>Fails closed: without an authenticated tenant this is Guid.Empty, which matches no orders.</summary>
    private Guid CurrentTenantId => currentTenant.Id ?? Guid.Empty;

    // SQLite drops DateTimeKind; treat every stored DateTime as UTC.
    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Properties<DateTime>()
            .HaveConversion<UtcDateTimeConverter>();
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CoffeeDbContext).Assembly);

        // Tenant isolation: orders (and everything hanging off them) are visible only to their own tenant.
        // Admin code that needs all orders must use IgnoreQueryFilters().
        modelBuilder.Entity<Order>()
            .HasQueryFilter(o => o.TenantId == CurrentTenantId);
        modelBuilder.Entity<OrderItem>()
            .HasQueryFilter(i => i.Order.TenantId == CurrentTenantId);
        modelBuilder.Entity<OrderItemTopping>()
            .HasQueryFilter(t => t.OrderItem.Order.TenantId == CurrentTenantId);
        modelBuilder.Entity<OrderIngredient>()
            .HasQueryFilter(i => i.Order.TenantId == CurrentTenantId);

        // The warehouse and purchase history are per tenant as well.
        modelBuilder.Entity<StockItem>()
            .HasQueryFilter(s => s.TenantId == CurrentTenantId);
        modelBuilder.Entity<Purchase>()
            .HasQueryFilter(p => p.TenantId == CurrentTenantId);
    }
}
