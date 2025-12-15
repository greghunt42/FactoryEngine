namespace FactoryEngine.Core.Diagnostics;

using System.Diagnostics;

internal readonly struct ValueStopwatch
{
    private static readonly double TimestampToTicks = (double)TimeSpan.TicksPerSecond / Stopwatch.Frequency;
    private readonly long _startTimestamp;

    private ValueStopwatch(long startTimestamp)
    {
        _startTimestamp = startTimestamp;
    }

    public static ValueStopwatch StartNew() => new(Stopwatch.GetTimestamp());

    public TimeSpan GetElapsedTime()
    {
        var end = Stopwatch.GetTimestamp();
        var ticks = (end - _startTimestamp) * TimestampToTicks;
        return TimeSpan.FromTicks((long)ticks);
    }
}
