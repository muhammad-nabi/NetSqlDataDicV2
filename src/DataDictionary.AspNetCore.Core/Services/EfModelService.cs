using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using DataDictionary.AspNetCore.Core.Models.Dto;
using DataDictionary.AspNetCore.Core.Entities;
using DataDictionary.AspNetCore.Core.Services.DbContextProviders;

namespace DataDictionary.AspNetCore.Core.Services;

public class EfModelService : IEfModelService
{
    private readonly IDbContextProviderFactory _providerFactory;
    private readonly ILogger<EfModelService> _logger;

    public EfModelService(
        IDbContextProviderFactory providerFactory,
        ILogger<EfModelService> logger)
    {
        _providerFactory = providerFactory;
        _logger = logger;
    }

    /// <summary>
    /// Gets EF model columns from a configured EfModelSource.
    /// </summary>
    public List<EfModelColumnDto> GetEfModelColumns(EfModelSource source)
    {
        var stopwatch = Stopwatch.StartNew();
        _logger.LogInformation("Getting EF model columns for source: {Name}", source.Name);

        using var provider = _providerFactory.GetProvider(source);
        using var result = provider.GetDbContext(source);

        var loadMs = stopwatch.ElapsedMilliseconds;

        if (!result.Success || result.Context == null)
        {
            _logger.LogError("Failed to get DbContext after {LoadMs}ms: {Error}", loadMs, result.ErrorMessage);
            throw new InvalidOperationException(
                $"Failed to load DbContext for source '{source.Name}': {result.ErrorMessage}");
        }

        _logger.LogDebug("DbContext loaded in {LoadMs}ms for source {SourceName}", loadMs, source.Name);

        var columns = ExtractColumnsFromContext(result.Context);

        stopwatch.Stop();
        _logger.LogInformation(
            "EF model extraction completed for {SourceName} in {ElapsedMs}ms: {ColumnCount} columns from {EntityCount} entities",
            source.Name, stopwatch.ElapsedMilliseconds, columns.Count,
            columns.Select(c => c.EntityName).Distinct().Count());

        return columns;
    }

    /// <summary>
    /// Discovers DbContext types in an assembly.
    /// </summary>
    public List<DbContextInfo> DiscoverDbContexts(string assemblyPath)
    {
        return _providerFactory.DiscoverDbContexts(assemblyPath);
    }

    /// <summary>
    /// Extracts column metadata from a DbContext's model.
    /// </summary>
    private List<EfModelColumnDto> ExtractColumnsFromContext(DbContext context)
    {
        var columns = new List<EfModelColumnDto>();
        var model = context.Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            // Skip owned types (they're part of the owner entity's table)
            if (entityType.IsOwned())
                continue;

            // Skip entities without CLR type
            if (entityType.ClrType == null)
                continue;

            var tableName = entityType.GetTableName();
            var schemaName = entityType.GetSchema() ?? "dbo";

            // Skip entities without a table mapping (e.g., query types)
            if (string.IsNullOrEmpty(tableName))
                continue;

            _logger.LogDebug("Processing entity {Entity} -> {Schema}.{Table}",
                entityType.ClrType.Name, schemaName, tableName);

            // Get primary key properties for this entity
            var primaryKey = entityType.FindPrimaryKey();
            var primaryKeyPropertyNames = primaryKey?.Properties
                .Select(p => p.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase)
                ?? new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var property in entityType.GetProperties())
            {
                // Skip shadow properties unless they map to columns
                if (property.IsShadowProperty() && string.IsNullOrEmpty(property.GetColumnName()))
                    continue;

                var columnName = property.GetColumnName();

                // Skip properties without column mappings
                if (string.IsNullOrEmpty(columnName))
                    continue;

                // Extract precision and scale
                var extractedPrecision = property.GetPrecision() is { } p ? (int?)p : null;
                var extractedScale = property.GetScale() is { } s ? (int?)s : null;

                // Fallback: parse from column type if not set via HasPrecision()
                // This handles cases like .HasColumnType("decimal(8, 2)")
                if (extractedPrecision == null || extractedScale == null)
                {
                    var columnType = property.GetColumnType();
                    var parsed = ParsePrecisionScaleFromColumnType(columnType);
                    extractedPrecision ??= parsed.Precision;
                    extractedScale ??= parsed.Scale;
                }

                columns.Add(new EfModelColumnDto
                {
                    EntityName = entityType.ClrType.Name,
                    PropertyName = property.Name,
                    ColumnName = columnName,
                    SchemaName = schemaName,
                    TableName = tableName,
                    ClrType = GetFriendlyTypeName(property.ClrType),
                    IsNullable = property.IsNullable,
                    MaxLength = property.GetMaxLength(),
                    Precision = extractedPrecision,
                    Scale = extractedScale,
                    IsPrimaryKey = primaryKeyPropertyNames.Contains(property.Name)
                });
            }
        }

        return columns;
    }

    /// <summary>
    /// Converts CLR type to friendly C# type name.
    /// </summary>
    private static string GetFriendlyTypeName(Type type)
    {
        var nullableUnderlying = Nullable.GetUnderlyingType(type);
        var actualType = nullableUnderlying ?? type;

        var friendlyName = actualType.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "String" => "string",
            "Boolean" => "bool",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "Guid" => "Guid",
            "DateTime" => "DateTime",
            "DateTimeOffset" => "DateTimeOffset",
            "DateOnly" => "DateOnly",
            "TimeOnly" => "TimeOnly",
            "TimeSpan" => "TimeSpan",
            "Byte[]" => "byte[]",
            _ => actualType.Name
        };

        return nullableUnderlying != null ? $"{friendlyName}?" : friendlyName;
    }

    /// <summary>
    /// Parses precision and scale from column type strings like "decimal(8, 2)" or "numeric(18,4)".
    /// Used as fallback when HasColumnType() is used instead of HasPrecision().
    /// </summary>
    private static (int? Precision, int? Scale) ParsePrecisionScaleFromColumnType(string? columnType)
    {
        if (string.IsNullOrEmpty(columnType)) return (null, null);

        // Match patterns like "decimal(8,2)", "numeric(18, 4)", etc.
        var match = Regex.Match(
            columnType,
            @"(?:decimal|numeric)\s*\(\s*(\d+)\s*,\s*(\d+)\s*\)",
            RegexOptions.IgnoreCase);

        if (match.Success &&
            int.TryParse(match.Groups[1].Value, out var precision) &&
            int.TryParse(match.Groups[2].Value, out var scale))
        {
            return (precision, scale);
        }

        return (null, null);
    }
}
