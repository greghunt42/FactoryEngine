using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Tests;

public class WorldAssetTests
{
    private class DummyAsset { }

    [Fact]
    public void World_ExposesAssetService()
    {
        var assetService = new AssetService();
        var catalog = new AssetCatalog("core");
        catalog.Assets["dummy"] = new AssetRecord { Type = nameof(DummyAsset), Path = "path" };
        assetService.RegisterCatalog(catalog);

        var world = new WorldBuilder().WithAssets(assetService).Build();
        var handle = world.Assets.Load<DummyAsset>(new AssetId("core", "dummy"));
        Assert.True(handle.IsValid);
    }
}
