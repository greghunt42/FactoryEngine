namespace FactoryEngine.Core.Diagnostics;

public enum MetricType
{
    Counter,
    Gauge,
    Histogram
}

public readonly record struct CaptureOptions(string Description);

public sealed record CaptureHandle(Guid Id);
