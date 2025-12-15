using FactoryEngine.Core.Engine;
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
        var world = new WorldBuilder().Build();
        world.Serialization.RegisterDescriptor(new Transform2DDescriptor());
        world.Serialization.RegisterDescriptor(new SpriteDescriptor());

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
}
