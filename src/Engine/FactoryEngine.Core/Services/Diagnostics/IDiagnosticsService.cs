using FactoryEngine.Core.Diagnostics;

namespace FactoryEngine.Core.Services.Diagnostics;

public interface IDiagnosticsService
{
    ILogger CreateLogger(string category);
    void RecordMetric(string name, double value, MetricType type, IReadOnlyDictionary<string, string>? labels = null);
    CaptureHandle StartCapture(CaptureOptions options);
}
