using System.Collections.Generic;
using System.IO;

namespace FactoryEngine.Core.Services.Asset;

public static class AssetCatalogDiscovery
{
    private static readonly string[] Patterns =
    {
        "*.catalog.json",
        "*.catalog.yaml",
        "*.catalog.yml"
    };

    public static List<string> EnumerateCatalogFiles(string directory)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        foreach (var pattern in Patterns)
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
            {
                results.Add(Path.GetFullPath(file));
            }
        }

        return new List<string>(results);
    }
}
