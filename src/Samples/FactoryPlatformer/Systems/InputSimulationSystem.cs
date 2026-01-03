using FactoryEngine.Core.Services.Input;
using FactoryEngine.Core.Systems;

namespace FactoryPlatformer.Systems;

public sealed class InputSimulationSystem : SystemBase
{
    private readonly string _action;
    private readonly float _interval;
    private float _timer;
    private bool _state;

    public InputSimulationSystem(string action, float intervalSeconds = 1f)
    {
        _action = action;
        _interval = intervalSeconds;
    }

    protected override void OnRun(SystemContext context)
    {
        _timer += context.DeltaTime;
        if (_timer >= _interval)
        {
            _timer = 0f;
            _state = !_state;
            context.Services.Input.SetActionState(_action, new ActionState(_state ? 1f : 0f, _state));
        }
    }
}
