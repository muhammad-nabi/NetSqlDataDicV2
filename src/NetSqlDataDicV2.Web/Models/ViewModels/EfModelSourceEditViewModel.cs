using System.ComponentModel.DataAnnotations;

namespace NetSqlDataDicV2.Web.Models.ViewModels;

/// <summary>
/// View model for editing an EF model source.
/// </summary>
public class EfModelSourceEditViewModel
{
    public int Id { get; set; }

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

    [Display(Name = "Active")]
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Indicates if a connection string is already stored (for UI display).
    /// </summary>
    public bool HasConnectionString { get; set; }
}
