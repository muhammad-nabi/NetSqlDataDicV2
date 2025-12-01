# Phase 3: Sync Feature

## Objective
Implement the database synchronization feature that reads metadata from a source SQL Server database and populates/updates the data dictionary.

## Tasks

### 3.1 Create DTOs

**src/NetSqlDataDicV2.Web/Models/Dto/SourceColumnDto.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.Dto;

public class SourceColumnDto
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string ColumnName { get; set; } = string.Empty;
    public string DataType { get; set; } = string.Empty;
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public bool IsIdentity { get; set; }
    public int OrdinalPosition { get; set; }
    public string? ForeignKeyTo { get; set; }
}
```

**src/NetSqlDataDicV2.Web/Models/ViewModels/SyncResultViewModel.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class SyncResultViewModel
{
    public int SyncHistoryId { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public int TablesProcessed { get; set; }
    public int ColumnsProcessed { get; set; }
    public int ColumnsAdded { get; set; }
    public int ColumnsUpdated { get; set; }
    public int ColumnsRemoved { get; set; }
    public TimeSpan Duration { get; set; }
}
```

**src/NetSqlDataDicV2.Web/Models/ViewModels/SyncHistoryViewModel.cs:**
```csharp
namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class SyncHistoryViewModel
{
    public int SyncHistoryId { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime SyncStartTime { get; set; }
    public DateTime? SyncEndTime { get; set; }
    public int? TablesProcessed { get; set; }
    public int? ColumnsProcessed { get; set; }
    public int? ColumnsAdded { get; set; }
    public int? ColumnsUpdated { get; set; }
    public int? ColumnsRemoved { get; set; }
    public string Status { get; set; } = string.Empty;
    public string? ErrorMessage { get; set; }

    public string StatusBadgeClass => Status switch
    {
        "Completed" => "bg-success",
        "Failed" => "bg-danger",
        "Running" => "bg-warning",
        _ => "bg-secondary"
    };

    public string DurationDisplay
    {
        get
        {
            if (!SyncEndTime.HasValue) return "In progress...";
            var duration = SyncEndTime.Value - SyncStartTime;
            return duration.TotalSeconds < 60
                ? $"{duration.TotalSeconds:F1}s"
                : $"{duration.TotalMinutes:F1}m";
        }
    }
}
```

### 3.2 Create Sync Service

**src/NetSqlDataDicV2.Web/Services/IDatabaseSyncService.cs:**
```csharp
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public interface IDatabaseSyncService
{
    Task<SyncResultViewModel> SyncDatabaseAsync(
        string connectionString,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default);

    Task<List<SourceColumnDto>> DiscoverColumnsAsync(
        string connectionString,
        CancellationToken cancellationToken = default);

    Task<List<SyncHistoryViewModel>> GetRecentSyncHistoryAsync(
        int count = 10,
        CancellationToken cancellationToken = default);
}
```

