using System.Text.Json.Serialization;
using CoffeeApi.Api.Auth;
using CoffeeApi.Api.Data;
using CoffeeApi.Api.Endpoints;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();
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

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// SQLite is owned by this single process, so it is safe to bring the schema up to date on startup in every
// environment (a fresh docker volume is created and migrated on the first run).
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<CoffeeDbContext>().Database.Migrate();
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
