using System;
using System.IO;
using System.Text.Json;
using FactoryEngine.Core.Services.Audio;

namespace FactoryEngine.Core.Services.Asset;

public sealed class SoundBankJsonLoader : IAssetLoader<SoundBank>
{
    public SoundBank Load(AssetRecord record, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(record);
        var fullPath = Path.Combine(rootPath, record.Path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Sound bank file '{fullPath}' not found.");
        }

        using var stream = File.OpenRead(fullPath);
        using var document = JsonDocument.Parse(stream);
        var root = document.RootElement;
        var name = root.TryGetProperty("name", out var nameProp)
            ? nameProp.GetString()
            : Path.GetFileNameWithoutExtension(record.Path);
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new InvalidOperationException($"Sound bank '{record.Path}' is missing a name.");
        }

        var bank = new SoundBank(name);
        if (!root.TryGetProperty("sounds", out var soundsElement) || soundsElement.ValueKind != JsonValueKind.Object)
        {
            return bank;
        }

        foreach (var sound in soundsElement.EnumerateObject())
        {
            var definition = ParseDefinition(sound.Name, sound.Value);
            bank.Sounds[sound.Name] = definition;
        }

        return bank;
    }

    private static SoundDefinition ParseDefinition(string soundName, JsonElement element)
    {
        if (!element.TryGetProperty("asset", out var assetProp) || assetProp.ValueKind != JsonValueKind.String)
        {
            throw new InvalidOperationException($"Sound '{soundName}' is missing an 'asset' reference.");
        }

        var assetValue = assetProp.GetString() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(assetValue))
        {
            throw new InvalidOperationException($"Sound '{soundName}' asset reference cannot be empty.");
        }

        var asset = AssetId.Parse(assetValue);
        var group = element.TryGetProperty("group", out var groupProp)
            ? groupProp.GetString() ?? "sfx"
            : "sfx";
        var volume = element.TryGetProperty("volume", out var volumeProp) && volumeProp.TryGetSingle(out var parsedVolume)
            ? parsedVolume
            : 1f;

        return new SoundDefinition
        {
            Asset = asset,
            Group = string.IsNullOrWhiteSpace(group) ? "sfx" : group,
            Volume = volume
        };
    }
}