**src/NetSqlDataDicV2.Web/Services/DatabaseSyncService.cs:**
```csharp
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.Web.Data;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public class DatabaseSyncService : IDatabaseSyncService
{
    private readonly DataDictionaryDbContext _context;
    private readonly ILogger<DatabaseSyncService> _logger;

    public DatabaseSyncService(
        DataDictionaryDbContext context,
        ILogger<DatabaseSyncService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<SyncResultViewModel> SyncDatabaseAsync(
        string connectionString,
        string serverName,
        string databaseName,
        CancellationToken cancellationToken = default)
    {
        var startTime = DateTime.UtcNow;

        // Create sync history record
        var syncHistory = new SyncHistory
        {
            DatabaseServer = serverName,
            DatabaseName = databaseName,
            SyncStartTime = startTime,
            Status = "Running"
        };

        _context.SyncHistory.Add(syncHistory);
        await _context.SaveChangesAsync(cancellationToken);

        try
        {
            _logger.LogInformation("Starting sync for {Server}/{Database}", serverName, databaseName);

            // Discover columns from source database
            var sourceColumns = await DiscoverColumnsAsync(connectionString, cancellationToken);

            _logger.LogInformation("Discovered {Count} columns from source", sourceColumns.Count);

            // Get existing entries (including soft-deleted for potential restore)
            var existingEntries = await _context.DataElements
                .IgnoreQueryFilters()
                .Where(e => e.DatabaseServer == serverName && e.DatabaseName == databaseName)
                .ToListAsync(cancellationToken);

            var existingLookup = existingEntries
                .Where(e => e.ColumnName != null)
                .ToDictionary(
                    e => $"{e.SchemaName}.{e.TableName}.{e.ColumnName}".ToUpperInvariant(),
                    StringComparer.OrdinalIgnoreCase);

            int added = 0, updated = 0, removed = 0;
            var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            // Upsert discovered columns
            foreach (var col in sourceColumns)
            {
                var key = $"{col.SchemaName}.{col.TableName}.{col.ColumnName}".ToUpperInvariant();
                processedKeys.Add(key);

                if (existingLookup.TryGetValue(key, out var existing))
                {
                    // Update existing
                    var changed = UpdateDataElement(existing, col, serverName, databaseName);
                    if (changed || existing.IsDeleted)
                    {
                        existing.IsDeleted = false;
                        existing.LastSyncTime = DateTime.UtcNow;
                        existing.LastUpdateTime = DateTime.UtcNow;
                        updated++;
                    }
                }
                else
                {
                    // Add new
                    var newElement = CreateDataElement(col, serverName, databaseName);
                    _context.DataElements.Add(newElement);
                    added++;
                }
            }

            // Soft-delete columns no longer in source
            foreach (var existing in existingEntries.Where(e => e.ColumnName != null && !e.IsDeleted))
            {
                var key = $"{existing.SchemaName}.{existing.TableName}.{existing.ColumnName}".ToUpperInvariant();
                if (!processedKeys.Contains(key))
                {
                    existing.IsDeleted = true;
                    existing.LastUpdateTime = DateTime.UtcNow;
                    removed++;
                }
            }

            await _context.SaveChangesAsync(cancellationToken);

            // Update sync history
            syncHistory.SyncEndTime = DateTime.UtcNow;
            syncHistory.Status = "Completed";
            syncHistory.TablesProcessed = sourceColumns.Select(c => $"{c.SchemaName}.{c.TableName}").Distinct().Count();
            syncHistory.ColumnsProcessed = sourceColumns.Count;
            syncHistory.ColumnsAdded = added;
            syncHistory.ColumnsUpdated = updated;
            syncHistory.ColumnsRemoved = removed;

            await _context.SaveChangesAsync(cancellationToken);

            _logger.LogInformation(
                "Sync completed for {Server}/{Database}: {Added} added, {Updated} updated, {Removed} removed",
                serverName, databaseName, added, updated, removed);

            return new SyncResultViewModel
            {
                SyncHistoryId = syncHistory.SyncHistoryId,
                DatabaseServer = serverName,
                DatabaseName = databaseName,
                Success = true,
                TablesProcessed = syncHistory.TablesProcessed ?? 0,
                ColumnsProcessed = syncHistory.ColumnsProcessed ?? 0,
                ColumnsAdded = added,
                ColumnsUpdated = updated,
                ColumnsRemoved = removed,
                Duration = DateTime.UtcNow - startTime
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync failed for {Server}/{Database}", serverName, databaseName);

            syncHistory.SyncEndTime = DateTime.UtcNow;
            syncHistory.Status = "Failed";
            syncHistory.ErrorMessage = ex.Message;
            await _context.SaveChangesAsync(cancellationToken);

            return new SyncResultViewModel
            {
                SyncHistoryId = syncHistory.SyncHistoryId,
                DatabaseServer = serverName,
                DatabaseName = databaseName,
                Success = false,
                ErrorMessage = ex.Message,
                Duration = DateTime.UtcNow - startTime
            };
        }
    }

    public async Task<List<SourceColumnDto>> DiscoverColumnsAsync(
        string connectionString,
        CancellationToken cancellationToken = default)
    {
        const string sql = @"
            SELECT
                s.name AS SchemaName,
                t.name AS TableName,
                c.name AS ColumnName,
                ty.name AS DataType,
                c.max_length AS MaxLength,
                c.precision AS [Precision],
                c.scale AS Scale,
                c.is_nullable AS IsNullable,
                c.is_identity AS IsIdentity,
                CASE WHEN pk.column_id IS NOT NULL THEN 1 ELSE 0 END AS IsPrimaryKey,
                c.column_id AS OrdinalPosition,
                fk.ForeignKeyTo
            FROM sys.tables t
            INNER JOIN sys.schemas s ON t.schema_id = s.schema_id
            INNER JOIN sys.columns c ON t.object_id = c.object_id
            INNER JOIN sys.types ty ON c.user_type_id = ty.user_type_id
            LEFT JOIN (
                SELECT ic.object_id, ic.column_id
                FROM sys.index_columns ic
                INNER JOIN sys.indexes i ON ic.object_id = i.object_id AND ic.index_id = i.index_id
                WHERE i.is_primary_key = 1
            ) pk ON c.object_id = pk.object_id AND c.column_id = pk.column_id
            LEFT JOIN (
                SELECT
                    fkc.parent_object_id,
                    fkc.parent_column_id,
                    OBJECT_SCHEMA_NAME(fkc.referenced_object_id) + '.' +
                    OBJECT_NAME(fkc.referenced_object_id) + '.' +
                    COL_NAME(fkc.referenced_object_id, fkc.referenced_column_id) AS ForeignKeyTo
                FROM sys.foreign_key_columns fkc
            ) fk ON c.object_id = fk.parent_object_id AND c.column_id = fk.parent_column_id
            WHERE t.is_ms_shipped = 0
            ORDER BY s.name, t.name, c.column_id";

        var columns = new List<SourceColumnDto>();

        await using var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(cancellationToken);

        await using var command = new SqlCommand(sql, connection);
        command.CommandTimeout = 120; // 2 minutes for large databases

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        while (await reader.ReadAsync(cancellationToken))
        {
            columns.Add(new SourceColumnDto
            {
                SchemaName = reader.GetString(0),
                TableName = reader.GetString(1),
                ColumnName = reader.GetString(2),
                DataType = reader.GetString(3),
                MaxLength = reader.IsDBNull(4) ? null : reader.GetInt16(4),
                Precision = reader.IsDBNull(5) ? null : (int)reader.GetByte(5),
                Scale = reader.IsDBNull(6) ? null : (int)reader.GetByte(6),
                IsNullable = reader.GetBoolean(7),
                IsIdentity = reader.GetBoolean(8),
                IsPrimaryKey = reader.GetInt32(9) == 1,
                OrdinalPosition = reader.GetInt32(10),
                ForeignKeyTo = reader.IsDBNull(11) ? null : reader.GetString(11)
            });
        }

        return columns;
    }

    public async Task<List<SyncHistoryViewModel>> GetRecentSyncHistoryAsync(
        int count = 10,
        CancellationToken cancellationToken = default)
    {
        return await _context.SyncHistory
            .OrderByDescending(h => h.SyncStartTime)
            .Take(count)
            .Select(h => new SyncHistoryViewModel
            {
                SyncHistoryId = h.SyncHistoryId,
                DatabaseServer = h.DatabaseServer,
                DatabaseName = h.DatabaseName,
                SyncStartTime = h.SyncStartTime,
                SyncEndTime = h.SyncEndTime,
                TablesProcessed = h.TablesProcessed,
                ColumnsProcessed = h.ColumnsProcessed,
                ColumnsAdded = h.ColumnsAdded,
                ColumnsUpdated = h.ColumnsUpdated,
                ColumnsRemoved = h.ColumnsRemoved,
                Status = h.Status,
                ErrorMessage = h.ErrorMessage
            })
            .ToListAsync(cancellationToken);
    }

    private static DataElement CreateDataElement(SourceColumnDto col, string serverName, string databaseName)
    {
        return new DataElement
        {
            DataElementName = FormatColumnName(col.ColumnName),
            DataElementType = "Column",
            DataType = FormatDataType(col),
            DatabaseServer = serverName,
            DatabaseName = databaseName,
            SchemaName = col.SchemaName,
            TableName = col.TableName,
            ColumnName = col.ColumnName,
            MaxLength = col.MaxLength,
            Precision = col.Precision,
            Scale = col.Scale,
            IsNullable = col.IsNullable,
            IsPrimaryKey = col.IsPrimaryKey,
            ForeignKeyTo = col.ForeignKeyTo,
            CreateTime = DateTime.UtcNow,
            LastUpdateTime = DateTime.UtcNow,
            LastSyncTime = DateTime.UtcNow
        };
    }

    private static bool UpdateDataElement(DataElement existing, SourceColumnDto col, string serverName, string databaseName)
    {
        bool changed = false;

        var newDataType = FormatDataType(col);
        if (existing.DataType != newDataType) { existing.DataType = newDataType; changed = true; }
        if (existing.MaxLength != col.MaxLength) { existing.MaxLength = col.MaxLength; changed = true; }
        if (existing.Precision != col.Precision) { existing.Precision = col.Precision; changed = true; }
        if (existing.Scale != col.Scale) { existing.Scale = col.Scale; changed = true; }
        if (existing.IsNullable != col.IsNullable) { existing.IsNullable = col.IsNullable; changed = true; }
        if (existing.IsPrimaryKey != col.IsPrimaryKey) { existing.IsPrimaryKey = col.IsPrimaryKey; changed = true; }
        if (existing.ForeignKeyTo != col.ForeignKeyTo) { existing.ForeignKeyTo = col.ForeignKeyTo; changed = true; }

        return changed;
    }

    private static string FormatDataType(SourceColumnDto col)
    {
        var baseType = col.DataType.ToUpperInvariant();

        return baseType switch
        {
            "VARCHAR" or "CHAR" => col.MaxLength == -1
                ? $"{baseType}(MAX)"
                : $"{baseType}({col.MaxLength})",
            "NVARCHAR" or "NCHAR" => col.MaxLength == -1
                ? $"{baseType}(MAX)"
                : $"{baseType}({col.MaxLength / 2})", // NVARCHAR stores 2 bytes per char
            "DECIMAL" or "NUMERIC" => $"{baseType}({col.Precision},{col.Scale})",
            "DATETIME2" => col.Scale > 0 ? $"{baseType}({col.Scale})" : baseType,
            _ => baseType
        };
    }

    private static string FormatColumnName(string columnName)
    {
        // Convert snake_case or camelCase to Title Case with spaces
        var result = System.Text.RegularExpressions.Regex.Replace(
            columnName,
            "([a-z])([A-Z])",
            "$1 $2");

        result = result.Replace("_", " ");

        // Title case
        return System.Globalization.CultureInfo.CurrentCulture.TextInfo
            .ToTitleCase(result.ToLowerInvariant());
    }
}
```

