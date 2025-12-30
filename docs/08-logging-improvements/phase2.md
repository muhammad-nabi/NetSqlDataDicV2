# Phase 2: Operational Visibility

## Overview

Medium-effort improvements that significantly enhance observability, debugging, and performance monitoring.

## Status

| Item | Status |
|------|--------|
| 2.1 Request/Response logging middleware | Complete |
| 2.2 Controller success logging | Complete |
| 2.3 Performance timing for services | Complete |

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
| `Controllers/SyncController.cs` | Sync execution |
| `Controllers/ComparisonController.cs` | Compare execution |
| `Controllers/DataDictionaryController.cs` | Update, Note added, CSV export |
| `Controllers/EfModelSourcesController.cs` | Create, Edit, Delete, ToggleActive |

### Implementation

**SyncController.cs - After successful sync:**

```csharp
_logger.LogInformation(
    "Sync executed for {Server}/{Database}: {Added} added, {Updated} updated, {Removed} deleted in {Duration:F1}s",
    serverName, databaseName,
    result.ColumnsAdded, result.ColumnsUpdated, result.ColumnsRemoved,
    result.Duration.TotalSeconds);
```

**ComparisonController.cs - After comparison:**

```csharp
_logger.LogInformation(
    "Comparison executed for source {SourceId}: {Total} items, {Matches} matches, {MissingInEf} missing in EF, {TypeMismatches} type mismatches",
    sourceId, result.TotalItems, result.TotalMatches, result.TotalMissingInEf, result.TotalTypeMismatches);
```

**DataDictionaryController.cs - For updates, notes, and CSV export:**

```csharp
// Update
_logger.LogInformation("DataElement {Id} updated successfully", model.DataElementId);

// AddNote
_logger.LogInformation("Note added to DataElement {Id}", model.DataElementId);

// ExportCsv
_logger.LogInformation(
    "CSV export generated: {RowCount} rows, filters: Server={Server}, Database={Database}",
    data.Count(), server ?? "all", database ?? "all");
```

**EfModelSourcesController.cs - For CRUD operations:**

```csharp
// Create
_logger.LogInformation("EF Model Source created: {Name} ({DllPath})", model.Name, model.AssemblyPath);

// Edit
_logger.LogInformation("EF Model Source updated: {Id} ({Name})", id, model.Name);

// Delete
_logger.LogInformation("EF Model Source deleted: {Id}", id);

// ToggleActive
_logger.LogInformation("EF Model Source {Id} toggled to {Status}", id, newStatus ? "active" : "inactive");
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

**DatabaseSyncService.cs - Phase-by-phase timing:**

```csharp
var totalStopwatch = Stopwatch.StartNew();
var phaseStopwatch = Stopwatch.StartNew();

// Phase 1: Discovery
var sourceColumns = await DiscoverColumnsAsync(connectionString, cancellationToken);
var discoveryMs = phaseStopwatch.ElapsedMilliseconds;
phaseStopwatch.Restart();

// Phase 2: Compare and process changes
// ... comparison logic ...
var compareMs = phaseStopwatch.ElapsedMilliseconds;
phaseStopwatch.Restart();

// Phase 3: Save
await _context.SaveChangesAsync(cancellationToken);
var saveMs = phaseStopwatch.ElapsedMilliseconds;
phaseStopwatch.Restart();

// Phase 4: Update sync history
await _context.SaveChangesAsync(cancellationToken);
var auditMs = phaseStopwatch.ElapsedMilliseconds;
totalStopwatch.Stop();

_logger.LogInformation(
    "Sync completed for {Server}/{Database} in {TotalMs}ms " +
    "(Discovery: {DiscoveryMs}ms, Compare: {CompareMs}ms, Save: {SaveMs}ms, Audit: {AuditMs}ms). " +
    "Results: {Added} added, {Updated} updated, {Removed} removed",
    serverName, databaseName, totalStopwatch.ElapsedMilliseconds,
    discoveryMs, compareMs, saveMs, auditMs,
    added, updated, removed);
```

**EfModelService.cs - DbContext loading and extraction timing:**

```csharp
var stopwatch = Stopwatch.StartNew();

using var provider = _providerFactory.GetProvider(source);
using var result = provider.GetDbContext(source);
var loadMs = stopwatch.ElapsedMilliseconds;

_logger.LogDebug("DbContext loaded in {LoadMs}ms for source {SourceName}", loadMs, source.Name);

var columns = ExtractColumnsFromContext(result.Context);

stopwatch.Stop();
_logger.LogInformation(
    "EF model extraction completed for {SourceName} in {ElapsedMs}ms: {ColumnCount} columns from {EntityCount} entities",
    source.Name, stopwatch.ElapsedMilliseconds, columns.Count,
    columns.Select(c => c.EntityName).Distinct().Count());
```

**ComparisonService.cs - Total comparison timing:**

```csharp
var totalStopwatch = Stopwatch.StartNew();

// ... comparison logic ...

totalStopwatch.Stop();

_logger.LogInformation(
    "Comparison completed for {SourceName} in {ElapsedMs}ms: " +
    "{Matches} matches, {MissingEf} missing in EF, {MissingDb} missing in DB, " +
    "{TypeMismatches} type mismatches, {ConstraintMismatches} constraint mismatches, " +
    "{SkippedTables} skipped tables ({SkippedColumns} columns)",
    source.Name, totalStopwatch.ElapsedMilliseconds,
    result.TotalMatches, result.TotalMissingInEf, result.TotalMissingInDb,
    result.TotalTypeMismatches, result.TotalConstraintMismatches,
    result.TotalSkippedTables, result.TotalSkippedColumns);
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

- [x] Request logging middleware created and registered
- [x] All HTTP requests logged with method, path, query, status, duration
- [x] Controllers log successful POST/PUT/DELETE operations
- [x] Sync operations log phase-by-phase timing breakdown
- [x] Comparison operations log total duration and results
- [x] All existing tests pass
- [x] Log output is clean and not excessively verbose
