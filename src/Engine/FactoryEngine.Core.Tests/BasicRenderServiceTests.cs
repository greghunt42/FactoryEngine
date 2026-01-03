using System.IO;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Rendering;

namespace FactoryEngine.Core.Tests;

public class BasicRenderServiceTests
{
    [Fact]
    public void Submit_LoadsTexturesAndInvokesBackend()
    {
        var (assets, cleanup) = CreateAssetServiceWithTexture(out var assetId);
        try
        {
            var backend = new TestRenderBackend();
            var render = new BasicRenderService(assets, backend);
            render.BeginFrame();
            var buffer = render.GetFrameBuffer();
            buffer.DrawSprite(new SpriteDrawCommand(assetId, 1f, 2f, 0f, 1f, 1f, 0.5f));

            render.Submit(buffer);

            Assert.Single(render.LastFrame);
            Assert.Single(backend.DrawnSprites);
            Assert.Equal(assetId, backend.DrawnSprites[0].Command.Texture);
            Assert.Contains("textures/player.tex", backend.DrawnSprites[0].Texture.Path);
        }
        finally
        {
            cleanup();
        }
    }

    private static (IAssetService Service, Action Cleanup) CreateAssetServiceWithTexture(out AssetId assetId)
    {
        var tempDir = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());
        Directory.CreateDirectory(tempDir);
        var texturesDir = Path.Combine(tempDir, "textures");
        Directory.CreateDirectory(texturesDir);
        var texturePath = Path.Combine(texturesDir, "player.tex");
        File.WriteAllText(texturePath, "texture-bytes");
        var manifestPath = Path.Combine(tempDir, "core.catalog.json");
        File.WriteAllText(manifestPath, """
        {
          "namespace": "core",
          "root": ".",
          "assets": {
            "player": { "type": "Texture", "path": "textures/player.tex", "metadata": { "format": "placeholder" } }
          }
        }
        """);

        var assets = AssetPipeline.CreateDefaultService();
        var catalog = AssetCatalogManifest.LoadFromJson(manifestPath);
        assets.RegisterCatalog(catalog);
        assetId = new AssetId("core", "player");
        return (assets, () => Directory.Delete(tempDir, true));
    }

    private sealed class TestRenderBackend : IRenderBackend
    {
        public List<RenderedSprite> DrawnSprites { get; } = new();
        public List<string> Errors { get; } = new();

        public void BeginFrame()
        {
            DrawnSprites.Clear();
            Errors.Clear();
        }

        public void DrawSprite(RenderedSprite sprite)
        {
            DrawnSprites.Add(sprite);
        }

        public void OnError(string message)
        {
            Errors.Add(message);
        }

        public void EndFrame()
        {
        }
    }
}
