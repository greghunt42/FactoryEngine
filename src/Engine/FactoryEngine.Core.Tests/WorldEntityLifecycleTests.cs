using FactoryEngine.Core.Engine;

namespace FactoryEngine.Core.Tests;

public class WorldEntityLifecycleTests
{
    [Fact]
    public void CreateDestroyEntity_FollowsDeferredLifecycle()
    {
        var world = new WorldBuilder().WithName("Test").Build();
        var entity = world.CreateEntity();
        Assert.True(world.IsAlive(entity));
        world.DestroyEntity(entity);
        Assert.True(world.IsAlive(entity)); // destruction deferred until Tick
        world.Tick(0.016f);
        Assert.False(world.IsAlive(entity));
    }
}
