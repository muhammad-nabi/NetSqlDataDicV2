namespace DataDictionary.AspNetCore.Core.Models.Dto;

/// <summary>
/// Information about a discovered DbContext type in an assembly.
/// </summary>
public class DbContextInfo
{
    /// <summary>
    /// Short name of the DbContext class.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Fully qualified type name including namespace.
    /// </summary>
    public string FullName { get; set; } = string.Empty;

    /// <summary>
    /// Number of entity types (DbSet properties) in the context.
    /// </summary>
    public int EntityCount { get; set; }

    /// <summary>
    /// Names of the entity types in the context.
    /// </summary>
    public List<string> EntityNames { get; set; } = new();

    /// <summary>
    /// Whether the DbContext has a parameterless constructor.
    /// </summary>
    public bool HasParameterlessConstructor { get; set; }

    /// <summary>
    /// Whether the DbContext has a constructor accepting DbContextOptions.
    /// </summary>
    public bool HasOptionsConstructor { get; set; }
}
