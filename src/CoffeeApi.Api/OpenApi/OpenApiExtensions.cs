using CoffeeApi.Api.Auth;
using Microsoft.OpenApi;

namespace CoffeeApi.Api.OpenApi;

/// <summary>
/// Two OpenAPI documents with their own security: <c>tenant</c> (the default, <c>/api/*</c> except admin) is
/// authorized by tenant name + tenant key, <c>admin</c> (<c>/api/admin/*</c>) by the admin key.
/// </summary>
public static class OpenApiExtensions
{
    public const string TenantDocument = "tenant";
    public const string AdminDocument = "admin";

    private const string AdminPrefix = "api/admin/";

    private const string TenantNameScheme = "TenantName";
    private const string TenantKeyScheme = "TenantKey";
    private const string AdminKeyScheme = "AdminKey";

    public static IServiceCollection AddCoffeeOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(TenantDocument, options =>
        {
            options.ShouldInclude = description => IsApi(description) && !IsAdmin(description);
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Coffee API";
                document.Info.Description = "Tenant API. Authorize with the tenant name and tenant key.";
                // Both headers are required together, so they form a single security requirement.
                AddApiKey(document, TenantNameScheme, AuthConstants.TenantNameHeader, "Tenant name.");
                AddApiKey(document, TenantKeyScheme, AuthConstants.TenantKeyHeader, "Tenant API key.");
                Require(document, TenantNameScheme, TenantKeyScheme);
                return Task.CompletedTask;
            });
        });

        services.AddOpenApi(AdminDocument, options =>
        {
            options.ShouldInclude = IsAdmin;
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info.Title = "Coffee API (admin)";
                document.Info.Description = "Admin API. Authorize with the admin key.";
                AddApiKey(document, AdminKeyScheme, AuthConstants.AdminHeader, "Admin key.");
                Require(document, AdminKeyScheme);
                return Task.CompletedTask;
            });
        });

        return services;
    }

    public static WebApplication UseCoffeeSwagger(this WebApplication app)
    {
        app.MapOpenApi();
        app.UseSwaggerUI(options =>
        {
            // The first definition is the one opened by default.
            options.SwaggerEndpoint($"/openapi/{TenantDocument}.json", "Tenant");
            options.SwaggerEndpoint($"/openapi/{AdminDocument}.json", "Admin");
        });
        return app;
    }

    private static bool IsApi(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription description) =>
        description.RelativePath?.StartsWith("api/", StringComparison.OrdinalIgnoreCase) == true;

    private static bool IsAdmin(Microsoft.AspNetCore.Mvc.ApiExplorer.ApiDescription description) =>
        description.RelativePath?.StartsWith(AdminPrefix, StringComparison.OrdinalIgnoreCase) == true;

    private static void AddApiKey(OpenApiDocument document, string id, string header, string description)
    {
        document.Components ??= new OpenApiComponents();
        document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
        document.Components.SecuritySchemes[id] = new OpenApiSecurityScheme
        {
            Type = SecuritySchemeType.ApiKey,
            In = ParameterLocation.Header,
            Name = header,
            Description = description
        };
    }

    private static void Require(OpenApiDocument document, params string[] schemeIds)
    {
        var requirement = new OpenApiSecurityRequirement();
        foreach (var id in schemeIds)
            requirement[new OpenApiSecuritySchemeReference(id, document)] = [];

        document.Security ??= [];
        document.Security.Add(requirement);
    }
}
