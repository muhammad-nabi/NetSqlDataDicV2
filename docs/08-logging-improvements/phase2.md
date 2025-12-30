# Phase 2: Operational Visibility

## Overview

Medium-effort improvements that significantly enhance observability, debugging, and performance monitoring.

## Status

| Item | Status |
|------|--------|
| 2.1 Request/Response logging middleware | Pending |
| 2.2 Controller success logging | Pending |
| 2.3 Performance timing for services | Pending |

---

## 2.1 Request/Response Logging Middleware

### Problem

No HTTP request-level logging exists. Debugging client issues requires guesswork.

### Files

| File | Action |
|------|--------|
| `src/NetSqlDataDicV2.Web/Middleware/RequestLoggingMiddleware.cs` | Create |
| `src/NetSqlDataDicV2.Web/Program.cs` | Register middleware |

### Implementation

**Create RequestLoggingMiddleware.cs:**

```csharp
using System.Diagnostics;

namespace NetSqlDataDicV2.Web.Middleware;

public class RequestLoggingMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<RequestLoggingMiddleware> _logger;

    public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context)
    {
        var stopwatch = Stopwatch.StartNew();
        var method = context.Request.Method;
        var path = context.Request.Path;
        var queryString = context.Request.QueryString.HasValue
            ? context.Request.QueryString.Value
            : string.Empty;

        try
        {
            await _next(context);
        }
        finally
        {
            stopwatch.Stop();
            var statusCode = context.Response.StatusCode;
            var elapsedMs = stopwatch.ElapsedMilliseconds;

            if (statusCode >= 500)
            {
                _logger.LogError(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs}ms",
                    method, path, queryString, statusCode, elapsedMs);
            }
            else if (statusCode >= 400)
            {
                _logger.LogWarning(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs}ms",
                    method, path, queryString, statusCode, elapsedMs);
            }
            else
            {
                _logger.LogInformation(
                    "HTTP {Method} {Path}{QueryString} responded {StatusCode} in {ElapsedMs}ms",
                    method, path, queryString, statusCode, elapsedMs);
            }
        }
    }
}
```

**Register in Program.cs (after builder.Build(), before app.Run()):**

```csharp
// Add before exception handling middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();
```

### Configuration (Optional)

Add to `appsettings.json` to control request logging verbosity:

```json
{
  "Logging": {
    "LogLevel": {
      "NetSqlDataDicV2.Web.Middleware.RequestLoggingMiddleware": "Information"
    }
  }
}
```

### Example Output

```
info: RequestLoggingMiddleware[0]
      HTTP GET /DataDictionary?server=Prod1 responded 200 in 45ms
info: RequestLoggingMiddleware[0]
      HTTP POST /Sync/Execute responded 200 in 3250ms
warn: RequestLoggingMiddleware[0]
      HTTP GET /DataDictionary/Details/99999 responded 404 in 12ms
```

### Edge Cases

- **Static files**: Will log requests to /css, /js, /lib - acceptable overhead
- **Health checks**: If added later, may want to exclude `/health` endpoints
- **Query string logging**: Includes query parameters per user decision
- **Performance**: Stopwatch overhead is negligible (<1ms)

---

## 2.2 Controller Success Logging

### Problem

Controllers only log errors. No visibility into successful operations.

### Files

| File | Actions to Log |
|------|----------------|
| `Controllers/SyncController.cs` | Sync execution, Results viewed |
| `Controllers/ComparisonController.cs` | Compare execution |
| `Controllers/DataDictionaryController.cs` | Update, Note added, CSV export |
| `Controllers/EfModelSourcesController.cs` | Create, Edit, Delete |

### Implementation

**SyncController.cs - After successful sync:**

```csharp
[HttpPost]
public async Task<IActionResult> Execute(...)
{
    // ... existing code ...

    var result = await _syncService.SyncDatabaseAsync(...);

    _logger.LogInformation(
        "Sync executed for {Server}/{Database}: {Added} added, {Modified} modified, {Deleted} deleted",
        model.ServerName, model.DatabaseName,
        result.Added, result.Modified, result.Deleted);

    return RedirectToAction("Results", new { id = result.SyncHistoryId });
}
```

