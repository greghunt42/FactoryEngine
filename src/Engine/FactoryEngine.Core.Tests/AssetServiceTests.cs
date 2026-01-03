using FactoryEngine.Core.Services.Asset;

namespace FactoryEngine.Core.Tests;

public class AssetServiceTests
{
    private class DummyAsset
    {
    }

    private sealed class DummyLoader : IAssetLoader<DummyAsset>
    {
        public int LoadCount { get; private set; }

        public DummyAsset Load(AssetRecord record, string rootPath)
        {
            LoadCount++;
            return new DummyAsset();
        }
    }

    [Fact]
    public void Load_ReturnsHandleWhenAssetExists()
    {
        var service = new AssetService();
        var loader = new DummyLoader();
        service.RegisterLoader(loader);
        var catalog = new AssetCatalog("core", ".");
        catalog.Assets["dummy"] = new AssetRecord { Type = nameof(DummyAsset), Path = "path" };
        service.RegisterCatalog(catalog);

        var handle = service.Load<DummyAsset>(new AssetId("core", "dummy"));
        Assert.True(handle.IsValid);
        Assert.IsType<DummyAsset>(handle.Value);
    }

    [Fact]
    public void Load_ReusesCachedAsset()
    {
        var service = new AssetService();
        var loader = new DummyLoader();
        service.RegisterLoader(loader);
        var catalog = new AssetCatalog("core", ".");
        catalog.Assets["dummy"] = new AssetRecord { Type = nameof(DummyAsset), Path = "path" };
        service.RegisterCatalog(catalog);

        var first = service.Load<DummyAsset>(new AssetId("core", "dummy"));
        var second = service.Load<DummyAsset>(new AssetId("core", "dummy"));

        Assert.Same(first.Value, second.Value);
        Assert.Equal(1, loader.LoadCount);
    }

    [Fact]
    public void Load_ThrowsWhenNamespaceMissing()
    {
        var service = new AssetService();
        Assert.Throws<InvalidOperationException>(() => service.Load<DummyAsset>(new AssetId("missing", "dummy")));
    }

    [Fact]
    public void Load_ThrowsWhenLoaderMissing()
    {
        var service = new AssetService();
        var catalog = new AssetCatalog("core", ".");
        catalog.Assets["dummy"] = new AssetRecord { Type = nameof(DummyAsset), Path = "path" };
        service.RegisterCatalog(catalog);

        Assert.Throws<InvalidOperationException>(() => service.Load<DummyAsset>(new AssetId("core", "dummy")));
    }
}
