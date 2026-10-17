using AntDesign;
using CoffeeApi.Tenant.Api;
using CoffeeApi.Tenant.Components;
using CoffeeApi.Tenant.Services;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();
builder.Services.AddAntDesign();

// Tenant name and key live in appsettings (section "CoffeeApi"); the app refuses to start without them.
builder.Services.AddOptions<CoffeeApiOptions>()
    .BindConfiguration(CoffeeApiOptions.SectionName)
    .ValidateDataAnnotations()
    .ValidateOnStart();

// Every call to the API carries the tenant's credentials. Requests are made from the server (Blazor Server),
// so the key is never sent to the browser.
builder.Services.AddHttpClient<CoffeeApiClient>((services, client) =>
{
    var options = services.GetRequiredService<IOptions<CoffeeApiOptions>>().Value;
    client.BaseAddress = new Uri(options.BaseUrl);
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Tenant-Name", options.TenantName);
    client.DefaultRequestHeaders.TryAddWithoutValidation("X-Api-Key", options.TenantKey);
});
builder.Services.AddScoped<TenantSession>();
builder.Services.AddScoped<CartState>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
