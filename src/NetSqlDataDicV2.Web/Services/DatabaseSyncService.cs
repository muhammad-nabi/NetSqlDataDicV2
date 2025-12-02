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
