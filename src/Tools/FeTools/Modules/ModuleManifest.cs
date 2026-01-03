using System.IO;
using System.Linq;
using System.Text.Json;
using YamlDotNet.RepresentationModel;

namespace FeTools.Modules;

internal sealed class ModuleManifest
{
    public string SourcePath { get; init; } = string.Empty;
    public string Name { get; init; } = string.Empty;
    public string Version { get; init; } = string.Empty;
    public string EngineVersion { get; init; } = string.Empty;
    public List<string> Dependencies { get; } = new();
    public List<ModulePhase> Phases { get; } = new();
    public List<string> Components { get; } = new();
    public List<ModuleSystem> Systems { get; } = new();
    public List<string> RequiredServices { get; } = new();
    public List<string> OptionalServices { get; } = new();
    public List<string> DescriptorManifests { get; } = new();
    public List<string> MetadataConfigs { get; } = new();

    public static ModuleManifest Load(string path)
    {
        var extension = Path.GetExtension(path);
        JsonElement root;
        if (extension.Equals(".yaml", StringComparison.OrdinalIgnoreCase) || extension.Equals(".yml", StringComparison.OrdinalIgnoreCase))
        {
            root = LoadYaml(path);
        }
        else
        {
            using var stream = File.OpenRead(path);
            using var document = JsonDocument.Parse(stream);
            root = document.RootElement.Clone();
        }
        var manifest = new ModuleManifest
        {
            SourcePath = path,
            Name = root.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
            Version = root.TryGetProperty("version", out var versionProp) ? versionProp.GetString() ?? string.Empty : string.Empty,
            EngineVersion = root.TryGetProperty("engineVersion", out var engineProp) ? engineProp.GetString() ?? string.Empty : string.Empty
        };

        if (root.TryGetProperty("dependencies", out var dependenciesProp) && dependenciesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in dependenciesProp.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String)
                {
                    manifest.Dependencies.Add(entry.GetString() ?? string.Empty);
                }
            }
        }

        if (root.TryGetProperty("components", out var componentsProp) && componentsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in componentsProp.EnumerateArray())
            {
                if (entry.ValueKind == JsonValueKind.String)
                {
                    manifest.Components.Add(entry.GetString() ?? string.Empty);
                }
            }
        }

        if (root.TryGetProperty("systems", out var systemsProp) && systemsProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in systemsProp.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var name = entry.TryGetProperty("name", out var nameElement) ? nameElement.GetString() ?? string.Empty : string.Empty;
                var typeName = entry.TryGetProperty("type", out var typeElement) ? typeElement.GetString() ?? string.Empty : string.Empty;
                if (string.IsNullOrWhiteSpace(typeName))
                {
                    typeName = name;
                }
                var phase = entry.TryGetProperty("phase", out var phaseElement) ? phaseElement.GetString() ?? string.Empty : string.Empty;
                var system = new ModuleSystem
                {
                    Name = name,
                    Type = typeName,
                    Phase = phase
                };

                manifest.Systems.Add(system);
            }
        }

        if (root.TryGetProperty("phases", out var phasesProp) && phasesProp.ValueKind == JsonValueKind.Array)
        {
            foreach (var entry in phasesProp.EnumerateArray())
            {
                if (entry.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                manifest.Phases.Add(new ModulePhase
                {
                    Name = entry.TryGetProperty("name", out var phaseName) ? phaseName.GetString() ?? string.Empty : string.Empty,
                    InsertAfter = entry.TryGetProperty("insertAfter", out var afterElement) ? afterElement.GetString() : null
                });
            }
        }

        if (root.TryGetProperty("services", out var servicesProp))
        {
            PopulateServices(manifest, servicesProp);
        }

        if (root.TryGetProperty("descriptorManifests", out var descriptorManifestsProp))
        {
            PopulateDescriptorManifests(manifest, descriptorManifestsProp);
        }
        else if (root.TryGetProperty("descriptorManifest", out var descriptorManifestProp))
        {
            PopulateDescriptorManifests(manifest, descriptorManifestProp);
        }

        if (root.TryGetProperty("metadataConfigs", out var metadataConfigsProp))
        {
            PopulateMetadataConfigs(manifest, metadataConfigsProp);
        }

        return manifest;
    }

    private static JsonElement LoadYaml(string path)
    {
        using var reader = new StreamReader(path);
        var yaml = new YamlStream();
        yaml.Load(reader);
        if (yaml.Documents.Count == 0)
        {
            throw new InvalidOperationException($"YAML manifest '{path}' is empty.");
        }

        var rootNode = yaml.Documents[0].RootNode;
        var obj = ConvertYamlNode(rootNode);
        return JsonSerializer.SerializeToElement(obj ?? new object());
    }

    private static object? ConvertYamlNode(YamlNode node)
    {
        switch (node)
        {
            case YamlScalarNode scalar:
                return ParseScalar(scalar);
            case YamlSequenceNode sequence:
                return sequence.Children.Select(ConvertYamlNode).ToList();
            case YamlMappingNode mapping:
                var dictionary = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                foreach (var entry in mapping.Children)
                {
                    if (entry.Key is YamlScalarNode keyNode)
                    {
                        var key = keyNode.Value ?? string.Empty;
                        dictionary[key] = ConvertYamlNode(entry.Value);
                    }
                }

                return dictionary;
            default:
                return null;
        }
    }

    private static object? ParseScalar(YamlScalarNode scalar)
    {
        if (scalar.Style == YamlDotNet.Core.ScalarStyle.Plain)
        {
            if (bool.TryParse(scalar.Value, out var boolResult))
            {
                return boolResult;
            }

            if (int.TryParse(scalar.Value, out var intResult))
            {
                return intResult;
            }

            if (double.TryParse(scalar.Value, out var doubleResult))
            {
                return doubleResult;
            }
        }

        return scalar.Value;
    }

    private static void PopulateServices(ModuleManifest manifest, JsonElement servicesProp)
    {
        switch (servicesProp.ValueKind)
        {
            case JsonValueKind.Object:
                AddServicesFromObject(manifest, servicesProp);
                break;
            case JsonValueKind.Array:
                foreach (var entry in servicesProp.EnumerateArray())
                {
                    if (entry.ValueKind == JsonValueKind.Object)
                    {
                        AddServicesFromObject(manifest, entry);
                    }
                }
                break;
        }
    }

    private static void AddServicesFromObject(ModuleManifest manifest, JsonElement element)
    {
        if (element.TryGetProperty("requires", out var requires))
        {
            if (requires.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in requires.EnumerateArray())
                {
                    if (child.ValueKind == JsonValueKind.String)
                    {
                        manifest.RequiredServices.Add(child.GetString() ?? string.Empty);
                    }
                }
            }
            else if (requires.ValueKind == JsonValueKind.String)
            {
                manifest.RequiredServices.Add(requires.GetString() ?? string.Empty);
            }
        }

        if (element.TryGetProperty("optional", out var optional))
        {
            if (optional.ValueKind == JsonValueKind.Array)
            {
                foreach (var child in optional.EnumerateArray())
                {
                    if (child.ValueKind == JsonValueKind.String)
                    {
                        manifest.OptionalServices.Add(child.GetString() ?? string.Empty);
                    }
                }
            }
            else if (optional.ValueKind == JsonValueKind.String)
            {
                manifest.OptionalServices.Add(optional.GetString() ?? string.Empty);
            }
        }
    }

    private static void PopulateDescriptorManifests(ModuleManifest manifest, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    AddDescriptorManifest(manifest, child);
                }
                break;
            default:
                AddDescriptorManifest(manifest, element);
                break;
        }
    }

    private static void AddDescriptorManifest(ModuleManifest manifest, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var resolved = ResolveRelativePath(manifest.SourcePath, value);
        manifest.DescriptorManifests.Add(resolved);
    }

    private static void PopulateMetadataConfigs(ModuleManifest manifest, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Array:
                foreach (var child in element.EnumerateArray())
                {
                    AddMetadataConfig(manifest, child);
                }
                break;
            default:
                AddMetadataConfig(manifest, element);
                break;
        }
    }

    private static void AddMetadataConfig(ModuleManifest manifest, JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.String)
        {
            return;
        }

        var value = element.GetString();
        if (string.IsNullOrWhiteSpace(value))
        {
            return;
        }

        var resolved = ResolveRelativePath(manifest.SourcePath, value);
        manifest.MetadataConfigs.Add(resolved);
    }

    private static string ResolveRelativePath(string manifestPath, string relativePath)
    {
        var normalized = relativePath.Replace('\\', Path.DirectorySeparatorChar)
            .Replace('/', Path.DirectorySeparatorChar);
        if (Path.IsPathRooted(normalized))
        {
            return Path.GetFullPath(normalized);
        }

        var directory = Path.GetDirectoryName(Path.GetFullPath(manifestPath));
        directory ??= Directory.GetCurrentDirectory();
        return Path.GetFullPath(Path.Combine(directory, normalized));
    }
}

internal sealed class ModulePhase
{
    public string Name { get; init; } = string.Empty;
    public string? InsertAfter { get; init; }
}

internal sealed class ModuleSystem
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = string.Empty;
    public string Phase { get; init; } = string.Empty;
}
