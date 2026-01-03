using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Tests;

public class WorldAssetTests
{
    private class DummyAsset { }
    private sealed class DummyLoader : IAssetLoader<DummyAsset>
    {
        public DummyAsset Load(AssetRecord record, string rootPath) => new();
    }

    [Fact]
    public void World_ExposesAssetService()
    {
        var assetService = new AssetService();
        assetService.RegisterLoader(new DummyLoader());
        var catalog = new AssetCatalog("core", ".");
        catalog.Assets["dummy"] = new AssetRecord { Type = nameof(DummyAsset), Path = "path" };
        assetService.RegisterCatalog(catalog);

        var world = new WorldBuilder().WithAssets(assetService).Build();
        var handle = world.Assets.Load<DummyAsset>(new AssetId("core", "dummy"));
        Assert.True(handle.IsValid);
    }
}
