using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Ollin.Editor.ViewModels;
using Ollin.Editor.Styles;
using Ollin.Editor.Views;

namespace Ollin.Editor;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var settings = EditorSettings.Load();
            EditorTheme.Apply(EditorTheme.Parse(settings.Theme));

            var arguments = Environment.GetCommandLineArgs();
            var exportDestination = ArgumentValue(arguments, "--export");

            if (!string.IsNullOrWhiteSpace(exportDestination))
            {
                var projectPath = ArgumentValue(arguments, "--project") ?? settings.LastProject;
                var viewModel = new MainViewModel();

                if (!string.IsNullOrWhiteSpace(projectPath))
                {
                    viewModel.OpenProjectLocation(projectPath, logToConsole: false);
                }

                var splash = !arguments.Contains("--no-splash", StringComparer.OrdinalIgnoreCase);
                var watermark = !arguments.Contains("--no-watermark", StringComparer.OrdinalIgnoreCase);
                var exported = viewModel.ExportGame(exportDestination, splash, watermark);

                desktop.ShutdownMode = Avalonia.Controls.ShutdownMode.OnExplicitShutdown;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => desktop.Shutdown(exported is null ? 1 : 0));
                return;
            }
            var openDirectly = arguments.Any(argument =>
                argument.Equals("--project", StringComparison.OrdinalIgnoreCase) ||
                argument.Equals("--script", StringComparison.OrdinalIgnoreCase) ||
                argument.Equals("--play", StringComparison.OrdinalIgnoreCase) ||
                argument.Equals("--no-launcher", StringComparison.OrdinalIgnoreCase));

            desktop.MainWindow = openDirectly
                ? new MainWindow { DataContext = new MainViewModel() }
                : new LauncherWindow(settings);
        }

        base.OnFrameworkInitializationCompleted();
    }
    private static string? ArgumentValue(string[] arguments, string name)
    {
        for (var i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i].Equals(name, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[i + 1];
            }
        }

        return null;
    }
}