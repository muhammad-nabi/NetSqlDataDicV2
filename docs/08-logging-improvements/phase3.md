# Phase 3: User Context & Audit Enhancement

## Overview

Enhance audit logging with proper user context. Currently, the application uses a hardcoded "system" placeholder for user identification.

## Status

| Item | Status |
|------|--------|
| 3.1 Implement user extraction | Pending |

## Prerequisites

This phase should be implemented **after authentication is added** to the application. Without authentication:
- User context is not available
- `HttpContext.User` will be empty or anonymous
- The hardcoded "system" placeholder remains appropriate

## Current State

### Problem

`EfModelSourceService.GetCurrentUserName()` returns a hardcoded value:

```csharp
private string GetCurrentUserName()
{
    // TODO: Implement actual user extraction when authentication is added
    return "system";
}
```

### Where It's Used

| Service Method | Audit Event |
|----------------|-------------|
| `CreateAsync()` | `LogSourceCreated(sourceId, name, userName)` |
| `DeleteAsync()` | `LogSourceDeleted(sourceId, name, userName)` |

### Security Audit Service

`SecurityAuditService` logs these events but receives the hardcoded "system" value:

```csharp
public void LogSourceCreated(int sourceId, string sourceName, string userName)
{
    _logger.LogInformation(
        "EF Model Source created. SourceId: {SourceId}, Name: {SourceName}, User: {UserName}",
        sourceId, sourceName, userName);
}
```

---

## 3.1 Implement User Extraction

### File

`src/NetSqlDataDicV2.Web/Services/EfModelSourceService.cs`

### Implementation (When Auth Is Available)

**Step 1: Inject IHttpContextAccessor**

```csharp
public class EfModelSourceService : IEfModelSourceService
{
    private readonly DataDictionaryDbContext _context;
    private readonly ISecurityAuditService _securityAuditService;
    private readonly IConnectionStringProtector _protector;
    private readonly ILogger<EfModelSourceService> _logger;
    private readonly IHttpContextAccessor _httpContextAccessor;

    public EfModelSourceService(
        DataDictionaryDbContext context,
        ISecurityAuditService securityAuditService,
        IConnectionStringProtector protector,
        ILogger<EfModelSourceService> logger,
        IHttpContextAccessor httpContextAccessor)
    {
        _context = context;
        _securityAuditService = securityAuditService;
        _protector = protector;
        _logger = logger;
        _httpContextAccessor = httpContextAccessor;
    }
```

**Step 2: Register IHttpContextAccessor in Program.cs**

```csharp
builder.Services.AddHttpContextAccessor();
```

**Step 3: Implement GetCurrentUserName()**

```csharp
private string GetCurrentUserName()
{
    var user = _httpContextAccessor.HttpContext?.User;

    if (user?.Identity?.IsAuthenticated == true)
    {
        // Try common claim types for username
        return user.Identity.Name
            ?? user.FindFirst(ClaimTypes.Email)?.Value
            ?? user.FindFirst(ClaimTypes.NameIdentifier)?.Value
            ?? "authenticated-unknown";
    }

    return "anonymous";
}
```

### Claim Type Priority

The implementation tries claims in order:
1. `Identity.Name` - Standard Windows/Forms authentication
2. `ClaimTypes.Email` - OAuth/OIDC providers
3. `ClaimTypes.NameIdentifier` - Unique identifier fallback
4. `"authenticated-unknown"` - Authenticated but no name claim
5. `"anonymous"` - Not authenticated

### Example Log Output

**Before (current):**
```
info: SecurityAuditService[0]
      EF Model Source created. SourceId: 5, Name: OrdersContext, User: system
```

**After (with auth):**
```
info: SecurityAuditService[0]
      EF Model Source created. SourceId: 5, Name: OrdersContext, User: john.doe@company.com
```

---

## Edge Cases

| Scenario | Behavior |
|----------|----------|
| No authentication configured | Returns "anonymous" |
| User authenticated but no claims | Returns "authenticated-unknown" |
| Background service context | No HttpContext - needs alternative approach |
| API key authentication | Depends on how claims are populated |

### Background Service Consideration

If EfModelSourceService is ever called from a background service (no HTTP context):

```csharp
private string GetCurrentUserName()
{
    var httpContext = _httpContextAccessor.HttpContext;

    // No HTTP context (background service or startup)
    if (httpContext == null)
    {
        return "system";
    }

    var user = httpContext.User;
    // ... rest of implementation
}
```

---

## Testing

### Unit Test Updates

Existing tests will need to mock `IHttpContextAccessor`:

```csharp
[Fact]
public async Task CreateAsync_LogsUserFromHttpContext()
{
    // Arrange
    var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();
    var claims = new List<Claim> { new Claim(ClaimTypes.Name, "test.user") };
    var identity = new ClaimsIdentity(claims, "TestAuth");
    var principal = new ClaimsPrincipal(identity);
    var httpContext = new DefaultHttpContext { User = principal };
    mockHttpContextAccessor.Setup(x => x.HttpContext).Returns(httpContext);

    var service = new EfModelSourceService(
        _context, _securityAuditService, _protector, _logger, mockHttpContextAccessor.Object);

    // Act
    await service.CreateAsync(model);

    // Assert
    _mockSecurityAuditService.Verify(x =>
        x.LogSourceCreated(It.IsAny<int>(), It.IsAny<string>(), "test.user"),
        Times.Once);
}
```

### Manual Testing

1. Log in with a test user
2. Create an EF Model Source
3. Check logs for correct username
4. Delete an EF Model Source
5. Verify deletion logged with correct username
6. Test anonymous access (if allowed) - should log "anonymous"

---

## Future Enhancements (Out of Scope)

These could be considered for a later phase:

| Enhancement | Description |
|-------------|-------------|
| IP Address logging | Log client IP for security audits |
| Session tracking | Correlate actions within a session |
| Role-based logging | Log user roles for authorization audits |
| Impersonation support | Handle admin impersonating other users |

---

## Files Summary

| File | Action | Risk |
|------|--------|------|
| `Program.cs` | Add `AddHttpContextAccessor()` | Very Low |
| `Services/EfModelSourceService.cs` | Inject accessor, implement GetCurrentUserName | Low |
| Tests | Update mocks | Low |

## Dependencies

- Authentication must be implemented first
- Claims must be properly populated by auth provider

## Completion Criteria

- [ ] IHttpContextAccessor registered in DI
- [ ] EfModelSourceService injects IHttpContextAccessor
- [ ] GetCurrentUserName() extracts user from claims
- [ ] Fallback to "anonymous" for unauthenticated users
- [ ] Fallback to "system" for non-HTTP contexts
- [ ] Audit logs show actual username after auth events
- [ ] Unit tests updated with HttpContextAccessor mocks
- [ ] All existing tests pass
