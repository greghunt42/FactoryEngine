using System.IO;

namespace FeTools.Modules;

internal static class ModuleManifestDiscovery
{
    private static readonly string[] SearchPatterns =
    {
        "*.module.json",
        "*.module.yaml",
        "*.module.yml",
        "*.manifest.json",
        "*.manifest.yaml",
        "*.manifest.yml",
        "*.json",
        "*.yaml",
        "*.yml"
    };

    public static List<string> EnumerateManifestFiles(string directory)
    {
        var results = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        if (!Directory.Exists(directory))
        {
            return new List<string>();
        }

        foreach (var pattern in SearchPatterns)
        {
            foreach (var file in Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories))
            {
                results.Add(Path.GetFullPath(file));
            }
        }

        return new List<string>(results);
    }
}
