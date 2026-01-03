using System;
using System.Linq;

namespace FactoryPlatformer;

public enum LevelLoopState
{
    Playing,
    Won,
    Lost
}

public readonly record struct ResetRequest(LevelLoopState Outcome, float DelaySeconds);

public readonly record struct GameEvent(string Message, DateTime Timestamp);

public sealed class FactoryPlatformerGameState
{
    private readonly object _sync = new();
    private ResetRequest? _pendingReset;
    private readonly List<GameEvent> _history = new();
    private const int HistoryLimit = 8;

    public int Score { get; private set; }
    public int HighScore { get; private set; }
    public string LastEvent { get; private set; } = "Welcome!";
    public DateTime LastEventTime { get; private set; } = DateTime.UtcNow;
    public LevelLoopState LoopState { get; private set; } = LevelLoopState.Playing;
    public event EventHandler<int>? HighScoreChanged;

    public void InitializeHighScore(int value)
    {
        lock (_sync)
        {
            HighScore = Math.Max(0, value);
        }
    }

    public void AddScore(int amount, string? message)
    {
        lock (_sync)
        {
            Score += Math.Max(0, amount);
            if (!string.IsNullOrWhiteSpace(message))
            {
                SetEventInternal(message);
            }
            else
            {
                SetEventInternal($"+{amount} points");
            }

            if (Score > HighScore)
            {
                HighScore = Score;
                HighScoreChanged?.Invoke(this, HighScore);
            }
        }
    }

    public void SetEvent(string message)
    {
        lock (_sync)
        {
            SetEventInternal(message);
        }
    }

    public void MarkVictory(string? message, float resetDelaySeconds = 2f)
    {
        lock (_sync)
        {
            LoopState = LevelLoopState.Won;
            SetEventInternal(string.IsNullOrWhiteSpace(message) ? "Goal reached!" : message!);
            ScheduleReset(LevelLoopState.Won, resetDelaySeconds);
        }
    }

    public void MarkFailure(string? message, float resetDelaySeconds = 1.5f)
    {
        lock (_sync)
        {
            LoopState = LevelLoopState.Lost;
            SetEventInternal(string.IsNullOrWhiteSpace(message) ? "Try again!" : message!);
            ScheduleReset(LevelLoopState.Lost, resetDelaySeconds);
        }
    }

    public bool TryDequeueReset(out ResetRequest request)
    {
        lock (_sync)
        {
            if (_pendingReset is ResetRequest pending)
            {
                request = pending;
                _pendingReset = null;
                return true;
            }
        }

        request = default;
        return false;
    }

    public void RestartLoop(string message = "Ready!", bool resetScore = true)
    {
        lock (_sync)
        {
            LoopState = LevelLoopState.Playing;
            if (resetScore)
            {
                Score = 0;
            }
            if (!string.IsNullOrWhiteSpace(message))
            {
                SetEventInternal(message);
            }
        }
    }

    public IReadOnlyList<GameEvent> GetEventHistorySnapshot(int maxEntries = HistoryLimit)
    {
        lock (_sync)
        {
            if (_history.Count == 0)
            {
                return Array.Empty<GameEvent>();
            }

            var count = Math.Clamp(maxEntries, 1, HistoryLimit);
            var skip = Math.Max(0, _history.Count - count);
            return _history.Skip(skip).ToArray();
        }
    }

    private void ScheduleReset(LevelLoopState outcome, float resetDelaySeconds)
    {
        var clampedDelay = float.IsFinite(resetDelaySeconds) ? MathF.Max(0.25f, resetDelaySeconds) : 1f;
        _pendingReset = new ResetRequest(outcome, clampedDelay);
    }

    private void SetEventInternal(string message)
    {
        LastEvent = message;
        LastEventTime = DateTime.UtcNow;
        AppendHistory(new GameEvent(message, LastEventTime));
    }

    private void AppendHistory(GameEvent entry)
    {
        _history.Add(entry);
        if (_history.Count > HistoryLimit)
        {
            _history.RemoveRange(0, _history.Count - HistoryLimit);
        }
    }
}