**ComparisonController.cs - After comparison:**

```csharp
[HttpPost]
public async Task<IActionResult> Compare(int sourceId)
{
    // ... existing code ...

    var result = await _efModelService.CompareAsync(sourceId);

    _logger.LogInformation(
        "Comparison executed for source {SourceId}: {Total} columns, {Matches} matches, {Mismatches} mismatches",
        sourceId, result.TotalColumns, result.Matches, result.Mismatches);

    return Json(result);
}
```

**DataDictionaryController.cs - For updates:**

```csharp
[HttpPost]
public async Task<IActionResult> Update(...)
{
    // ... existing code ...

    await _service.UpdateAsync(viewModel);

    _logger.LogInformation(
        "DataElement {Id} updated: {Server}/{Database}.{Schema}.{Table}.{Column}",
        id, viewModel.DatabaseServer, viewModel.DatabaseName,
        viewModel.SchemaName, viewModel.TableName, viewModel.ColumnName);

    return RedirectToAction("Details", new { id });
}

[HttpPost]
public async Task<IActionResult> AddNote(int id, string noteText)
{
    // ... existing code ...

    await _service.AddNoteAsync(id, noteText);

    _logger.LogInformation("Note added to DataElement {Id}", id);

    return RedirectToAction("Details", new { id });
}

public async Task<IActionResult> ExportCsv(...)
{
    // ... existing code ...

    _logger.LogInformation(
        "CSV export generated: {RowCount} rows, filters: Server={Server}, Database={Database}",
        data.Count(), serverFilter ?? "all", databaseFilter ?? "all");

    return File(csvBytes, "text/csv", "data-dictionary-export.csv");
}
```

**EfModelSourcesController.cs - For CRUD:**

```csharp
[HttpPost]
public async Task<IActionResult> Create(...)
{
    // ... existing code ...

    await _service.CreateAsync(model);

    _logger.LogInformation("EF Model Source created: {Name} ({DllPath})",
        model.Name, model.DllPath);

    return RedirectToAction("Index");
}

[HttpPost]
public async Task<IActionResult> Delete(int id)
{
    var source = await _service.GetByIdAsync(id);
    // ... existing code ...

    await _service.DeleteAsync(id);

    _logger.LogInformation("EF Model Source deleted: {Id} ({Name})",
        id, source?.Name ?? "unknown");

    return RedirectToAction("Index");
}
```

### Testing

1. Execute a sync operation - verify log entry appears
2. Run a comparison - verify log entry appears
3. Update a data element - verify log entry appears
4. Add a note - verify log entry appears
5. Export CSV - verify log entry appears
6. Create/Delete EF Model Source - verify log entries appear

---

## 2.3 Performance Timing for Long Operations

### Problem

No visibility into how long critical operations take. Can't identify performance bottlenecks.

### Files

| File | Operations to Time |
|------|-------------------|
| `Services/DatabaseSyncService.cs` | Full sync, Discovery, Save |
| `Services/EfModelService.cs` | Model loading, Comparison |
| `Services/ComparisonService.cs` | Compare operation |

### Implementation

**DatabaseSyncService.cs:**

