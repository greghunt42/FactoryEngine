using System;
using System.IO;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Asset;
using FactoryEngine.Core.Services.Rendering;
using FactoryEngine.Core.Services.Serialization;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

namespace FactoryPlatformer.Tests;

public class RenderingSystemTests
{
    [Fact]
    public void RenderingSystem_EnqueuesSpriteCommands()
    {
        var assets = AssetPipeline.CreateDefaultService();
        assets.RegisterLoader(new StubTextureLoader());
        var catalog = new AssetCatalog("core", ".");
        catalog.Assets["player"] = new AssetRecord { Type = AssetTypes.Texture, Path = "player.tex" };
        assets.RegisterCatalog(catalog);

        var serialization = new SerializationService();
        serialization.RegisterDescriptor(new Transform2DDescriptor());
        serialization.RegisterDescriptor(new SpriteDescriptor());
        var renderService = new BasicRenderService(assets, new ConsoleRenderBackend());
        var world = new WorldBuilder()
            .WithSerialization(serialization)
            .WithAssets(assets)
            .WithRendering(renderService)
            .Build();

        var entity = world.CreateEntity();
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 1, Y = 2 };
        ref var sprite = ref world.AddComponent<Sprite>(entity);
        sprite = new Sprite { TextureNamespace = "core", TextureName = "player", Layer = 0.3f };

        world.RegisterSystem(new RenderingSystem(), SystemPhase.RenderPrep);
        world.Rendering.BeginFrame();
        world.Tick(0.016f);

        var sprites = world.Rendering.GetFrameBuffer().Sprites;
        Assert.Single(sprites);
        Assert.Equal("core:player", sprites[0].Texture.ToString());
    }

    private sealed class StubTextureLoader : IAssetLoader<TextureAsset>
    {
        public TextureAsset Load(AssetRecord record, string rootPath)
        {
            return new TextureAsset(Path.Combine(rootPath, record.Path), Array.Empty<byte>());
        }
    }
}
