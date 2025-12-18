namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class DataElementViewModel
{
    public int DataElementId { get; set; }
    public string DataElementName { get; set; } = string.Empty;
    public string? DataElementType { get; set; }
    public string? DataType { get; set; }
    public string? DataPurpose { get; set; }
    public string? EntityPurpose { get; set; }
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? OriginalDataSource { get; set; }
    public string? Notes { get; set; }
    public string? ForeignKeyTo { get; set; }
    public long? RowCount { get; set; }
    public bool IsNullable { get; set; }
    public bool IsPrimaryKey { get; set; }
    public int? MaxLength { get; set; }
    public DateTime CreateTime { get; set; }
    public DateTime LastUpdateTime { get; set; }
    public DateTime? LastSyncTime { get; set; }
    public bool IsDeleted { get; set; }

    // Note count (populated from DataElementNotes)
    public int NoteCount { get; set; }

    // Computed display properties
    public string FullyQualifiedName =>
        string.IsNullOrEmpty(ColumnName)
            ? $"[{SchemaName}].[{TableName}]"
            : $"[{SchemaName}].[{TableName}].[{ColumnName}]";

    public string DisplayDataType
    {
        get
        {
            if (string.IsNullOrEmpty(DataType)) return string.Empty;

            var baseType = DataType.ToUpperInvariant();
            if (MaxLength.HasValue && (baseType.Contains("VARCHAR") || baseType.Contains("CHAR")))
            {
                return MaxLength == -1 ? $"{DataType}(MAX)" : $"{DataType}({MaxLength})";
            }
            return DataType;
        }
    }
}
