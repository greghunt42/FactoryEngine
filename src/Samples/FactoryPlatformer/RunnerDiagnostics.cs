using System.Collections.Concurrent;

namespace FactoryPlatformer;

public sealed class RunnerDiagnostics
{
    private const int MaxEntries = 8;
    private readonly ConcurrentQueue<DiagnosticEntry> _renderErrors = new();

    public void ReportRenderError(string message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return;
        }

        _renderErrors.Enqueue(new DiagnosticEntry(DateTime.UtcNow, message));
        TrimQueue(_renderErrors);
    }

    public IReadOnlyList<DiagnosticEntry> GetRenderErrors() => _renderErrors.ToArray();

    private static void TrimQueue(ConcurrentQueue<DiagnosticEntry> queue)
    {
        while (queue.Count > MaxEntries && queue.TryDequeue(out _))
        {
        }
    }
}

public readonly record struct DiagnosticEntry(DateTime Timestamp, string Message);
