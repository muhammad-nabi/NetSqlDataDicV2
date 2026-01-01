using System.ComponentModel.DataAnnotations;

namespace DataDictionary.AspNetCore.Core.Models.ViewModels;

/// <summary>
/// View model for creating a new EF model source.
/// </summary>
public class EfModelSourceCreateViewModel
{
    [Required]
    [MaxLength(200)]
    [Display(Name = "Name")]
    public string Name { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Provider Type")]
    public string ProviderType { get; set; } = "DynamicDll";

    [MaxLength(500)]
    [Display(Name = "Assembly Path")]
    public string? AssemblyPath { get; set; }

    [MaxLength(500)]
    [Display(Name = "DbContext Type Name")]
    public string? DbContextTypeName { get; set; }

    [MaxLength(2000)]
    [Display(Name = "Connection String")]
    [DataType(DataType.Password)]
    public string? ConnectionString { get; set; }

    [Required]
    [MaxLength(200)]
    [Display(Name = "Target Server")]
    public string TargetServer { get; set; } = string.Empty;

    [Required]
    [MaxLength(200)]
    [Display(Name = "Target Database")]
    public string TargetDatabase { get; set; } = string.Empty;

    [MaxLength(1000)]
    [Display(Name = "Description")]
    public string? Description { get; set; }
}
