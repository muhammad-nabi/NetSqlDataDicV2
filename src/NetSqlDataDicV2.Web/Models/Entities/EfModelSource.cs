namespace NetSqlDataDicV2.Web.Models.Entities;

/// <summary>
/// Represents a configured source for EF Core model metadata.
/// </summary>
public class EfModelSource
{
    public int Id { get; set; }

    /// <summary>
    /// Display name for this source (e.g., "Sales API DbContext").
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Provider type: "DynamicDll" (runtime DLL loading for EF model comparison).
    /// </summary>
    public string ProviderType { get; set; } = "DynamicDll";

    /// <summary>
    /// Full path to the DLL file (for DynamicDll provider).
    /// </summary>
    public string? AssemblyPath { get; set; }

    /// <summary>
    /// Fully qualified type name of the DbContext class.
    /// Example: "MyApp.Data.ApplicationDbContext"
    /// </summary>
    public string? DbContextTypeName { get; set; }

    /// <summary>
    /// Connection string for the DbContext (encrypted at rest).
    /// Used to instantiate the DbContext for model reflection.
    /// </summary>
    public string? ConnectionString { get; set; }

    /// <summary>
    /// Target database server name to compare against in Data Dictionary.
    /// </summary>
    public string TargetServer { get; set; } = string.Empty;

    /// <summary>
    /// Target database name to compare against in Data Dictionary.
    /// </summary>
    public string TargetDatabase { get; set; } = string.Empty;

    /// <summary>
    /// Whether this source is active and available for comparison.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// When this source configuration was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// When a comparison was last performed using this source.
    /// </summary>
    public DateTime? LastComparedAt { get; set; }

    /// <summary>
    /// Optional description or notes about this source.
    /// </summary>
    public string? Description { get; set; }
}
