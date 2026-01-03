using System.Text.Json;
using FactoryEngine.Core.Diagnostics;

namespace FeTools.Descriptors;

internal sealed class DescriptorManifest
{
    public string SourcePath { get; init; } = string.Empty;
    public List<ComponentDescriptorDefinition> Components { get; } = new();

    public static DescriptorManifest Load(string path, NdjsonLogger logger)
    {
        using var stream = File.OpenRead(path);
        using var document = JsonDocument.Parse(stream);
        var manifest = new DescriptorManifest { SourcePath = path };
        var root = document.RootElement;
        if (root.TryGetProperty("components", out var components) && components.ValueKind == JsonValueKind.Array)
        {
            foreach (var componentElement in components.EnumerateArray())
            {
                if (componentElement.ValueKind != JsonValueKind.Object)
                {
                    continue;
                }

                var definition = new ComponentDescriptorDefinition
                {
                    Name = componentElement.TryGetProperty("name", out var nameProp) ? nameProp.GetString() ?? string.Empty : string.Empty,
                    Version = componentElement.TryGetProperty("version", out var versionProp) && versionProp.ValueKind == JsonValueKind.Number
                        ? versionProp.GetInt32()
                        : 1
                };

                if (componentElement.TryGetProperty("fields", out var fieldsProp) && fieldsProp.ValueKind == JsonValueKind.Array)
                {
                    foreach (var fieldElement in fieldsProp.EnumerateArray())
                    {
                        if (fieldElement.ValueKind != JsonValueKind.Object)
                        {
                            continue;
                        }

                        var field = new FieldDefinition
                        {
                            Name = fieldElement.TryGetProperty("name", out var fieldName) ? fieldName.GetString() ?? string.Empty : string.Empty,
                            Type = fieldElement.TryGetProperty("type", out var fieldType) ? fieldType.GetString() ?? "string" : "string",
                            Required = fieldElement.TryGetProperty("required", out var requiredProp) && requiredProp.ValueKind == JsonValueKind.True,
                            Min = fieldElement.TryGetProperty("min", out var minProp) && minProp.ValueKind == JsonValueKind.Number
                                ? minProp.GetDouble()
                                : null,
                            Max = fieldElement.TryGetProperty("max", out var maxProp) && maxProp.ValueKind == JsonValueKind.Number
                                ? maxProp.GetDouble()
                                : null
                        };

                        if (fieldElement.TryGetProperty("allowedValues", out var allowedValues) && allowedValues.ValueKind == JsonValueKind.Array)
                        {
                            foreach (var valueElement in allowedValues.EnumerateArray())
                            {
                                if (valueElement.ValueKind == JsonValueKind.String)
                                {
                                    field.AllowedValues.Add(valueElement.GetString() ?? string.Empty);
                                }
                            }
                        }

                        definition.Fields.Add(field);
                    }
                }

                manifest.Components.Add(definition);
            }
        }
        else
        {
            logger.Warn($"Descriptor manifest '{path}' has no components.");
        }

        return manifest;
    }
}

internal sealed class ComponentDescriptorDefinition
{
    public string Name { get; init; } = string.Empty;
    public int Version { get; init; } = 1;
    public List<FieldDefinition> Fields { get; } = new();
}

internal sealed class FieldDefinition
{
    public string Name { get; init; } = string.Empty;
    public string Type { get; init; } = "string";
    public bool Required { get; init; }
    public List<string> AllowedValues { get; } = new();
    public double? Min { get; init; }
    public double? Max { get; init; }
}
