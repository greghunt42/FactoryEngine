namespace FactoryEngine.Core.Diagnostics;

public interface ILogger
{
    void Log(LogLevel level, string message, Exception? exception = null, IReadOnlyDictionary<string, object>? properties = null);

    void Trace(string message) => Log(LogLevel.Trace, message);
    void Debug(string message) => Log(LogLevel.Debug, message);
    void Info(string message) => Log(LogLevel.Information, message);
    void Warn(string message) => Log(LogLevel.Warning, message);
    void Error(string message, Exception? exception = null) => Log(LogLevel.Error, message, exception);
    void Critical(string message, Exception? exception = null) => Log(LogLevel.Critical, message, exception);
}

public enum LogLevel
{
    Trace,
    Debug,
    Information,
    Warning,
    Error,
    Critical
}
