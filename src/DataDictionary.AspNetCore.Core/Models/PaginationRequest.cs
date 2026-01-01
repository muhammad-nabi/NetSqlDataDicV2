namespace DataDictionary.AspNetCore.Core.Models;

/// <summary>
/// Request model for paginated data queries. Replaces Kendo DataSourceRequest.
/// </summary>
public class PaginationRequest
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
    public string? SortField { get; set; }
    public string SortDirection { get; set; } = "asc";
    public string? SearchTerm { get; set; }
    public Dictionary<string, string>? Filters { get; set; }

    public int Skip => (Page - 1) * PageSize;
}
