using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Engine;
using FactoryEngine.Core.Services.Diagnostics;

namespace FactoryEngine.Core.Systems;

public sealed class SystemScheduler
{
    private readonly Dictionary<SystemPhase, List<SystemRegistration>> _systems = new();
    private readonly List<SystemPhase> _phaseOrder = Enum.GetValues<SystemPhase>().ToList();
    private readonly IDiagnosticsService _diagnostics;

    public SystemScheduler(IDiagnosticsService diagnostics)
    {
        _diagnostics = diagnostics;
    }

    public void Register(ISystem system, SystemPhase phase, int priority = 0)
    {
        ArgumentNullException.ThrowIfNull(system);
        if (!_systems.TryGetValue(phase, out var list))
        {
            list = new List<SystemRegistration>();
            _systems[phase] = list;
        }

        var registration = new SystemRegistration(system, priority, list.Count);
        foreach (var existing in list)
        {
            if (AccessAnalyzer.HasConflict(existing.System.Access, system.Access))
            {
                throw new InvalidOperationException($"System conflict between {existing.System.GetType().Name} and {system.GetType().Name} in phase {phase}");
            }
        }

        list.Add(registration);
        list.Sort(static (a, b) =>
        {
            var priorityCompare = b.Priority.CompareTo(a.Priority);
            return priorityCompare != 0 ? priorityCompare : a.InsertionOrder.CompareTo(b.InsertionOrder);
        });
    }

    public void Run(World world, float deltaTime)
    {
        foreach (var phase in _phaseOrder)
        {
            if (!_systems.TryGetValue(phase, out var list))
            {
                continue;
            }

            var context = new SystemContext(phase, deltaTime, world.Services);
            foreach (var registration in list)
            {
                var stopwatch = ValueStopwatch.StartNew();
                registration.System.Run(world, context);
                var elapsed = stopwatch.GetElapsedTime();
                _diagnostics.RecordMetric("system.duration", elapsed.TotalMilliseconds, MetricType.Histogram,
                    new Dictionary<string, string>
                    {
                        ["system"] = registration.System.GetType().Name,
                        ["phase"] = phase.ToString()
                    });
            }
        }
    }

    private sealed record SystemRegistration(ISystem System, int Priority, int InsertionOrder);
}
