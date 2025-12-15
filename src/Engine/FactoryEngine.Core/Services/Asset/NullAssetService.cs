namespace FactoryEngine.Core.Services.Asset;

public sealed class NullAssetService : IAssetService
{
    public event Action<AssetId>? AssetReloaded;

    public AssetHandle<T> Load<T>(AssetId assetId) where T : class => new(null, string.Empty);

    public ValueTask<AssetHandle<T>> LoadAsync<T>(AssetId assetId) where T : class => ValueTask.FromResult(Load<T>(assetId));

    public void Release<T>(AssetHandle<T> handle) where T : class
    {
        // Nothing to release in the placeholder implementation.
    }

    public void NotifyReload(AssetId assetId) => AssetReloaded?.Invoke(assetId);
}
