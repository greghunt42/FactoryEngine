using System;

namespace FactoryPlatformer;

public sealed class LoopResetController
{
    private readonly FactoryPlatformerGameState _state;
    private readonly Action _resetCallback;
    private PendingReset? _pendingReset;

    public LoopResetController(FactoryPlatformerGameState state, Action resetCallback)
    {
        _state = state ?? throw new ArgumentNullException(nameof(state));
        _resetCallback = resetCallback ?? throw new ArgumentNullException(nameof(resetCallback));
    }

    public float? PendingResetSeconds => _pendingReset?.RemainingSeconds;

    public void Update(float deltaSeconds)
    {
        if (_pendingReset is null && _state.TryDequeueReset(out var request))
        {
            _pendingReset = new PendingReset(request.Outcome, request.DelaySeconds);
        }

        if (_pendingReset is not PendingReset pending)
        {
            return;
        }

        var remaining = MathF.Max(0f, pending.RemainingSeconds - MathF.Max(0f, deltaSeconds));
        if (remaining <= 0f)
        {
            _resetCallback();
            _state.RestartLoop();
            _pendingReset = null;
            return;
        }

        _pendingReset = pending with { RemainingSeconds = remaining };
    }

    private readonly record struct PendingReset(LevelLoopState Outcome, float RemainingSeconds);
}
