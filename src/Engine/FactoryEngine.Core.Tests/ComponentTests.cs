using FactoryEngine.Core.Engine;

namespace FactoryEngine.Core.Tests;

public class ComponentTests
{
    private struct Position
    {
        public float X;
        public float Y;
    }

    [Fact]
    public void AddComponent_ReturnsMutableReference()
    {
        var world = new WorldBuilder().Build();
        var entity = world.CreateEntity();
        ref var pos = ref world.AddComponent<Position>(entity);
        pos.X = 10;
        pos.Y = 20;
        ref var read = ref world.GetComponent<Position>(entity);
        Assert.Equal(10, read.X);
        Assert.Equal(20, read.Y);
    }

    [Fact]
    public void RemoveComponent_MakesHasReturnFalse()
    {
        var world = new WorldBuilder().Build();
        var entity = world.CreateEntity();
        world.AddComponent<Position>(entity);
        Assert.True(world.HasComponent<Position>(entity));
        world.RemoveComponent<Position>(entity);
        Assert.False(world.HasComponent<Position>(entity));
    }

    [Fact]
    public void DestroyEntity_RemovesComponentsDuringTick()
    {
        var world = new WorldBuilder().Build();
        var entity = world.CreateEntity();
        world.AddComponent<Position>(entity);
        world.DestroyEntity(entity);
        world.Tick(0f);
        Assert.False(world.HasComponent<Position>(entity));
    }
}
