namespace NetSqlDataDicV2.Web.Models.ViewModels;

public class ComparisonResultViewModel
{
    public string DatabaseServer { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public DateTime ComparisonTime { get; set; } = DateTime.UtcNow;
    public List<ComparisonItemViewModel> Items { get; set; } = new();

    /// <summary>
    /// Tables in Data Dictionary that have no corresponding entity in DbContext.
    /// These tables exist in the database but are not mapped in the EF model.
    /// </summary>
    public List<SkippedTableViewModel> SkippedTables { get; set; } = new();

    public int TotalItems => Items.Count;
    public int TotalMatches => Items.Count(i => i.Status == ComparisonStatus.Match);
    public int TotalMissingInEf => Items.Count(i => i.Status == ComparisonStatus.MissingInEfModel);
    public int TotalMissingInDb => Items.Count(i => i.Status == ComparisonStatus.MissingInDatabase);
    public int TotalTypeMismatches => Items.Count(i => i.Status == ComparisonStatus.TypeMismatch);
    public int TotalSkippedTables => SkippedTables.Count;
    public int TotalSkippedColumns => SkippedTables.Sum(t => t.ColumnCount);
}

/// <summary>
/// Represents a table that exists in the Data Dictionary but has no
/// corresponding entity in the DbContext (entirely skipped from comparison).
/// </summary>
public class SkippedTableViewModel
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public int ColumnCount { get; set; }
    public string FullTableName => $"{SchemaName}.{TableName}";
}

public class ComparisonItemViewModel
{
    public string SchemaName { get; set; } = "dbo";
    public string TableName { get; set; } = string.Empty;
    public string? ColumnName { get; set; }
    public string? DatabaseType { get; set; }
    public string? EfClrType { get; set; }
    public string? EfEntityName { get; set; }
    public string? EfPropertyName { get; set; }
    public ComparisonStatus Status { get; set; }
    public string? Notes { get; set; }

    public string StatusDisplay => Status switch
    {
        ComparisonStatus.Match => "Match",
        ComparisonStatus.MissingInEfModel => "Missing in EF Model",
        ComparisonStatus.MissingInDatabase => "Missing in Database",
        ComparisonStatus.TypeMismatch => "Type Mismatch",
        _ => "Unknown"
    };

    public string StatusBadgeClass => Status switch
    {
        ComparisonStatus.Match => "bg-success",
        ComparisonStatus.MissingInEfModel => "bg-warning",
        ComparisonStatus.MissingInDatabase => "bg-danger",
        ComparisonStatus.TypeMismatch => "bg-info",
        _ => "bg-secondary"
    };
}

public enum ComparisonStatus
{
    Match,
    MissingInEfModel,
    MissingInDatabase,
    TypeMismatch
}
