namespace FactoryEngine.Core.Services.Asset;

public interface IAssetLoader<T> where T : class
{
    T Load(AssetRecord record, string rootPath);
}
