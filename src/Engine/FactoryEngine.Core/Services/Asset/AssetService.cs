namespace FactoryEngine.Core.Services.Asset;

public sealed class AssetService : IAssetService
{
    private readonly Dictionary<string, AssetCatalog> _catalogs = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<AssetId, object> _loaded = new();
    private readonly Dictionary<Type, object> _loaders = new();

    public event Action<AssetId>? AssetReloaded
    {
        add { }
        remove { }
    }

    public void RegisterCatalog(AssetCatalog catalog)
    {
        if (catalog == null)
        {
            throw new ArgumentNullException(nameof(catalog));
        }

        _catalogs[catalog.Namespace] = catalog;
    }

    public AssetHandle<T> Load<T>(AssetId assetId) where T : class
    {
        if (_loaded.TryGetValue(assetId, out var existing))
        {
            return new AssetHandle<T>(existing as T, string.Empty);
        }

        var (catalog, record) = ResolveCatalog(assetId);
        if (!_loaders.TryGetValue(typeof(T), out var loaderObj))
        {
            throw new InvalidOperationException($"No loader registered for asset type {typeof(T).Name}");
        }

        var loader = (IAssetLoader<T>)loaderObj;
        var asset = loader.Load(record, catalog.RootPath);
        _loaded[assetId] = asset;
        return new AssetHandle<T>(asset, record.Hash ?? string.Empty);
    }

    public ValueTask<AssetHandle<T>> LoadAsync<T>(AssetId assetId) where T : class
    {
        return ValueTask.FromResult(Load<T>(assetId));
    }

    public void Release<T>(AssetHandle<T> handle) where T : class
    {
        // placeholder
    }

    public void RegisterLoader<T>(IAssetLoader<T> loader) where T : class
    {
        if (loader == null)
        {
            throw new ArgumentNullException(nameof(loader));
        }

        _loaders[typeof(T)] = loader;
    }

    private (AssetCatalog, AssetRecord) ResolveCatalog(AssetId assetId)
    {
        var key = string.IsNullOrEmpty(assetId.Namespace) ? "default" : assetId.Namespace;
        if (!_catalogs.TryGetValue(key, out var catalog))
        {
            throw new InvalidOperationException($"Asset namespace '{key}' not registered.");
        }

        if (!catalog.Assets.TryGetValue(assetId.Name, out var record))
        {
            throw new InvalidOperationException($"Asset '{assetId}' not found.");
        }

        return (catalog, record);
    }
}
