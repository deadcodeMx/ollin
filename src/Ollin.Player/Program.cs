using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Media;
using Avalonia.Themes.Fluent;
using Ollin.Core.Audio;
using Ollin.Core.Diagnostics;
using Ollin.Editor.Audio;

namespace Ollin.Player;

/// <summary>Reproductor independiente de un juego exportado con Ollin.</summary>
public sealed class PlayerApp : Application
{
    public override void Initialize()
    {
        Styles.Add(new FluentTheme());
        var logPath = Path.Combine(AppContext.BaseDirectory, "player.log");

        try
        {
            File.AppendAllText(logPath, $"--- Ollin Player {DateTime.Now:HH:mm:ss} ---{Environment.NewLine}");
        }
        catch
        {
            logPath = Path.Combine(Path.GetTempPath(), "OllinPlayer.log");
        }
        Log.Written += entry =>
        {
            try
            {
                File.AppendAllText(logPath, $"{entry.Timestamp:HH:mm:ss} {entry.Level} [{entry.Source}] {entry.Message}{Environment.NewLine}");
            }
            catch
            {
                // El registro nunca debe tumbar el juego.
            }
        };

        Audio.Initialize(new NAudioBackend());
        Audio.VerboseLogging = true;
        Log.Info($"Backend de audio: {(Audio.Enabled ? "activo" : "inactivo")}", "Player");
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Exit += (_, _) => Audio.Shutdown();

            var game = GameLoader.Load();

            desktop.MainWindow = game is null
                ? new ErrorWindow("No se encontró ningún juego.\n\nColoca el ejecutable junto a game.pak o usa --project <carpeta>.")
                : new PlayerWindow(game);
        }

        base.OnFrameworkInitializationCompleted();
    }
}

internal static class Program
{
    [STAThread]
    private static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder.Configure<PlayerApp>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
