using Microsoft.EntityFrameworkCore;
using NetSqlDataDicV2.SourceModels;
using NetSqlDataDicV2.Web.Models.Dto;

namespace NetSqlDataDicV2.Web.Services;

public class EfModelService : IEfModelService
{
    private readonly SourceDbContext _sourceContext;
    private readonly ILogger<EfModelService> _logger;

    public EfModelService(
        SourceDbContext sourceContext,
        ILogger<EfModelService> logger)
    {
        _sourceContext = sourceContext;
        _logger = logger;
    }

    public List<EfModelColumnDto> GetEfModelColumns()
    {
        var results = new List<EfModelColumnDto>();
        var model = _sourceContext.Model;

        foreach (var entityType in model.GetEntityTypes())
        {
            // Skip shadow types and query types
            if (entityType.IsOwned() || entityType.ClrType == null)
                continue;

            var tableName = entityType.GetTableName();
            var schemaName = entityType.GetSchema() ?? "dbo";

            _logger.LogDebug("Processing entity {Entity} -> {Schema}.{Table}",
                entityType.ClrType.Name, schemaName, tableName);

            foreach (var property in entityType.GetProperties())
            {
                // Skip shadow properties
                if (property.IsShadowProperty())
                    continue;

                var columnName = property.GetColumnName();
                var maxLength = property.GetMaxLength();

                results.Add(new EfModelColumnDto
                {
                    EntityName = entityType.ClrType.Name,
                    PropertyName = property.Name,
                    ClrType = GetClrTypeName(property.ClrType),
                    ColumnName = columnName,
                    TableName = tableName,
                    SchemaName = schemaName,
                    IsNullable = property.IsNullable,
                    MaxLength = maxLength
                });
            }
        }

        _logger.LogInformation("Extracted {Count} columns from EF Core model", results.Count);
        return results;
    }

    private static string GetClrTypeName(Type type)
    {
        // Handle nullable types
        var underlyingType = Nullable.GetUnderlyingType(type);
        if (underlyingType != null)
        {
            return $"{GetSimpleTypeName(underlyingType)}?";
        }

        return GetSimpleTypeName(type);
    }

    private static string GetSimpleTypeName(Type type)
    {
        // Map common types to C# keywords
        return type.Name switch
        {
            "Int32" => "int",
            "Int64" => "long",
            "Int16" => "short",
            "Byte" => "byte",
            "Boolean" => "bool",
            "String" => "string",
            "Decimal" => "decimal",
            "Double" => "double",
            "Single" => "float",
            "DateTime" => "DateTime",
            "DateTimeOffset" => "DateTimeOffset",
            "TimeSpan" => "TimeSpan",
            "Guid" => "Guid",
            "Byte[]" => "byte[]",
            _ => type.Name
        };
    }
}
