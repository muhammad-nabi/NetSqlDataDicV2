using System.Diagnostics;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataDictionary.AspNetCore.Core.Data;
using DataDictionary.AspNetCore.Core.Models.Dto;
using DataDictionary.AspNetCore.Core.Entities;
using DataDictionary.AspNetCore.Core.Models.ViewModels;

namespace DataDictionary.AspNetCore.Core.Services;

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
            var totalStopwatch = Stopwatch.StartNew();
            var phaseStopwatch = Stopwatch.StartNew();

            _logger.LogInformation("Starting sync for {Server}/{Database}", serverName, databaseName);

            // Phase 1: Discover columns from source database
            var sourceColumns = await DiscoverColumnsAsync(connectionString, cancellationToken);
            var discoveryMs = phaseStopwatch.ElapsedMilliseconds;
            phaseStopwatch.Restart();

            _logger.LogDebug("Discovery completed in {DiscoveryMs}ms - found {Count} columns",
                discoveryMs, sourceColumns.Count);

            // Phase 2: Get existing entries (including soft-deleted for potential restore)
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
            var auditRecords = new List<DataElementAudit>();

            // Upsert discovered columns
            foreach (var col in sourceColumns)
            {
                var key = $"{col.SchemaName}.{col.TableName}.{col.ColumnName}".ToUpperInvariant();
                processedKeys.Add(key);

                if (existingLookup.TryGetValue(key, out var existing))
                {
                    var wasDeleted = existing.IsDeleted;
                    var changes = UpdateDataElement(existing, col, serverName, databaseName);

                    if (wasDeleted)
                    {
                        // Column was soft-deleted, now restored
                        existing.IsDeleted = false;
                        existing.LastSyncTime = DateTime.UtcNow;
                        existing.LastUpdateTime = DateTime.UtcNow;

                        auditRecords.Add(new DataElementAudit
                        {
                            DataElementId = existing.DataElementId,
                            SyncHistoryId = syncHistory.SyncHistoryId,
                            ChangeType = "Restored",
                            PropertyName = null,
                            OldValue = "Deleted",
                            NewValue = "Active",
                            ChangeTime = DateTime.UtcNow
                        });

                        // Also record any property changes during restoration
                        foreach (var change in changes)
                        {
                            auditRecords.Add(new DataElementAudit
                            {
                                DataElementId = existing.DataElementId,
                                SyncHistoryId = syncHistory.SyncHistoryId,
                                ChangeType = "Modified",
                                PropertyName = change.PropertyName,
                                OldValue = change.OldValue,
                                NewValue = change.NewValue,
                                ChangeTime = DateTime.UtcNow
                            });
                        }

                        updated++;
                    }
                    else if (changes.Count > 0)
                    {
                        // Properties changed
                        existing.LastSyncTime = DateTime.UtcNow;
                        existing.LastUpdateTime = DateTime.UtcNow;

                        foreach (var change in changes)
                        {
                            auditRecords.Add(new DataElementAudit
                            {
                                DataElementId = existing.DataElementId,
                                SyncHistoryId = syncHistory.SyncHistoryId,
                                ChangeType = "Modified",
                                PropertyName = change.PropertyName,
                                OldValue = change.OldValue,
                                NewValue = change.NewValue,
                                ChangeTime = DateTime.UtcNow
                            });
                        }

                        updated++;
                    }
                    // else: no changes, no audit record (requirement: skip if no change)
                }
                else
                {
                    // Add new column
                    var newElement = CreateDataElement(col, serverName, databaseName);
                    _context.DataElements.Add(newElement);

                    // Create audit record using navigation property (ID assigned after SaveChanges)
                    auditRecords.Add(new DataElementAudit
                    {
                        DataElement = newElement,
                        SyncHistoryId = syncHistory.SyncHistoryId,
                        ChangeType = "Added",
                        PropertyName = null,
                        OldValue = null,
                        NewValue = FormatDataType(col),
                        ChangeTime = DateTime.UtcNow
                    });

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

                    auditRecords.Add(new DataElementAudit
                    {
                        DataElementId = existing.DataElementId,
                        SyncHistoryId = syncHistory.SyncHistoryId,
                        ChangeType = "Deleted",
                        PropertyName = null,
                        OldValue = "Active",
                        NewValue = "Deleted",
                        ChangeTime = DateTime.UtcNow
                    });

                    removed++;
                }
            }

            // Add all audit records
            _context.DataElementAudits.AddRange(auditRecords);

            var compareMs = phaseStopwatch.ElapsedMilliseconds;
            phaseStopwatch.Restart();

            await _context.SaveChangesAsync(cancellationToken);

            var saveMs = phaseStopwatch.ElapsedMilliseconds;
            phaseStopwatch.Restart();

            // Update sync history
            syncHistory.SyncEndTime = DateTime.UtcNow;
            syncHistory.Status = "Completed";
            syncHistory.TablesProcessed = sourceColumns.Select(c => $"{c.SchemaName}.{c.TableName}").Distinct().Count();
            syncHistory.ColumnsProcessed = sourceColumns.Count;
            syncHistory.ColumnsAdded = added;
            syncHistory.ColumnsUpdated = updated;
            syncHistory.ColumnsRemoved = removed;

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

    public async Task<SyncResultsViewModel?> GetSyncResultsAsync(
        int syncHistoryId,
        CancellationToken cancellationToken = default)
    {
        // Get sync history record
        var syncHistory = await _context.SyncHistory
            .AsNoTracking()
            .FirstOrDefaultAsync(h => h.SyncHistoryId == syncHistoryId, cancellationToken);

        if (syncHistory == null)
        {
            return null;
        }

        // Get all audit records for this sync with DataElement navigation
        var auditItems = await _context.DataElementAudits
            .AsNoTracking()
            .Include(a => a.DataElement)
            .IgnoreQueryFilters()
            .Where(a => a.SyncHistoryId == syncHistoryId)
            .OrderByDescending(a => a.ChangeTime)
            .ThenBy(a => a.DataElement.SchemaName)
            .ThenBy(a => a.DataElement.TableName)
            .ThenBy(a => a.DataElement.ColumnName)
            .Select(a => new SyncAuditItemViewModel
            {
                DataElementAuditId = a.DataElementAuditId,
                DataElementId = a.DataElementId,
                ChangeType = a.ChangeType,
                PropertyName = a.PropertyName,
                OldValue = a.OldValue,
                NewValue = a.NewValue,
                ChangeTime = a.ChangeTime,
                SchemaName = a.DataElement.SchemaName,
                TableName = a.DataElement.TableName,
                ColumnName = a.DataElement.ColumnName ?? string.Empty
            })
            .ToListAsync(cancellationToken);

        // Calculate counts from audit records
        var addedCount = auditItems.Count(a => a.ChangeType == "Added");
        var modifiedCount = auditItems.Count(a => a.ChangeType == "Modified");
        var deletedCount = auditItems.Count(a => a.ChangeType == "Deleted");
        var restoredCount = auditItems.Count(a => a.ChangeType == "Restored");

        return new SyncResultsViewModel
        {
            SyncHistoryId = syncHistory.SyncHistoryId,
            DatabaseServer = syncHistory.DatabaseServer,
            DatabaseName = syncHistory.DatabaseName,
            SyncStartTime = syncHistory.SyncStartTime,
            SyncEndTime = syncHistory.SyncEndTime,
            Status = syncHistory.Status,
            ErrorMessage = syncHistory.ErrorMessage,
            TablesProcessed = syncHistory.TablesProcessed ?? 0,
            ColumnsProcessed = syncHistory.ColumnsProcessed ?? 0,
            AddedCount = addedCount,
            ModifiedCount = modifiedCount,
            DeletedCount = deletedCount,
            RestoredCount = restoredCount,
            AuditItems = auditItems
        };
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

    private static List<PropertyChange> UpdateDataElement(DataElement existing, SourceColumnDto col, string serverName, string databaseName)
    {
        var changes = new List<PropertyChange>();

        var newDataType = FormatDataType(col);
        if (existing.DataType != newDataType)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.DataType),
                OldValue = existing.DataType,
                NewValue = newDataType
            });
            existing.DataType = newDataType;
        }

        if (existing.MaxLength != col.MaxLength)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.MaxLength),
                OldValue = existing.MaxLength?.ToString(),
                NewValue = col.MaxLength?.ToString()
            });
            existing.MaxLength = col.MaxLength;
        }

        if (existing.Precision != col.Precision)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.Precision),
                OldValue = existing.Precision?.ToString(),
                NewValue = col.Precision?.ToString()
            });
            existing.Precision = col.Precision;
        }

        if (existing.Scale != col.Scale)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.Scale),
                OldValue = existing.Scale?.ToString(),
                NewValue = col.Scale?.ToString()
            });
            existing.Scale = col.Scale;
        }

        if (existing.IsNullable != col.IsNullable)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.IsNullable),
                OldValue = existing.IsNullable.ToString(),
                NewValue = col.IsNullable.ToString()
            });
            existing.IsNullable = col.IsNullable;
        }

        if (existing.IsPrimaryKey != col.IsPrimaryKey)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.IsPrimaryKey),
                OldValue = existing.IsPrimaryKey.ToString(),
                NewValue = col.IsPrimaryKey.ToString()
            });
            existing.IsPrimaryKey = col.IsPrimaryKey;
        }

        if (existing.ForeignKeyTo != col.ForeignKeyTo)
        {
            changes.Add(new PropertyChange
            {
                PropertyName = nameof(DataElement.ForeignKeyTo),
                OldValue = existing.ForeignKeyTo,
                NewValue = col.ForeignKeyTo
            });
            existing.ForeignKeyTo = col.ForeignKeyTo;
        }

        return changes;
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
