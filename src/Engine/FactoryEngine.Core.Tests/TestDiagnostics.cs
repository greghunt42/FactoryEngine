using FactoryEngine.Core.Diagnostics;
using FactoryEngine.Core.Services.Diagnostics;

namespace FactoryEngine.Core.Tests;

internal sealed class TestDiagnostics : IDiagnosticsService
{
    public ILogger CreateLogger(string category) => new NdjsonLogger(category, TextWriter.Null);

    public void RecordMetric(string name, double value, MetricType type, IReadOnlyDictionary<string, string>? labels = null)
    {
    }

    public CaptureHandle StartCapture(CaptureOptions options) => new(Guid.Empty);
}
