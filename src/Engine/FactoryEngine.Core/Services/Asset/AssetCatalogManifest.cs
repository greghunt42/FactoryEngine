using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;

namespace FactoryEngine.Core.Services.Asset;

public static class AssetCatalogManifest
{
    public static AssetCatalog LoadFromJson(string manifestPath)
    {
        ArgumentException.ThrowIfNullOrEmpty(manifestPath);
        using var stream = File.OpenRead(manifestPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var ns = root.GetProperty("namespace").GetString() ?? throw new InvalidOperationException("catalog namespace missing");
        var rootDir = ResolveRoot(manifestPath, root);
        var catalog = new AssetCatalog(ns, rootDir);
        if (!root.TryGetProperty("assets", out var assetsElement))
        {
            return catalog;
        }

        foreach (var assetProperty in assetsElement.EnumerateObject())
        {
            var data = assetProperty.Value;
            var type = data.GetProperty("type").GetString() ?? throw new InvalidOperationException($"Asset '{assetProperty.Name}' missing type");
            var path = data.GetProperty("path").GetString() ?? throw new InvalidOperationException($"Asset '{assetProperty.Name}' missing path");
            var record = new AssetRecord
            {
                Type = type,
                Path = path,
                Hash = data.TryGetProperty("hash", out var hashProp) ? hashProp.GetString() : null,
                Metadata = ReadMetadata(data)
            };
            catalog.Assets[assetProperty.Name] = record;
        }

        return catalog;
    }

    private static string ResolveRoot(string manifestPath, JsonElement root)
    {
        var manifestDirectory = Path.GetDirectoryName(Path.GetFullPath(manifestPath)) ?? Environment.CurrentDirectory;
        if (!root.TryGetProperty("root", out var rootProp) || rootProp.ValueKind != JsonValueKind.String)
        {
            return manifestDirectory;
        }

        var rootValue = rootProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(rootValue))
        {
            return manifestDirectory;
        }

        if (Path.IsPathRooted(rootValue))
        {
            return Path.GetFullPath(rootValue);
        }

        return Path.GetFullPath(Path.Combine(manifestDirectory, rootValue));
    }

    private static Dictionary<string, string>? ReadMetadata(JsonElement element)
    {
        if (!element.TryGetProperty("metadata", out var metadataElement) || metadataElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var metadata = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var property in metadataElement.EnumerateObject())
        {
            metadata[property.Name] = property.Value.ValueKind switch
            {
                JsonValueKind.String => property.Value.GetString() ?? string.Empty,
                JsonValueKind.Number => property.Value.GetRawText(),
                JsonValueKind.True => "true",
                JsonValueKind.False => "false",
                _ => property.Value.GetRawText()
            };
        }

        return metadata;
    }
}
