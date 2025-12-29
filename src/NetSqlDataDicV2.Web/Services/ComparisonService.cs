using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.ViewModels;

namespace NetSqlDataDicV2.Web.Services;

public class ComparisonService : IComparisonService
{
    private readonly IDataDictionaryService _dataDictionaryService;
    private readonly IEfModelService _efModelService;
    private readonly IEfModelSourceService _sourceService;
    private readonly ILogger<ComparisonService> _logger;

    // SQL Server to CLR type mapping
    private static readonly Dictionary<string, string[]> SqlToClrTypeMap =
        new(StringComparer.OrdinalIgnoreCase)
    {
        ["INT"] = ["int", "Int32", "int?"],
        ["BIGINT"] = ["long", "Int64", "long?"],
        ["SMALLINT"] = ["short", "Int16", "short?"],
        ["TINYINT"] = ["byte", "Byte", "byte?"],
        ["BIT"] = ["bool", "Boolean", "bool?"],
        ["DECIMAL"] = ["decimal", "Decimal", "decimal?"],
        ["NUMERIC"] = ["decimal", "Decimal", "decimal?"],
        ["MONEY"] = ["decimal", "Decimal", "decimal?"],
        ["SMALLMONEY"] = ["decimal", "Decimal", "decimal?"],
        ["FLOAT"] = ["double", "Double", "double?"],
        ["REAL"] = ["float", "Single", "float?"],
        ["DATETIME"] = ["DateTime", "DateTime?"],
        ["DATETIME2"] = ["DateTime", "DateTime?"],
        ["SMALLDATETIME"] = ["DateTime", "DateTime?"],
        ["DATE"] = ["DateTime", "DateOnly", "DateTime?", "DateOnly?"],
        ["TIME"] = ["TimeSpan", "TimeOnly", "TimeSpan?", "TimeOnly?"],
        ["DATETIMEOFFSET"] = ["DateTimeOffset", "DateTimeOffset?"],
        ["VARCHAR"] = ["string", "String"],
        ["NVARCHAR"] = ["string", "String"],
        ["CHAR"] = ["string", "String"],
        ["NCHAR"] = ["string", "String"],
        ["TEXT"] = ["string", "String"],
        ["NTEXT"] = ["string", "String"],
        ["UNIQUEIDENTIFIER"] = ["Guid", "Guid?"],
        ["VARBINARY"] = ["byte[]", "Byte[]"],
        ["BINARY"] = ["byte[]", "Byte[]"],
        ["IMAGE"] = ["byte[]", "Byte[]"],
        ["XML"] = ["string", "String"]
    };

    public ComparisonService(
        IDataDictionaryService dataDictionaryService,
        IEfModelService efModelService,
        IEfModelSourceService sourceService,
        ILogger<ComparisonService> logger)
    {
        _dataDictionaryService = dataDictionaryService;
        _efModelService = efModelService;
        _sourceService = sourceService;
        _logger = logger;
    }

