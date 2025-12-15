using FactoryEngine.Core.Engine;

namespace FactoryEngine.Core.Tests;

public class MultiComponentQueryTests
{
#pragma warning disable CS0649
    private struct Position { public float X; public float Y; }
    private struct Velocity { public float X; public float Y; }
#pragma warning restore CS0649

    [Fact]
    public void Query_TwoComponents_ReturnsOnlyEntitiesWithBoth()
    {
        var world = new WorldBuilder().Build();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        var e3 = world.CreateEntity();

        world.AddComponent<Position>(e1).X = 1;
        world.AddComponent<Velocity>(e1).X = 10;

        world.AddComponent<Position>(e2).X = 2;
        // e2 missing velocity
        world.AddComponent<Velocity>(e3).X = 30; // missing position

        var count = 0;
        foreach (var entry in world.Query<Position, Velocity>())
        {
            count++;
            Assert.True(entry.Entity == e1);
            entry.A.X += entry.B.X;
        }

        Assert.Equal(1, count);
        Assert.Equal(11, world.GetComponent<Position>(e1).X);
    }
}
