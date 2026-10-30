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
}
