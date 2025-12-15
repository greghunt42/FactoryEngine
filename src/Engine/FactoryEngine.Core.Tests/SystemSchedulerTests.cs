using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Systems;

namespace FactoryEngine.Core.Tests;

public class SystemSchedulerTests
{
    private sealed class TestSystem : SystemBase
    {
        private readonly Action<SystemContext> _onRun;

        public TestSystem(Action<SystemContext> onRun, Action<ComponentAccessBuilder>? configure = null)
        {
            _onRun = onRun;
            if (configure is not null)
            {
                DeclareAccess(configure);
            }
        }

        protected override void OnRun(SystemContext context)
        {
            _onRun(context);
        }
    }

    [Fact]
    public void Systems_RunInPhaseOrderAndPriority()
    {
        var world = new WorldBuilder().WithDiagnostics(new TestDiagnostics()).Build();
        var executed = new List<string>();

        world.RegisterSystem(new TestSystem(ctx => executed.Add($"{ctx.Phase}-low")), SystemPhase.Simulation, priority: 0);
        world.RegisterSystem(new TestSystem(ctx => executed.Add($"{ctx.Phase}-high")), SystemPhase.Simulation, priority: 10);
        world.RegisterSystem(new TestSystem(ctx => executed.Add($"{ctx.Phase}")), SystemPhase.Input);

        world.Tick(0.016f);

        Assert.Equal(new[] { "Input", "Simulation-high", "Simulation-low" }, executed);
    }

    [Fact]
    public void Register_ThrowsOnWriteConflict()
    {
        var diagnostics = new TestDiagnostics();
        var world = new WorldBuilder().WithDiagnostics(diagnostics).Build();
        world.RegisterSystem(new TestSystem(_ => { }, builder => builder.Writes<Position>()), SystemPhase.Simulation);

        Assert.Throws<InvalidOperationException>(() =>
            world.RegisterSystem(new TestSystem(_ => { }, builder => builder.Writes<Position>()), SystemPhase.Simulation));
    }

    private struct Position;
}
