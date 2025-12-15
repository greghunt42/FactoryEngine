using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Ecs;

namespace FactoryEngine.Core.Tests;

public class QueryBuilderTests
{
#pragma warning disable CS0649
    private struct Position { public float X; }
    private struct Velocity { public float X; }
    private struct Health { public int Value; }
#pragma warning restore CS0649

    [Fact]
    public void Query_WithAllAnyNone_FiltersEntities()
    {
        var world = new WorldBuilder().Build();
        var e1 = world.CreateEntity();
        world.AddComponent<Position>(e1);
        world.AddComponent<Velocity>(e1);

        var e2 = world.CreateEntity();
        world.AddComponent<Position>(e2);

        var e3 = world.CreateEntity();
        world.AddComponent<Velocity>(e3);
        world.AddComponent<Health>(e3);

        var results = new List<Entity>();
        foreach (var entity in world.Query(q => q.All<Position>().Any<Velocity>().None<Health>()))
        {
            results.Add(entity);
        }

        Assert.Single(results);
        Assert.Equal(e1, results[0]);
    }
}
