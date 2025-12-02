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
