namespace FactoryEngine.Core.Services.Asset;

public sealed class FileAssetLoader : IAssetLoader<byte[]>
{
    public byte[] Load(AssetRecord record, string rootPath)
    {
        var fullPath = Path.Combine(rootPath, record.Path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Asset file '{fullPath}' not found");
        }

        return File.ReadAllBytes(fullPath);
    }
}
