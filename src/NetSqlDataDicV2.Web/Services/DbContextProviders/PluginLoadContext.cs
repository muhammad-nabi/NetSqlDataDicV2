using System.Reflection;
using System.Runtime.Loader;

namespace NetSqlDataDicV2.Web.Services.DbContextProviders;

/// <summary>
/// Custom AssemblyLoadContext for loading plugin assemblies in isolation.
/// Marked as collectible to allow unloading when no longer needed.
/// </summary>
public class PluginLoadContext : AssemblyLoadContext
{
    private readonly AssemblyDependencyResolver _resolver;

    public PluginLoadContext(string pluginPath) : base(isCollectible: true)
    {
        _resolver = new AssemblyDependencyResolver(pluginPath);
    }

    protected override Assembly? Load(AssemblyName assemblyName)
    {
        // First, try to resolve from the plugin's dependencies
        var assemblyPath = _resolver.ResolveAssemblyToPath(assemblyName);

        if (assemblyPath != null)
        {
            return LoadFromAssemblyPath(assemblyPath);
        }

        // For shared framework assemblies, return null to let the default context handle it
        // This prevents loading duplicate copies of Microsoft.EntityFrameworkCore, etc.
        return null;
    }

    protected override IntPtr LoadUnmanagedDll(string unmanagedDllName)
    {
        var libraryPath = _resolver.ResolveUnmanagedDllToPath(unmanagedDllName);

        if (libraryPath != null)
        {
            return LoadUnmanagedDllFromPath(libraryPath);
        }

        return IntPtr.Zero;
    }
}
