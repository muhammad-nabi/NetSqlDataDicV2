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
                var isCompatible = IsTypeCompatible(dict.DataType, ef.ClrType);

                results.Add(new ComparisonItemViewModel
                {
                    SchemaName = dict.SchemaName,
                    TableName = dict.TableName,
                    ColumnName = dict.ColumnName,
                    DatabaseType = dict.DataType,
                    EfClrType = ef.ClrType,
                    EfEntityName = ef.EntityName,
                    EfPropertyName = ef.PropertyName,
                    Status = isCompatible ? ComparisonStatus.Match : ComparisonStatus.TypeMismatch,
                    Notes = isCompatible ? null : $"DB type '{dict.DataType}' may not match CLR type '{ef.ClrType}'"
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
            "{MissingDb} missing in DB, {Mismatches} type mismatches, {SkippedTables} skipped tables ({SkippedColumns} columns)",
            source.Name, result.TotalMatches, result.TotalMissingInEf, result.TotalMissingInDb,
            result.TotalTypeMismatches, result.TotalSkippedTables, result.TotalSkippedColumns);

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
}
