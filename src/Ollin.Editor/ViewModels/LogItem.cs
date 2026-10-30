using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Threading;
using Ollin.Core.Diagnostics;

namespace Ollin.Editor.ViewModels;

/// <summary>Entrada de la consola del editor.</summary>
public sealed class LogItem
{
    private static readonly IBrush InfoBrush = new SolidColorBrush(Color.Parse("#A9B4C8"));
    private static readonly IBrush WarningBrush = new SolidColorBrush(Color.Parse("#E9C46A"));
    private static readonly IBrush ErrorBrush = new SolidColorBrush(Color.Parse("#E76F6F"));

    public LogItem(LogEntry entry)
    {
        Time = entry.TimeText;
        Message = entry.Message;
        Source = entry.Source ?? string.Empty;
        Brush = entry.Level switch
        {
            LogLevel.Warning => WarningBrush,
            LogLevel.Error => ErrorBrush,
            _ => InfoBrush,
        };
        LevelText = entry.Level switch
        {
            LogLevel.Warning => "AVISO",
            LogLevel.Error => "ERROR",
            _ => "INFO",
        };
    }

    public string Time { get; }

    public string LevelText { get; }

    public string Message { get; }

    public string Source { get; }

    public IBrush Brush { get; }
}
