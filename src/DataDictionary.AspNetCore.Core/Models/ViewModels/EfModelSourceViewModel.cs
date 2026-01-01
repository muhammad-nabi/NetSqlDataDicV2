namespace DataDictionary.AspNetCore.Core.Models.ViewModels;

/// <summary>
/// View model for displaying EF model source information.
/// </summary>
public class EfModelSourceViewModel
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string ProviderType { get; set; } = string.Empty;
    public string? AssemblyPath { get; set; }
    public string? DbContextTypeName { get; set; }
    public string TargetServer { get; set; } = string.Empty;
    public string TargetDatabase { get; set; } = string.Empty;
    public string? Description { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? LastComparedAt { get; set; }
    public bool HasConnectionString { get; set; }

    /// <summary>
    /// Display string for the target database.
    /// </summary>
    public string TargetDisplay => $"{TargetServer}/{TargetDatabase}";

    /// <summary>
    /// Display string showing last compared time or "Never".
    /// </summary>
    public string LastComparedDisplay => LastComparedAt?.ToString("g") ?? "Never";
}
