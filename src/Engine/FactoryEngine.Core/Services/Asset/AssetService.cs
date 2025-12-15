namespace FactoryEngine.Core.Services.Asset;

public sealed class AssetService : IAssetService
{
    private readonly Dictionary<string, AssetCatalog> _catalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<AssetId, object> _loaded = new();

    public event Action<AssetId>? AssetReloaded
    {
        add { }
        remove { }
    }

    public void RegisterCatalog(AssetCatalog catalog)
    {
        _catalogs[catalog.Namespace] = catalog;
    }

    public AssetHandle<T> Load<T>(AssetId assetId) where T : class
    {
        if (_loaded.TryGetValue(assetId, out var existing))
        {
            return new AssetHandle<T>(existing as T, string.Empty);
        }

        var catalog = ResolveCatalog(assetId);
        if (!catalog.Assets.TryGetValue(assetId.Name, out var record))
        {
            throw new InvalidOperationException($"Asset '{assetId}' not found.");
        }

        var asset = Activator.CreateInstance(typeof(T));
        if (asset is null)
        {
            throw new InvalidOperationException($"Unable to create asset of type {typeof(T).Name}.");
        }

        _loaded[assetId] = asset;
        return new AssetHandle<T>(asset as T, record.Hash ?? string.Empty);
    }

    public ValueTask<AssetHandle<T>> LoadAsync<T>(AssetId assetId) where T : class
    {
        return ValueTask.FromResult(Load<T>(assetId));
    }

    public void Release<T>(AssetHandle<T> handle) where T : class
    {
        // placeholder
    }

    private AssetCatalog ResolveCatalog(AssetId assetId)
    {
        var key = string.IsNullOrEmpty(assetId.Namespace) ? "default" : assetId.Namespace;
        if (!_catalogs.TryGetValue(key, out var catalog))
        {
            throw new InvalidOperationException($"Asset namespace '{key}' not registered.");
        }

        return catalog;
    }
}