### 3.3 Register Service in Program.cs

Add to **src/NetSqlDataDicV2.Web/Program.cs**:
```csharp
builder.Services.AddScoped<IDatabaseSyncService, DatabaseSyncService>();
```

### 3.4 Create Sync Controller

**src/NetSqlDataDicV2.Web/Controllers/SyncController.cs:**
```csharp
using Microsoft.AspNetCore.Mvc;
using NetSqlDataDicV2.Web.Services;

namespace NetSqlDataDicV2.Web.Controllers;

public class SyncController : Controller
{
    private readonly IDatabaseSyncService _syncService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<SyncController> _logger;

    public SyncController(
        IDatabaseSyncService syncService,
        IConfiguration configuration,
        ILogger<SyncController> logger)
    {
        _syncService = syncService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task<IActionResult> Index()
    {
        var history = await _syncService.GetRecentSyncHistoryAsync(20);

        ViewBag.SourceServer = _configuration["SourceDatabase:Server"] ?? "localhost";
        ViewBag.SourceDatabase = _configuration["SourceDatabase:Database"] ?? "";

        return View(history);
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Execute(CancellationToken cancellationToken)
    {
        try
        {
            var connectionString = _configuration.GetConnectionString("SourceDatabase");
            var serverName = _configuration["SourceDatabase:Server"] ?? "localhost";
            var databaseName = _configuration["SourceDatabase:Database"] ?? "Unknown";

            if (string.IsNullOrEmpty(connectionString))
            {
                return Json(new { success = false, error = "Source database connection string not configured" });
            }

            var result = await _syncService.SyncDatabaseAsync(
                connectionString,
                serverName,
                databaseName,
                cancellationToken);

            return Json(new
            {
                success = result.Success,
                error = result.ErrorMessage,
                tablesProcessed = result.TablesProcessed,
                columnsProcessed = result.ColumnsProcessed,
                columnsAdded = result.ColumnsAdded,
                columnsUpdated = result.ColumnsUpdated,
                columnsRemoved = result.ColumnsRemoved,
                duration = result.Duration.TotalSeconds
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Sync execution failed");
            return Json(new { success = false, error = ex.Message });
        }
    }
}
```