    public async Task<ComparisonResultViewModel> CompareAsync(
        int sourceId,
        CancellationToken cancellationToken = default)
    {
        var source = await _sourceService.GetByIdAsync(sourceId, cancellationToken);

        if (source == null)
        {
            throw new ArgumentException($"EfModelSource with ID {sourceId} not found.");
        }

        _logger.LogInformation("Starting comparison using source {SourceId} - {Name}",
            sourceId, source.Name);

        // Get data dictionary entries for the target database
        var dictionaryEntries = await _dataDictionaryService.GetByDatabaseAsync(
            source.TargetServer, source.TargetDatabase, cancellationToken);

        var columnEntries = dictionaryEntries
            .Where(d => !string.IsNullOrEmpty(d.ColumnName))
            .ToList();

        // Get EF model columns from the configured source
        var efColumns = _efModelService.GetEfModelColumns(source);

        _logger.LogInformation(
            "Comparing {DictCount} dictionary entries with {EfCount} EF model columns (source: {Source})",
            columnEntries.Count, efColumns.Count, source.Name);

        // Build lookup dictionaries
        var dictLookup = columnEntries.ToDictionary(
            d => $"{d.SchemaName}.{d.TableName}.{d.ColumnName}".ToUpperInvariant(),
            StringComparer.OrdinalIgnoreCase);

        var efLookup = efColumns
            .Where(e => !string.IsNullOrEmpty(e.TableName) && !string.IsNullOrEmpty(e.ColumnName))
            .ToDictionary(
                e => $"{e.SchemaName ?? "dbo"}.{e.TableName}.{e.ColumnName}".ToUpperInvariant(),
                StringComparer.OrdinalIgnoreCase);

        // Build set of tables that exist in EF model (case-insensitive)
        var efTableKeys = efColumns
            .Where(e => !string.IsNullOrEmpty(e.TableName))
            .Select(e => $"{e.SchemaName ?? "dbo"}.{e.TableName}".ToUpperInvariant())
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        var results = new List<ComparisonItemViewModel>();
        var processedKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // Track columns per skipped table
        var skippedTableCounts = new Dictionary<string, (string Schema, string Table, int Count)>(
            StringComparer.OrdinalIgnoreCase);

        // Compare dictionary entries against EF model
        foreach (var dict in columnEntries)
        {
            var columnKey = $"{dict.SchemaName}.{dict.TableName}.{dict.ColumnName}".ToUpperInvariant();
            var tableKey = $"{dict.SchemaName}.{dict.TableName}".ToUpperInvariant();

            processedKeys.Add(columnKey);

            // Check if TABLE exists in EF model first
            if (!efTableKeys.Contains(tableKey))
            {
                // Entire table not in EF - add to skipped tables count
                if (skippedTableCounts.TryGetValue(tableKey, out var existing))
                {
                    skippedTableCounts[tableKey] = (existing.Schema, existing.Table, existing.Count + 1);
                }
                else
                {
                    skippedTableCounts[tableKey] = (dict.SchemaName, dict.TableName, 1);
                }
                continue; // Don't add to main comparison results
            }

            // Table IS in EF - proceed with column-level comparison
            if (efLookup.TryGetValue(columnKey, out var ef))
            {
                var isTypeCompatible = IsTypeCompatible(dict.DataType, ef.ClrType);
                var constraintMismatches = new List<ConstraintMismatchDetail>();

                // Only check constraints if type is compatible
                if (isTypeCompatible)
                {
                    constraintMismatches = CompareConstraints(dict, ef);
                }

                // Determine status (TypeMismatch takes precedence)
                ComparisonStatus status;
                string? notes = null;

                if (!isTypeCompatible)
                {
                    status = ComparisonStatus.TypeMismatch;
                    notes = $"DB type '{dict.DataType}' may not match CLR type '{ef.ClrType}'";
                }
                else if (constraintMismatches.Count > 0)
                {
                    status = ComparisonStatus.ConstraintMismatch;
                    notes = string.Join("; ", constraintMismatches.Select(c => c.DisplayText));
                }
                else
                {
                    status = ComparisonStatus.Match;
                }

                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = dict.SchemaName,
                    TableName = dict.TableName,
                    ColumnName = dict.ColumnName,
                    DatabaseType = dict.DataType,
                    EfClrType = ef.ClrType,
                    EfEntityName = ef.EntityName,
                    EfPropertyName = ef.PropertyName,
                    Status = status,
                    Notes = notes,
                    ConstraintMismatches = constraintMismatches
                });
            }
            else
            {
                // Column missing but TABLE is in EF
                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = dict.SchemaName,
                    TableName = dict.TableName,
                    ColumnName = dict.ColumnName,
                    DatabaseType = dict.DataType,
                    Status = ComparisonStatus.MissingInEfModel,
                    Notes = "Column exists in database but not mapped in EF Core model."
                });
            }
        }

        // Find columns in EF but not in dictionary
        foreach (var ef in efColumns.Where(e =>
            !string.IsNullOrEmpty(e.TableName) && !string.IsNullOrEmpty(e.ColumnName)))
        {
            var key = $"{ef.SchemaName ?? "dbo"}.{ef.TableName}.{ef.ColumnName}".ToUpperInvariant();

            if (!processedKeys.Contains(key))
            {
                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = ef.SchemaName ?? "dbo",
                    TableName = ef.TableName!,
                    ColumnName = ef.ColumnName,
                    EfClrType = ef.ClrType,
                    EfEntityName = ef.EntityName,
                    EfPropertyName = ef.PropertyName,
                    Status = ComparisonStatus.MissingInDatabase,
                    Notes = "Property exists in EF Core model but column not found in database."
                });
            }
        }

        // Build skipped tables list from counts
        var skippedTables = skippedTableCounts.Values
            .Select(t => new SkippedTableViewModel
            {
                SchemaName = t.Schema,
                TableName = t.Table,
                ColumnCount = t.Count
            })
            .OrderBy(t => t.SchemaName)
            .ThenBy(t => t.TableName)
            .ToList();

        var result = new ComparisonResultViewModel
        {
            DatabaseServer = source.TargetServer,
            DatabaseName = source.TargetDatabase,
            ComparisonTime = DateTime.UtcNow,
            Items = results
                .OrderBy(r => r.Status)
                .ThenBy(r => r.SchemaName)
                .ThenBy(r => r.TableName)
                .ThenBy(r => r.ColumnName)
                .ToList(),
            SkippedTables = skippedTables
        };

        // Update last compared timestamp
        await _sourceService.UpdateLastComparedAsync(sourceId, cancellationToken);

        _logger.LogInformation(
            "Comparison complete (source {Source}): {Matches} matches, {MissingEf} missing in EF, " +
            "{MissingDb} missing in DB, {TypeMismatches} type mismatches, {ConstraintMismatches} constraint mismatches, " +
            "{SkippedTables} skipped tables ({SkippedColumns} columns)",
            source.Name, result.TotalMatches, result.TotalMissingInEf, result.TotalMissingInDb,
            result.TotalTypeMismatches, result.TotalConstraintMismatches, result.TotalSkippedTables, result.TotalSkippedColumns);

        return result;
    }

    private bool IsTypeCompatible(string? sqlType, string? clrType)
    {
        if (string.IsNullOrEmpty(sqlType) || string.IsNullOrEmpty(clrType))
            return true; // Can't determine, assume compatible

        // Extract base SQL type (remove size specifiers)
        var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();

        // Normalize CLR type (remove nullable indicator for lookup)
        var normalizedClrType = clrType.TrimEnd('?');

        if (SqlToClrTypeMap.TryGetValue(baseSqlType, out var compatibleTypes))
        {
            return compatibleTypes.Any(t =>
                t.Equals(clrType, StringComparison.OrdinalIgnoreCase) ||
                t.Equals(normalizedClrType, StringComparison.OrdinalIgnoreCase));
        }

        // Unknown SQL type - log and assume compatible
        _logger.LogWarning("Unknown SQL type '{SqlType}' - assuming compatible with '{ClrType}'",
            sqlType, clrType);
        return true;
    }

    private static bool IsStringType(string? sqlType)
    {
        if (string.IsNullOrEmpty(sqlType)) return false;
        var upper = sqlType.ToUpperInvariant();
        return upper.Contains("CHAR") || upper.Contains("TEXT");
    }

    private static bool IsUnicodeStringType(string? sqlType)
    {
        if (string.IsNullOrEmpty(sqlType)) return false;
        var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();
        return baseSqlType is "NVARCHAR" or "NCHAR" or "NTEXT";
    }

    private static bool IsDecimalType(string? sqlType)
    {
        if (string.IsNullOrEmpty(sqlType)) return false;
        var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();
        return baseSqlType is "DECIMAL" or "NUMERIC" or "MONEY" or "SMALLMONEY";
    }

    private static bool IsMoneyType(string? sqlType)
    {
        if (string.IsNullOrEmpty(sqlType)) return false;
        var baseSqlType = sqlType.Split('(')[0].ToUpperInvariant();
        return baseSqlType is "MONEY" or "SMALLMONEY";
    }

    /// <summary>
    /// Compares constraints between Data Dictionary and EF model.
    /// Returns list of mismatches (empty if all match).
    /// </summary>
    private List<ConstraintMismatchDetail> CompareConstraints(
        Models.ViewModels.DataElementViewModel dict,
        EfModelColumnDto ef)
    {
        var mismatches = new List<ConstraintMismatchDetail>();

        // Compare MaxLength (only for string types)
        if (IsStringType(dict.DataType))
        {
            var efMaxLength = ef.MaxLength;
            var dbMaxLength = dict.MaxLength;

            // For Unicode types (NVARCHAR, NCHAR, NTEXT), SQL Server stores max_length in bytes
            // (2 bytes per character), while EF Core uses character count
            if (IsUnicodeStringType(dict.DataType) && dbMaxLength.HasValue && dbMaxLength > 0)
            {
                dbMaxLength = dbMaxLength / 2;
            }

            // EF null with DB -1 (MAX) is considered a match
            bool isMaxLengthMatch = efMaxLength == dbMaxLength
                || (efMaxLength == null && dbMaxLength == -1);

            if (!isMaxLengthMatch)
            {
                mismatches.Add(new ConstraintMismatchDetail
                {
                    ConstraintName = "MaxLength",
                    DatabaseValue = dbMaxLength == -1 ? "MAX" : dbMaxLength?.ToString() ?? "null",
                    EfValue = efMaxLength?.ToString() ?? "MAX"
                });
            }
        }

        // Compare IsNullable
        if (dict.IsNullable != ef.IsNullable)
        {
            mismatches.Add(new ConstraintMismatchDetail
            {
                ConstraintName = "IsNullable",
                DatabaseValue = dict.IsNullable.ToString(),
                EfValue = ef.IsNullable.ToString()
            });
        }

        // Compare Precision/Scale (only for decimal/numeric types, NOT money types)
        // MONEY and SMALLMONEY have fixed precision/scale that can't be configured in EF Core
        if (IsDecimalType(dict.DataType) && !IsMoneyType(dict.DataType))
        {
            if (dict.Precision.HasValue || ef.Precision.HasValue)
            {
                if (dict.Precision != ef.Precision)
                {
                    mismatches.Add(new ConstraintMismatchDetail
                    {
                        ConstraintName = "Precision",
                        DatabaseValue = dict.Precision?.ToString() ?? "null",
                        EfValue = ef.Precision?.ToString() ?? "default"
                    });
                }
            }

            if (dict.Scale.HasValue || ef.Scale.HasValue)
            {
                if (dict.Scale != ef.Scale)
                {
                    mismatches.Add(new ConstraintMismatchDetail
                    {
                        ConstraintName = "Scale",
                        DatabaseValue = dict.Scale?.ToString() ?? "null",
                        EfValue = ef.Scale?.ToString() ?? "default"
                    });
                }
            }
        }

        // Compare IsPrimaryKey
        if (dict.IsPrimaryKey != ef.IsPrimaryKey)
        {
            mismatches.Add(new ConstraintMismatchDetail
            {
                ConstraintName = "IsPrimaryKey",
                DatabaseValue = dict.IsPrimaryKey.ToString(),
                EfValue = ef.IsPrimaryKey.ToString()
            });
        }

        return mismatches;
    }
}
