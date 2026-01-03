using System.IO;

namespace FactoryEngine.Core.Services.Asset;

public sealed class TextureFileLoader : IAssetLoader<TextureAsset>
{
    public TextureAsset Load(AssetRecord record, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(record);
        var fullPath = Path.Combine(rootPath, record.Path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Texture asset '{fullPath}' not found");
        }

        var bytes = File.ReadAllBytes(fullPath);
        return new TextureAsset(fullPath, bytes, record.Metadata);
    }
}
