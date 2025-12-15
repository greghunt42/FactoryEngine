using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Systems;

namespace FactoryEngine.Core.Tests;

public class SystemBaseTests
{
    private struct Position { public float X; }

    private sealed class SampleSystem : SystemBase
    {
        public List<float> Positions { get; } = new();

        public SampleSystem()
        {
            DeclareAccess(builder => builder.Reads<Position>());
        }

        protected override void OnRun(SystemContext context)
        {
            ForEach<Position>((entity, pos) => Positions.Add(pos.X));
        }
    }

    [Fact]
    public void ForEach_IteratesComponentQuery()
    {
        var world = new WorldBuilder().Build();
        world.AddComponent<Position>(world.CreateEntity()).X = 5;
        world.AddComponent<Position>(world.CreateEntity()).X = 10;

        var system = new SampleSystem();
        world.RegisterSystem(system, SystemPhase.Simulation);
        world.Tick(0.016f);

        Assert.Equal(new[] { 5f, 10f }, system.Positions);
    }
}
