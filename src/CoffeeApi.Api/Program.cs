using System.Text.Json.Serialization;
using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Data.Seeding;
using CoffeeApi.Api.Endpoints;
using CoffeeApi.Api.OpenApi;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCoffeeOpenApi();
builder.Services.AddValidation();
builder.Services.ConfigureHttpJsonOptions(options =>
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<ICurrentTenant, HttpContextCurrentTenant>();

builder.Services.AddAuthentication()
    .AddScheme<AuthenticationSchemeOptions, AdminApiKeyAuthenticationHandler>(AuthConstants.AdminScheme, null)
    .AddScheme<AuthenticationSchemeOptions, TenantApiKeyAuthenticationHandler>(AuthConstants.TenantScheme, null);
builder.Services.AddAuthorizationBuilder()
    .AddPolicy(AuthConstants.AdminPolicy, policy => policy
        .AddAuthenticationSchemes(AuthConstants.AdminScheme)
        .RequireRole(AuthConstants.AdminRole))
    .AddPolicy(AuthConstants.TenantPolicy, policy => policy
        .AddAuthenticationSchemes(AuthConstants.TenantScheme)
        .RequireRole(AuthConstants.TenantRole));

builder.Services.AddDbContext<CoffeeDbContext>(options =>
    options.UseSqlite(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        // Responses load several collections (items, toppings, ingredients): one query each, no cartesian products.
        sqlite => sqlite.UseQuerySplittingBehavior(QuerySplittingBehavior.SplitQuery)));

builder.Services.Configure<SeedOptions>(builder.Configuration.GetSection(SeedOptions.SectionName));
builder.Services.AddScoped<DatabaseSeeder>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseCoffeeSwagger();
}

// SQLite is owned by this single process, so it is safe to bring the schema up to date on startup in every
// environment (a fresh docker volume is created and migrated on the first run), and to fill a database
// that is still empty with demo data (see SeedOptions to switch that off).
using (var scope = app.Services.CreateScope())
{
    await scope.ServiceProvider.GetRequiredService<CoffeeDbContext>().Database.MigrateAsync();
    await scope.ServiceProvider.GetRequiredService<DatabaseSeeder>().SeedAsync();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapTenantEndpoints();
app.MapDrinkEndpoints();
app.MapToppingEndpoints();
app.MapCategoryEndpoints();
app.MapProductEndpoints();

app.MapCatalogEndpoints();
app.MapWarehouseEndpoints();
app.MapOrderEndpoints();

var summaries = new[]
{
    "Freezing", "Bracing", "Chilly", "Cool", "Mild", "Warm", "Balmy", "Hot", "Sweltering", "Scorching"
};

app.MapGet("/weatherforecast", () =>
{
    var forecast =  Enumerable.Range(1, 5).Select(index =>
        new WeatherForecast
        (
            DateOnly.FromDateTime(DateTime.Now.AddDays(index)),
            Random.Shared.Next(-20, 55),
            summaries[Random.Shared.Next(summaries.Length)]
        ))
        .ToArray();
    return forecast;
})
.WithName("GetWeatherForecast");

app.Run();

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
