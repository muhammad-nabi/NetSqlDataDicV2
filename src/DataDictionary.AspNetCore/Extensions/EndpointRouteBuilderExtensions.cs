using DataDictionary.AspNetCore.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;

namespace DataDictionary.AspNetCore.Extensions;

/// <summary>
/// Extension methods for mapping Data Dictionary routes.
/// </summary>
public static class EndpointRouteBuilderExtensions
{
    /// <summary>
    /// Maps the Data Dictionary area routes.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    /// <param name="routePrefix">Optional custom route prefix. Uses configured value if null.</param>
    /// <returns>The endpoint route builder for chaining.</returns>
    public static IEndpointRouteBuilder MapDataDictionary(
        this IEndpointRouteBuilder endpoints,
        string? routePrefix = null)
    {
        var options = endpoints.ServiceProvider.GetRequiredService<DataDictionaryOptions>();
        var prefix = routePrefix ?? options.RoutePrefix;

        // Main area routes
        var conventionBuilder = endpoints.MapAreaControllerRoute(
            name: "DataDictionary",
            areaName: options.AreaName,
            pattern: $"{prefix}/{{controller=Home}}/{{action=Index}}/{{id?}}");

        // Apply authorization based on configuration
        if (options.RequireAuthorization)
        {
            if (options.RequiredRoles?.Length > 0)
            {
                // Role-based authorization
                conventionBuilder.RequireAuthorization(new AuthorizeAttribute
                {
                    Roles = string.Join(",", options.RequiredRoles)
                });
            }
            else if (!string.IsNullOrEmpty(options.AuthorizationPolicy))
            {
                // Named policy authorization
                conventionBuilder.RequireAuthorization(options.AuthorizationPolicy);
            }
            else
            {
                // Default authorization (requires authenticated user)
                conventionBuilder.RequireAuthorization();
            }
        }

        return endpoints;
    }
}
