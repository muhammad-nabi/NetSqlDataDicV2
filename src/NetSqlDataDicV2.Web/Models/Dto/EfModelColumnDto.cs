namespace NetSqlDataDicV2.Web.Models.Dto;

public class EfModelColumnDto
{
    public string EntityName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public string ClrType { get; set; } = string.Empty;
    public string? TableName { get; set; }
    public string? ColumnName { get; set; }
    public string? SchemaName { get; set; }
    public bool IsNullable { get; set; }
    public int? MaxLength { get; set; }
}
