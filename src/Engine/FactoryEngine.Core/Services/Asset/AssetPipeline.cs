using System;

namespace FactoryEngine.Core.Services.Asset;

public static class AssetPipeline
{
    public static AssetService CreateDefaultService()
    {
        var service = new AssetService();
        RegisterDefaultLoaders(service);
        return service;
    }

    public static void RegisterDefaultLoaders(IAssetService service)
    {
        ArgumentNullException.ThrowIfNull(service);
        service.RegisterLoader(new FileAssetLoader());
        service.RegisterLoader(new PrefabJsonAssetLoader());
        service.RegisterLoader(new TextureFileLoader());
        service.RegisterLoader(new AudioFileLoader());
        service.RegisterLoader(new SoundBankJsonLoader());
    }
}
