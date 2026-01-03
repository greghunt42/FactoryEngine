using System.Threading.Tasks;

namespace FactoryEngine.Core.Services.Asset;

public interface IAssetService
{
    void RegisterCatalog(AssetCatalog catalog);
    void RegisterLoader<T>(IAssetLoader<T> loader) where T : class;
    AssetHandle<T> Load<T>(AssetId assetId) where T : class;
    ValueTask<AssetHandle<T>> LoadAsync<T>(AssetId assetId) where T : class;
    void Release<T>(AssetHandle<T> handle) where T : class;
    event Action<AssetId>? AssetReloaded;
}
