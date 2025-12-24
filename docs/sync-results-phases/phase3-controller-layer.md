# Phase 3: Controller Layer

## Status: Pending

## Objective

Add Results action to SyncController to display sync results page.

## Files to Modify

| File | Change |
|------|--------|
| `Controllers/SyncController.cs` | Add Results action |

## Controller Action

Add to `SyncController.cs`:

```csharp
/// <summary>
/// Displays detailed results for a specific sync operation.
/// </summary>
/// <param name="id">The sync history ID.</param>
/// <param name="cancellationToken">Cancellation token.</param>
/// <returns>Results view or NotFound.</returns>
[HttpGet]
public async Task<IActionResult> Results(int id, CancellationToken cancellationToken)
{
    var results = await _syncService.GetSyncResultsAsync(id, cancellationToken);

    if (results == null)
    {
        _logger.LogWarning("Sync history {SyncHistoryId} not found", id);
        return NotFound();
    }

    return View(results);
}
```

## Route

The action will be accessible at:
- `/Sync/Results/{id}` - Standard MVC routing
- Example: `/Sync/Results/42`

## Design Notes

1. **Parameter naming** - Uses `id` to match standard ASP.NET Core routing convention
2. **Logging** - Logs warning for not-found requests (helps troubleshoot broken links)
3. **CancellationToken** - Supports request cancellation
4. **NotFound** - Returns 404 for invalid syncHistoryId

## Existing Controller Context

Current `SyncController` has:
- `Index()` - GET - Shows sync page with history
- `Execute()` - POST - Runs sync and returns JSON

The new `Results()` action follows the same pattern.

## Edge Cases

| Scenario | Response |
|----------|----------|
| Valid syncHistoryId | 200 OK with Results view |
| Invalid syncHistoryId | 404 NotFound |
| syncHistoryId = 0 | 404 NotFound (service returns null) |
| Negative syncHistoryId | 404 NotFound |

## Verification

After this phase:
- [ ] Project builds successfully
- [ ] `/Sync/Results/1` returns Results view (assuming syncHistoryId 1 exists)
- [ ] `/Sync/Results/999999` returns 404 NotFound
- [ ] Warning logged for not-found requests
