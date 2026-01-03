using FactoryEngine.Core.Services.Serialization;
using System.IO;

namespace FactoryEngine.Core.Services.Asset;

public sealed class PrefabJsonAssetLoader : IAssetLoader<PrefabDefinition>
{
    public PrefabDefinition Load(AssetRecord record, string rootPath)
    {
        ArgumentNullException.ThrowIfNull(record);
        var fullPath = Path.Combine(rootPath, record.Path);
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Prefab asset '{fullPath}' not found");
        }

        using var stream = File.OpenRead(fullPath);
        return PrefabJsonSerializer.Read(stream);
    }
}