### 3.5 Create Sync View

**src/NetSqlDataDicV2.Web/Views/Sync/Index.cshtml:**
```html
@model List<NetSqlDataDicV2.Web.Models.ViewModels.SyncHistoryViewModel>
@{
    ViewData["Title"] = "Database Sync";
}

<h2>Database Synchronization</h2>

<div class="row">
    <div class="col-md-6">
        <div class="card mb-4">
            <div class="card-header">
                <h5 class="mb-0">Sync Configuration</h5>
            </div>
            <div class="card-body">
                <dl class="row mb-0">
                    <dt class="col-sm-4">Server</dt>
                    <dd class="col-sm-8"><code>@ViewBag.SourceServer</code></dd>

                    <dt class="col-sm-4">Database</dt>
                    <dd class="col-sm-8"><code>@ViewBag.SourceDatabase</code></dd>
                </dl>

                <hr />

                <form id="syncForm">
                    @Html.AntiForgeryToken()
                    <button type="submit" id="syncButton" class="btn btn-primary">
                        <span class="spinner-border spinner-border-sm d-none" role="status"></span>
                        <span class="btn-text">Sync Now</span>
                    </button>
                </form>

                <div id="syncResult" class="mt-3 d-none">
                    <div class="alert" role="alert">
                        <h6 class="alert-heading mb-2"></h6>
                        <div class="result-details"></div>
                    </div>
                </div>
            </div>
        </div>
    </div>

    <div class="col-md-6">
        <div class="card mb-4">
            <div class="card-header">
                <h5 class="mb-0">Quick Stats</h5>
            </div>
            <div class="card-body">
                @if (Model.Any())
                {
                    var lastSync = Model.First();
                    <dl class="row mb-0">
                        <dt class="col-sm-6">Last Sync</dt>
                        <dd class="col-sm-6">@lastSync.SyncStartTime.ToString("yyyy-MM-dd HH:mm")</dd>

                        <dt class="col-sm-6">Status</dt>
                        <dd class="col-sm-6">
                            <span class="badge @lastSync.StatusBadgeClass">@lastSync.Status</span>
                        </dd>

                        <dt class="col-sm-6">Tables Processed</dt>
                        <dd class="col-sm-6">@(lastSync.TablesProcessed ?? 0)</dd>

                        <dt class="col-sm-6">Columns Processed</dt>
                        <dd class="col-sm-6">@(lastSync.ColumnsProcessed ?? 0)</dd>
                    </dl>
                }
                else
                {
                    <p class="text-muted mb-0">No sync history available. Run your first sync!</p>
                }
            </div>
        </div>
    </div>
</div>

<div class="card">
    <div class="card-header">
        <h5 class="mb-0">Sync History</h5>
    </div>
    <div class="card-body p-0">
        <div class="table-responsive">
            <table class="table table-striped table-hover mb-0">
                <thead class="table-light">
                    <tr>
                        <th>Started</th>
                        <th>Server / Database</th>
                        <th>Status</th>
                        <th>Duration</th>
                        <th>Tables</th>
                        <th>Columns</th>
                        <th>Added</th>
                        <th>Updated</th>
                        <th>Removed</th>
                    </tr>
                </thead>
                <tbody>
                    @if (!Model.Any())
                    {
                        <tr>
                            <td colspan="9" class="text-center text-muted py-4">
                                No sync history yet. Click "Sync Now" to get started.
                            </td>
                        </tr>
                    }
                    else
                    {
                        @foreach (var item in Model)
                        {
                            <tr>
                                <td>@item.SyncStartTime.ToString("yyyy-MM-dd HH:mm:ss")</td>
                                <td>
                                    <small>@item.DatabaseServer</small><br />
                                    <strong>@item.DatabaseName</strong>
                                </td>
                                <td>
                                    <span class="badge @item.StatusBadgeClass">@item.Status</span>
                                    @if (!string.IsNullOrEmpty(item.ErrorMessage))
                                    {
                                        <br /><small class="text-danger">@item.ErrorMessage</small>
                                    }
                                </td>
                                <td>@item.DurationDisplay</td>
                                <td>@(item.TablesProcessed ?? 0)</td>
                                <td>@(item.ColumnsProcessed ?? 0)</td>
                                <td>
                                    @if (item.ColumnsAdded > 0)
                                    {
                                        <span class="text-success">+@item.ColumnsAdded</span>
                                    }
                                    else
                                    {
                                        <span class="text-muted">0</span>
                                    }
                                </td>
                                <td>
                                    @if (item.ColumnsUpdated > 0)
                                    {
                                        <span class="text-warning">~@item.ColumnsUpdated</span>
                                    }
                                    else
                                    {
                                        <span class="text-muted">0</span>
                                    }
                                </td>
                                <td>
                                    @if (item.ColumnsRemoved > 0)
                                    {
                                        <span class="text-danger">-@item.ColumnsRemoved</span>
                                    }
                                    else
                                    {
                                        <span class="text-muted">0</span>
                                    }
                                </td>
                            </tr>
                        }
                    }
                </tbody>
            </table>
        </div>
    </div>
</div>

@section Scripts {
<script>
    $(document).ready(function () {
        $("#syncForm").on("submit", function (e) {
            e.preventDefault();

            var $btn = $("#syncButton");
            var $spinner = $btn.find(".spinner-border");
            var $text = $btn.find(".btn-text");
            var $result = $("#syncResult");

            // Disable button and show spinner
            $btn.prop("disabled", true);
            $spinner.removeClass("d-none");
            $text.text("Syncing...");
            $result.addClass("d-none");

            $.ajax({
                url: "@Url.Action("Execute", "Sync")",
                type: "POST",
                data: $(this).serialize(),
                success: function (data) {
                    var $alert = $result.find(".alert");
                    var $heading = $alert.find(".alert-heading");
                    var $details = $alert.find(".result-details");

                    if (data.success) {
                        $alert.removeClass("alert-danger").addClass("alert-success");
                        $heading.text("Sync Completed Successfully!");
                        $details.html(
                            '<ul class="mb-0">' +
                            '<li>Tables processed: <strong>' + data.tablesProcessed + '</strong></li>' +
                            '<li>Columns processed: <strong>' + data.columnsProcessed + '</strong></li>' +
                            '<li>Added: <strong class="text-success">' + data.columnsAdded + '</strong></li>' +
                            '<li>Updated: <strong class="text-warning">' + data.columnsUpdated + '</strong></li>' +
                            '<li>Removed: <strong class="text-danger">' + data.columnsRemoved + '</strong></li>' +
                            '<li>Duration: <strong>' + data.duration.toFixed(1) + ' seconds</strong></li>' +
                            '</ul>'
                        );
                    } else {
                        $alert.removeClass("alert-success").addClass("alert-danger");
                        $heading.text("Sync Failed");
                        $details.html('<p class="mb-0">' + (data.error || "Unknown error occurred") + '</p>');
                    }

                    $result.removeClass("d-none");

                    // Reload page after 2 seconds to show updated history
                    setTimeout(function () {
                        location.reload();
                    }, 2000);
                },
                error: function (xhr, status, error) {
                    var $alert = $result.find(".alert");
                    $alert.removeClass("alert-success").addClass("alert-danger");
                    $alert.find(".alert-heading").text("Error");
                    $alert.find(".result-details").html('<p class="mb-0">' + error + '</p>');
                    $result.removeClass("d-none");
                },
                complete: function () {
                    $btn.prop("disabled", false);
                    $spinner.addClass("d-none");
                    $text.text("Sync Now");
                }
            });
        });
    });
</script>
}
```

## Deliverables

- [ ] SourceColumnDto for metadata from source DB
- [ ] SyncResultViewModel and SyncHistoryViewModel
- [ ] IDatabaseSyncService interface
- [ ] DatabaseSyncService with SQL metadata discovery
- [ ] SyncController with Execute action
- [ ] Sync view with history table
- [ ] AJAX-based sync execution with progress feedback

## Verification

1. Configure SourceDatabase connection string in appsettings.json
2. Navigate to /Sync
3. Click "Sync Now"
4. Should see spinner while syncing
5. Should see success message with statistics
6. Page should reload showing sync history
7. Navigate to /DataDictionary - should see synced data
