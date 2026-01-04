using DataDictionary.AspNetCore.Core.Configuration;

namespace DataDictionary.AspNetCore.Configuration;

/// <summary>
/// Configuration options for the Data Dictionary package.
/// </summary>
public class DataDictionaryOptions
{
    public const string SectionName = "DataDictionary";

    /// <summary>
    /// The area name for routing. Default: "DataDictionary"
    /// </summary>
    public string AreaName { get; set; } = "DataDictionary";

    /// <summary>
    /// The route prefix for all Data Dictionary routes. Default: "tools/datadictionary"
    /// </summary>
    public string RoutePrefix { get; set; } = "tools/datadictionary";

    /// <summary>
    /// The connection string name to use. Default: "DataDictionary"
    /// </summary>
    public string ConnectionStringName { get; set; } = "DataDictionary";

    /// <summary>
    /// Whether to auto-migrate database on startup. Default: true
    /// </summary>
    public bool AutoMigrate { get; set; } = true;

    /// <summary>
    /// Enable the database sync feature. Default: true
    /// </summary>
    public bool EnableSyncFeature { get; set; } = true;

    /// <summary>
    /// Enable the EF model comparison feature. Default: true
    /// </summary>
    public bool EnableComparisonFeature { get; set; } = true;

    /// <summary>
    /// Enable the EF model sources management. Default: true
    /// </summary>
    public bool EnableEfModelSources { get; set; } = true;

    /// <summary>
    /// DLL security options for assembly loading.
    /// </summary>
    public DllSecurityOptions DllSecurity { get; set; } = new();

    /// <summary>
    /// Whether to require authorization for Data Dictionary routes. Default: false (opt-in)
    /// Set to true to require authentication for all routes.
    /// </summary>
    public bool RequireAuthorization { get; set; } = false;

    /// <summary>
    /// The authorization policy name to use. Default: null (uses default policy)
    /// Only applies when RequireAuthorization is true.
    /// </summary>
    public string? AuthorizationPolicy { get; set; }

    /// <summary>
    /// Roles required to access Data Dictionary routes. Default: null (no role restriction)
    /// Only applies when RequireAuthorization is true. Takes precedence over AuthorizationPolicy.
    /// </summary>
    public string[]? RequiredRoles { get; set; }
}
