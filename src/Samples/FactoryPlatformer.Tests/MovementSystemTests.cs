using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Serialization;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

namespace FactoryPlatformer.Tests;

public class MovementSystemTests
{
    [Fact]
    public void MovementSystem_UpdatesPosition()
    {
        var world = new WorldBuilder().Build();
        world.Serialization.RegisterDescriptor(new Transform2DDescriptor());
        world.Serialization.RegisterDescriptor(new Velocity2DDescriptor());

        var entity = world.CreateEntity();
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 0, Y = 0 };
        ref var velocity = ref world.AddComponent<Velocity2D>(entity);
        velocity = new Velocity2D { VX = 5, VY = 0 };

        world.RegisterSystem(new MovementSystem(), SystemPhase.Simulation);
        world.Tick(0.5f);

        var result = world.GetComponent<Transform2D>(entity);
        Assert.Equal(2.5f, result.X, 3);
        Assert.Equal(0f, result.Y, 3);
    }
}
