using System.Text.Json;
using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Services.Asset;
using FeTools.Modules;

namespace FeTools.Metadata;

internal static class MetadataConfigLoader
{
    private static readonly string[] DefaultTextureFormats = { "png", "dds", "tga", "bin", "placeholder" };
    private static readonly string[] DefaultAudioGroups = { "sfx", "music", "ui", "ambience", "dialog", "voice" };
    private static readonly string WorkspaceConfigPath = Path.Combine("data", "catalogs", "asset-metadata.config.json");

    public static IReadOnlyCollection<string> DiscoverWorkspaceConfigs(NdjsonLogger logger)
    {
        var list = new List<string>();
        var fullPath = Path.GetFullPath(WorkspaceConfigPath);
        if (File.Exists(fullPath))
        {
            list.Add(fullPath);
            logger.Info($"Discovered workspace metadata config '{fullPath}'.");
        }
        return list;
    }

    public static IReadOnlyCollection<string> DiscoverModuleConfigs(NdjsonLogger logger)
    {
        var directory = Path.Combine("data", "modules");
        if (!Directory.Exists(directory))
        {
            return Array.Empty<string>();
        }

        var manifests = ModuleManifestDiscovery.EnumerateManifestFiles(directory);
        if (manifests.Count == 0)
        {
            return Array.Empty<string>();
        }

        var configs = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var manifestPath in manifests)
        {
            try
            {
                var manifest = ModuleManifest.Load(manifestPath);
                foreach (var config in manifest.MetadataConfigs)
                {
                    if (configs.Add(config))
                    {
                        logger.Info($"Discovered metadata config '{config}' from module '{manifest.Name}'.");
                    }
                }
            }
            catch (Exception ex)
            {
                logger.Warn($"Failed to parse module manifest '{manifestPath}' while discovering metadata configs: {ex.Message}");
            }
        }

        return configs.ToList();
    }

    public static AssetMetadataRules BuildRules(IEnumerable<string> configPaths, List<string> warnings, NdjsonLogger logger)
    {
        var textureFormats = new List<string>(DefaultTextureFormats);
        var audioGroups = new List<string>(DefaultAudioGroups);
        var defaultAudioGroup = DefaultAudioGroups[0];

        foreach (var path in configPaths ?? Enumerable.Empty<string>())
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                continue;
            }

            if (!File.Exists(path))
            {
                Warn($"Metadata config '{path}' not found.", warnings, logger);
                continue;
            }

            try
            {
                using var document = JsonDocument.Parse(File.ReadAllText(path));
                var root = document.RootElement;
                if (root.TryGetProperty("textureFormats", out var textureNode) && textureNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in textureNode.EnumerateArray())
                    {
                        if (entry.ValueKind == JsonValueKind.String)
                        {
                            AddUnique(textureFormats, entry.GetString());
                        }
                    }
                }

                if (root.TryGetProperty("audioGroups", out var audioNode) && audioNode.ValueKind == JsonValueKind.Array)
                {
                    foreach (var entry in audioNode.EnumerateArray())
                    {
                        if (entry.ValueKind == JsonValueKind.String)
                        {
                            AddUnique(audioGroups, entry.GetString());
                        }
                    }
                }

                if (root.TryGetProperty("defaultAudioGroup", out var defaultNode) && defaultNode.ValueKind == JsonValueKind.String)
                {
                    var candidate = defaultNode.GetString();
                    if (!string.IsNullOrWhiteSpace(candidate))
                    {
                        defaultAudioGroup = candidate!;
                        AddUnique(audioGroups, candidate);
                    }
                }
            }
            catch (Exception ex)
            {
                Warn($"Failed to parse metadata config '{path}': {ex.Message}", warnings, logger);
            }
        }

        return new AssetMetadataRules(textureFormats, audioGroups, defaultAudioGroup);
    }

    private static void AddUnique(List<string> list, string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        if (!list.Any(existing => string.Equals(existing, value, StringComparison.OrdinalIgnoreCase)))
        {
            list.Add(value);
        }
    }

    private static void Warn(string message, List<string> warnings, NdjsonLogger logger)
    {
        warnings.Add(message);
        logger.Warn(message);
    }
}
