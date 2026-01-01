namespace DataDictionary.AspNetCore.Core.Models.ViewModels;

/// <summary>
/// Result of a validation operation.
/// </summary>
public class ValidationResultViewModel
{
    public bool IsValid { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}
