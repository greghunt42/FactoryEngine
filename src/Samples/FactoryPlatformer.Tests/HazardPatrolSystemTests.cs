using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Systems;
using FactoryPlatformer.Components;
using FactoryPlatformer.Systems;

namespace FactoryPlatformer.Tests;

public class HazardPatrolSystemTests
{
    [Fact]
    public void HazardPatrolSystem_MovesHazardBetweenBounds()
    {
        var world = new WorldBuilder().Build();
        world.RegisterSystem(new HazardPatrolSystem(), SystemPhase.Simulation);

        var entity = world.CreateEntity();
        ref var transform = ref world.AddComponent<Transform2D>(entity);
        transform = new Transform2D { X = 0, Y = 0 };
        ref var patrol = ref world.AddComponent<HazardPatrol>(entity);
        patrol = new HazardPatrol
        {
            Axis = PatrolAxis.Horizontal,
            Range = 10f,
            Speed = 20f,
            Direction = 1f,
            OriginX = 0f,
            OriginY = 0f
        };

        world.Tick(0.5f); // move to positive bound and flip
        Assert.Equal(10f, transform.X, 2);

        world.Tick(0.5f); // move back towards origin after flip
        Assert.True(transform.X <= 0.01f, $"Expected to head back toward origin, got {transform.X}");
    }
}
