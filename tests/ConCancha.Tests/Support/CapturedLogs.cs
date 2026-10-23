using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace ConCancha.Tests.Support;

/// <summary>Keeps every log line the application writes, so tests can check what must never be logged.</summary>
public sealed class CapturedLogs : ILoggerProvider
{
    public ConcurrentQueue<string> Messages { get; } = new();

    public ILogger CreateLogger(string categoryName) => new Logger(this);

    public void Dispose()
    {
    }

    private sealed class Logger(CapturedLogs logs) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(LogLevel logLevel) => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
            logs.Messages.Enqueue(formatter(state, exception) + (exception is null ? string.Empty : " " + exception));
    }
}
