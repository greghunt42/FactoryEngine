using System.Text.Json;

namespace FactoryEngine.Core.Diagnostics;

public sealed class NdjsonLogger : ILogger
{
    private readonly string _category;
    private readonly TextWriter _writer;

    public NdjsonLogger(string category, TextWriter writer)
    {
        _category = category;
        _writer = writer;
    }

    public void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object>? properties = null)
    {
        var payload = new Dictionary<string, object?>
        {
            ["timestamp"] = DateTimeOffset.UtcNow,
            ["level"] = level.ToString(),
            ["category"] = _category,
            ["message"] = message
        };

        if (exception is not null)
        {
            payload["exception"] = exception.ToString();
        }

        if (properties is not null)
        {
            payload["properties"] = properties;
        }

        var json = JsonSerializer.Serialize(payload);
        _writer.WriteLine(json);
        _writer.Flush();
    }

    public void Trace(string message) => Log(LogLevel.Trace, message);
    public void Debug(string message) => Log(LogLevel.Debug, message);
    public void Info(string message) => Log(LogLevel.Information, message);
    public void Warn(string message) => Log(LogLevel.Warning, message);
    public void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    public void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);
}
