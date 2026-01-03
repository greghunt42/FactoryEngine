using System.IO;

namespace FactoryEngine.Core.Services.Asset;

public sealed class AudioFileLoader : IAssetLoader<AudioClipAsset>
{
    public AudioClipAsset Load(AssetRecord record, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(record);
        var fullPath = Path.Combine(rootPath, record.Path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Audio asset '{fullPath}' not found");
        }

        var bytes = File.ReadAllBytes(fullPath);
        return new AudioClipAsset(fullPath, bytes, record.Metadata);
    }
}
