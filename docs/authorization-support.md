# Add Authorization Support to Data Dictionary RCL

## Summary
Add configurable authorization to the Data Dictionary Razor Class Library so consumer applications can protect `/tools/datadictionary` routes. Authorization is **opt-in** (non-breaking change).

## Files to Modify

| File | Change |
|------|--------|
| `src/DataDictionary.AspNetCore/Configuration/DataDictionaryOptions.cs` | Add 3 authorization properties |
| `src/DataDictionary.AspNetCore/Extensions/EndpointRouteBuilderExtensions.cs` | Apply authorization to mapped routes |
| `CLAUDE.md` | Document authorization configuration |
| `tests/NetSqlDataDicV2.Tests/Extensions/EndpointRouteBuilderExtensionsTests.cs` | New test file |

---

## Implementation Steps

### Step 1: Update DataDictionaryOptions.cs

Add three new properties:

```csharp
/// <summary>
/// Whether to require authorization for Data Dictionary routes. Default: false (opt-in)
/// Set to true to require authentication for all routes.
/// </summary>
public bool RequireAuthorization { get; set; } = false;

/// <summary>
/// The authorization policy name to use. Default: null (uses default policy)
/// Only applies when RequireAuthorization is true.
/// </summary>
public string? AuthorizationPolicy { get; set; }

/// <summary>
/// Roles required to access Data Dictionary routes. Default: null (no role restriction)
/// Only applies when RequireAuthorization is true. Takes precedence over AuthorizationPolicy.
/// </summary>
public string[]? RequiredRoles { get; set; }
```

### Step 2: Update EndpointRouteBuilderExtensions.cs

Add using statement:
```csharp
using Microsoft.AspNetCore.Authorization;
```

Modify `MapDataDictionary()` to apply conditional authorization:

```csharp
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
```

### Step 3: Update CLAUDE.md

Add new section under "Pluggable UI Package":

```markdown
## Authorization Configuration

The Data Dictionary supports configurable authorization (opt-in).

**Configuration Options (appsettings.json):**
```json
{
  "DataDictionary": {
    "RequireAuthorization": false,
    "AuthorizationPolicy": null,
    "RequiredRoles": null
  }
}
```

**Scenarios:**

| Scenario | Configuration |
|----------|--------------|
| Public access (default) | `"RequireAuthorization": false` |
| Authenticated users | `"RequireAuthorization": true` |
| Custom policy | `"RequireAuthorization": true, "AuthorizationPolicy": "MyPolicy"` |
| Role-based | `"RequireAuthorization": true, "RequiredRoles": ["Admin"]` |

**Important:** Consumer must call `app.UseAuthentication()` and `app.UseAuthorization()` before `app.MapDataDictionary()`.
```

### Step 4: Add Unit Tests

Create new test file with tests for:
- `MapDataDictionary_WithRequireAuthorizationFalse_NoAuthApplied` (default)
- `MapDataDictionary_WithRequireAuthorizationTrue_AppliesDefaultAuth`
- `MapDataDictionary_WithAuthorizationPolicy_AppliesNamedPolicy`
- `MapDataDictionary_WithRequiredRoles_AppliesRoleAuth`
- `MapDataDictionary_RolesTakePrecedenceOverPolicy`

---

## Consumer Usage Examples

**Enable authentication (default policy):**
```json
{
  "DataDictionary": {
    "RequireAuthorization": true
  }
}
```

**Custom policy:**
```json
{
  "DataDictionary": {
    "RequireAuthorization": true,
    "AuthorizationPolicy": "DataDictionaryAdmin"
  }
}
```

**Role-based:**
```json
{
  "DataDictionary": {
    "RequireAuthorization": true,
    "RequiredRoles": ["Admin", "DataAdmin"]
  }
}
```
