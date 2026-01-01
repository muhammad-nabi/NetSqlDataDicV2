namespace DataDictionary.AspNetCore.Core.Models.ViewModels;

public class DataElementUpdateViewModel
{
    public int DataElementId { get; set; }
    public string? DataPurpose { get; set; }
    public string? EntityPurpose { get; set; }
    public string? OriginalDataSource { get; set; }
    public string? Notes { get; set; }
}
