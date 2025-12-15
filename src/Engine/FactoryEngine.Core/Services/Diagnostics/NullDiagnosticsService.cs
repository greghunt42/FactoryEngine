using FactoryEngine.Core.Diagnostics;

namespace FactoryEngine.Core.Services.Diagnostics;

public sealed class NullDiagnosticsService : IDiagnosticsService
{
    private readonly TextWriter _writer;

    public NullDiagnosticsService(TextWriter? writer = null)
    {
        _writer = writer ?? Console.Out;
    }

    public ILogger CreateLogger(string category) => new NdjsonLogger(category, _writer);

    public void RecordMetric(string name, double value, MetricType type, IReadOnlyDictionary<string, string>? labels = null)
    {
        // Metrics are no-op until runtime implementation fills them in.
    }

    public CaptureHandle StartCapture(CaptureOptions options)
    {
        // Capture not yet supported; return dummy handle.
        return new CaptureHandle(Guid.NewGuid());
    }
}
