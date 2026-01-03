using System.Reflection;
using System.Runtime.Loader;
using FactoryEngine.Core.Diagnostics;

namespace FeTools.Modules;

internal sealed class TypeLookup
{
    private readonly List<Assembly> _assemblies = new();
    private readonly Dictionary<string, bool> _cache = new(StringComparer.Ordinal);

    public TypeLookup(IEnumerable<string> assemblyPaths, NdjsonLogger logger)
    {
        foreach (var path in assemblyPaths)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            var fullPath = Path.GetFullPath(path);
            if (!File.Exists(fullPath))
            {
                logger.Warn($"Assembly '{path}' not found.");
                continue;
            }

            try
            {
                var assembly = AssemblyLoadContext.Default.LoadFromAssemblyPath(fullPath);
                _assemblies.Add(assembly);
                logger.Info($"Loaded assembly '{assembly.GetName().Name}'.");
            }
            catch (Exception ex)
            {
                logger.Error($"Failed to load assembly '{path}'", ex);
            }
        }
    }

    public bool HasAssemblies => _assemblies.Count > 0;

    public bool TypeExists(string? typeName)
    {
        if (!HasAssemblies)
        {
            return true;
        }

        if (string.IsNullOrWhiteSpace(typeName))
        {
            return false;
        }

        if (_cache.TryGetValue(typeName, out var exists))
        {
            return exists;
        }

        foreach (var assembly in _assemblies)
        {
            var type = assembly.GetType(typeName, throwOnError: false, ignoreCase: false);
            if (type is not null)
            {
                _cache[typeName] = true;
                return true;
            }
        }

        _cache[typeName] = false;
        return false;
    }
}
