using FactoryEngine.Core.Engine;

namespace FactoryEngine.Core.Tests;

public class QueryTests
{
    private struct Velocity
    {
        public float X;
    }

    [Fact]
    public void Query_ReturnsAllEntitiesWithComponent()
    {
        var world = new WorldBuilder().Build();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        var e3 = world.CreateEntity();
        world.AddComponent<Velocity>(e1).X = 1;
        world.AddComponent<Velocity>(e2).X = 2;
        // e3 has no component

        var sum = 0f;
        foreach (var entry in world.Query<Velocity>())
        {
            sum += entry.Component.X;
            Assert.True(entry.Entity == e1 || entry.Entity == e2);
        }

        Assert.Equal(3f, sum);
    }

    [Fact]
    public void Query_ReflectsComponentRemoval()
    {
        var world = new WorldBuilder().Build();
        var e1 = world.CreateEntity();
        world.AddComponent<Velocity>(e1);
        var count = 0;
        foreach (var _ in world.Query<Velocity>())
        {
            count++;
        }
        Assert.Equal(1, count);
        world.RemoveComponent<Velocity>(e1);
        count = 0;
        foreach (var _ in world.Query<Velocity>())
        {
            count++;
        }
        Assert.Equal(0, count);
    }
}
