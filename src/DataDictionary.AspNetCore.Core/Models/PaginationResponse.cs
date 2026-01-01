namespace DataDictionary.AspNetCore.Core.Models;

/// <summary>
/// Response model for paginated data. Replaces Kendo DataSourceResult.
/// </summary>
public class PaginationResponse<T>
{
    public IEnumerable<T> Data { get; set; } = Enumerable.Empty<T>();
    public int Total { get; set; }
    public int Page { get; set; }
    public int PageSize { get; set; }
    public int TotalPages => (int)Math.Ceiling((double)Total / PageSize);

    public static PaginationResponse<T> Create(IEnumerable<T> data, int total, PaginationRequest request)
    {
        return new PaginationResponse<T>
        {
            Data = data,
            Total = total,
            Page = request.Page,
            PageSize = request.PageSize
        };
    }
}
