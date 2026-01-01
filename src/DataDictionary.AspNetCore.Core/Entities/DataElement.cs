namespace DataDictionary.AspNetCore.Core.Entities;

public class DataElement
{
    public int DataElementId { get; set; }
    public string DataElementName { get; set; } = string.Empty;
    public string? DataElementType { get; set; }
    public string? DataType { get; set; }
    public string? DataPurpose { get; set; }
    public string? EntityPurpose { get; set; }

    // Location
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }

    // Metadata
    public string? OriginalDataSource { get; set; }
    public string? Notes { get; set; }
    public string? ForeignKeyTo { get; set; }
    public long? RowCount { get; set; }
    public bool IsNullable { get; set; } = true;
    public bool IsPrimaryKey { get; set; }
    public int? MaxLength { get; set; }
    public int? Precision { get; set; }
    public int? Scale { get; set; }

    // Audit
    public DateTime CreateTime { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdateTime { get; set; } = DateTime.UtcNow;
    public DateTime? LastSyncTime { get; set; }
    public bool IsDeleted { get; set; }

    // Navigation property for notes
    public ICollection<DataElementNote> DataElementNotes { get; set; } = new List<DataElementNote>();
}
