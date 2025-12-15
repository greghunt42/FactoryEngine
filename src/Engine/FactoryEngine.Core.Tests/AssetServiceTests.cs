using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Tests;

public class AssetServiceTests
{
    private class DummyAsset
    {
    }

    [Fact]
    public void Load_ReturnsHandleWhenAssetExists()
    {
        var service = new AssetService();
        var catalog = new AssetCatalog("core");
        catalog.Assets["dummy"] = new AssetRecord { Type = nameof(DummyAsset), Path = "path" };
        service.RegisterCatalog(catalog);

        var handle = service.Load<DummyAsset>(new AssetId("core", "dummy"));
        Assert.True(handle.IsValid);
        Assert.NotNull(handle.Value);
    }

    [Fact]
    public void Load_ThrowsWhenNamespaceMissing()
    {
        var service = new AssetService();
        Assert.Throws<InvalidOperationException>(() => service.Load<DummyAsset>(new AssetId("missing", "dummy")));
    }
}