```csharp
using System.Diagnostics;

public async Task<SyncResult> SyncDatabaseAsync(...)
{
    var totalStopwatch = Stopwatch.StartNew();
    var phaseStopwatch = new Stopwatch();

    // Phase 1: Discovery
    phaseStopwatch.Start();
    var sourceColumns = await DiscoverSourceColumnsAsync(connectionString);
    phaseStopwatch.Stop();
    var discoveryMs = phaseStopwatch.ElapsedMilliseconds;

    // Phase 2: Comparison
    phaseStopwatch.Restart();
    var changes = CompareWithExisting(sourceColumns, existingColumns);
    phaseStopwatch.Stop();
    var comparisonMs = phaseStopwatch.ElapsedMilliseconds;

    // Phase 3: Save
    phaseStopwatch.Restart();
    await SaveChangesAsync(changes);
    phaseStopwatch.Stop();
    var saveMs = phaseStopwatch.ElapsedMilliseconds;

    // Phase 4: Audit
    phaseStopwatch.Restart();
    await CreateAuditRecordsAsync(changes, syncHistoryId);
    phaseStopwatch.Stop();
    var auditMs = phaseStopwatch.ElapsedMilliseconds;

    totalStopwatch.Stop();

    _logger.LogInformation(
        "Sync completed for {Server}/{Database} in {TotalMs}ms " +
        "(Discovery: {DiscoveryMs}ms, Compare: {CompareMs}ms, Save: {SaveMs}ms, Audit: {AuditMs}ms). " +
        "Results: {Added} added, {Modified} modified, {Deleted} deleted",
        serverName, databaseName,
        totalStopwatch.ElapsedMilliseconds, discoveryMs, comparisonMs, saveMs, auditMs,
        result.Added, result.Modified, result.Deleted);

    return result;
}
```

**EfModelService.cs:**

```csharp
public async Task<ComparisonResult> CompareAsync(int sourceId)
{
    var stopwatch = Stopwatch.StartNew();

    // ... existing code ...

    var loadMs = stopwatch.ElapsedMilliseconds;
    _logger.LogDebug("DbContext loaded in {LoadMs}ms for source {SourceId}", loadMs, sourceId);

    // ... comparison logic ...

    stopwatch.Stop();
    _logger.LogInformation(
        "EF comparison completed for source {SourceId} ({SourceName}) in {ElapsedMs}ms. " +
        "Columns: {Total}, Matches: {Matches}, Mismatches: {Mismatches}, Skipped Tables: {Skipped}",
        sourceId, source.Name, stopwatch.ElapsedMilliseconds,
        result.TotalColumns, result.Matches, result.Mismatches, result.SkippedTables);

    return result;
}
```

**ComparisonService.cs:**

```csharp
public async Task<IEnumerable<ColumnComparisonResult>> CompareAsync(...)
{
    var stopwatch = Stopwatch.StartNew();

    // ... existing code ...

    stopwatch.Stop();
    _logger.LogInformation(
        "Comparison completed in {ElapsedMs}ms: {TotalColumns} columns compared",
        stopwatch.ElapsedMilliseconds, results.Count());

    return results;
}
```

### Example Output

```
info: DatabaseSyncService[0]
      Sync completed for Prod1/SalesDB in 4532ms (Discovery: 2100ms, Compare: 150ms, Save: 1800ms, Audit: 482ms).
      Results: 12 added, 45 modified, 3 deleted

info: EfModelService[0]
      EF comparison completed for source 5 (OrdersContext) in 890ms.
      Columns: 234, Matches: 220, Mismatches: 10, Skipped Tables: 4
```

### Benefits

- Identify which phase is slow (discovery vs save vs audit)
- Track performance over time
- Detect degradation in specific operations
- Guide optimization efforts

---

## Files Summary

| File | Action | Risk |
|------|--------|------|
| `Middleware/RequestLoggingMiddleware.cs` | Create new | Low |
| `Program.cs` | Register middleware | Low |
| `Controllers/SyncController.cs` | Add success logging | Low |
| `Controllers/ComparisonController.cs` | Add success logging | Low |
| `Controllers/DataDictionaryController.cs` | Add success logging | Low |
| `Controllers/EfModelSourcesController.cs` | Add success logging | Low |
| `Services/DatabaseSyncService.cs` | Add timing | Low |
| `Services/EfModelService.cs` | Add timing | Low |
| `Services/ComparisonService.cs` | Add timing | Low |

## Completion Criteria

- [ ] Request logging middleware created and registered
- [ ] All HTTP requests logged with method, path, query, status, duration
- [ ] Controllers log successful POST/PUT/DELETE operations
- [ ] Sync operations log phase-by-phase timing breakdown
- [ ] Comparison operations log total duration and results
- [ ] All existing tests pass
- [ ] Log output is clean and not excessively verbose
