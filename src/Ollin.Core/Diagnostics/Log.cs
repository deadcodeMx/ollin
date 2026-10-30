namespace Ollin.Core.Diagnostics;

/// <summary>Nivel de severidad de un mensaje del motor.</summary>
public enum LogLevel
{
    Info,
    Warning,
    Error,
}

/// <summary>Una entrada del registro del motor.</summary>
public sealed record LogEntry(DateTime Timestamp, LogLevel Level, string Message, string? Source = null)
{
    public string TimeText => Timestamp.ToString("HH:mm:ss");
}

/// <summary>
/// Registro central del motor. Los suscriptores (consola del editor, archivo, tests)
/// reciben los mensajes en el hilo que los emite.
/// </summary>
public static class Log
{
    private static readonly object Gate = new();

    /// <summary>Se dispara por cada mensaje escrito.</summary>
    public static event Action<LogEntry>? Written;

    /// <summary>Historial en memoria de los últimos mensajes.</summary>
    public static IReadOnlyList<LogEntry> History
    {
        get
        {
            lock (Gate)
            {
                return _history.ToArray();
            }
        }
    }

    private static readonly List<LogEntry> _history = new();
    private const int MaxHistory = 500;

    public static void Info(string message, string? source = null) => Write(LogLevel.Info, message, source);

    public static void Warn(string message, string? source = null) => Write(LogLevel.Warning, message, source);

    public static void Error(string message, string? source = null) => Write(LogLevel.Error, message, source);

    public static void Error(Exception exception, string? source = null) =>
        Write(LogLevel.Error, $"{exception.GetType().Name}: {exception.Message}", source);

    public static void Clear()
    {
        lock (Gate)
        {
            _history.Clear();
        }
    }

    private static void Write(LogLevel level, string message, string? source)
    {
        var entry = new LogEntry(DateTime.Now, level, message, source);
        lock (Gate)
        {
            _history.Add(entry);
            if (_history.Count > MaxHistory)
            {
                _history.RemoveRange(0, _history.Count - MaxHistory);
            }
        }

        Written?.Invoke(entry);
    }
}
