using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NetSqlDataDicV2.Web.Models.Dto;
using NetSqlDataDicV2.Web.Models.Entities;
using NetSqlDataDicV2.Web.Services.DbContextProviders;

namespace NetSqlDataDicV2.Web.Services;

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
        _logger.LogInformation("Getting EF model columns for source: {Name}", source.Name);

        using var provider = _providerFactory.GetProvider(source);
        using var result = provider.GetDbContext(source);

        if (!result.Success || result.Context == null)
        {
            _logger.LogError("Failed to get DbContext: {Error}", result.ErrorMessage);
            throw new InvalidOperationException(
                $"Failed to load DbContext for source '{source.Name}': {result.ErrorMessage}");
        }

        return ExtractColumnsFromContext(result.Context);
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

            foreach (var property in entityType.GetProperties())
            {
                // Skip shadow properties unless they map to columns
                if (property.IsShadowProperty() && string.IsNullOrEmpty(property.GetColumnName()))
                    continue;

                var columnName = property.GetColumnName();

                // Skip properties without column mappings
                if (string.IsNullOrEmpty(columnName))
                    continue;

                columns.Add(new EfModelColumnDto
                {
                    EntityName = entityType.ClrType.Name,
                    PropertyName = property.Name,
                    ColumnName = columnName,
                    SchemaName = schemaName,
                    TableName = tableName,
                    ClrType = GetFriendlyTypeName(property.ClrType),
                    IsNullable = property.IsNullable,
                    MaxLength = property.GetMaxLength()
                });
            }
        }

        _logger.LogInformation("Extracted {Count} columns from DbContext model", columns.Count);

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
}
