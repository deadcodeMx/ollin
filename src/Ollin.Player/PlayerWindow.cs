using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using Ollin.Core.Input;
using Ollin.Core.Projects;
using Ollin.Editor.Controls;
using Ollin.Editor.ViewModels;

namespace Ollin.Player;

/// <summary>Ventana del juego: pantalla de inicio con el logo y el viewport.</summary>
public sealed class PlayerWindow : Window
{
    private readonly GameManifest _manifest;
    private readonly MainViewModel _viewModel;
    private readonly ViewportSurface _surface;
    private readonly Grid _splash;
    private bool _started;

    public PlayerWindow(LoadedGame game)
    {
        var manifest = game.Manifest;
        _manifest = manifest;
        Title = manifest.ProjectName;
        Width = 1280;
        Height = 720;
        MinWidth = 640;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Black;

        _viewModel = new MainViewModel();
        Ollin.Core.Audio.Audio.Root = game.Root;
        _viewModel.OpenProjectLocation(game.Root, logToConsole: false, remember: false);

        if (game.ScriptsImage is { Length: > 0 })
        {
            _viewModel.LoadPrecompiledScripts(game.ScriptsImage);
        }

        _surface = new ViewportSurface
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _surface.Viewport.ViewModel = _viewModel;

        var content = new Grid();
        content.Children.Add(_surface);

        if (manifest.Watermark)
        {
            content.Children.Add(BuildWatermark());
        }

        _splash = BuildSplash();
        content.Children.Add(_splash);
        Content = content;

        Opened += (_, _) => _surface.Focus();
        KeyDown += OnPlayerKeyDown;
        KeyUp += OnPlayerKeyUp;

        if (manifest.Splash)
        {
            DispatcherTimer.RunOnce(StartGame, TimeSpan.FromSeconds(2.4));
        }
        else
        {
            DispatcherTimer.RunOnce(StartGame, TimeSpan.FromMilliseconds(120));
        }
    }

    private Grid BuildSplash()
    {
        var grid = new Grid { Background = new SolidColorBrush(Color.Parse("#0B0D12")) };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
        };

        try
        {
            stack.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://Ollin.Editor/Assets/ollin.png"))),
                Width = 180,
                Height = 180,
            });
        }
        catch
        {
            // Sin logo, queda el texto.
        }

        stack.Children.Add(new TextBlock
        {
            Text = "Ollin",
            FontSize = 42,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        stack.Children.Add(new TextBlock
        {
            Text = _manifest.ProjectName,
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Hecho con Ollin",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 0),
        });

        grid.Children.Add(stack);
        return grid;
    }

    private Control BuildWatermark() => new TextBlock
    {
        Text = "Hecho con Ollin",
        FontSize = 11,
        Foreground = Brushes.White,
        Opacity = 0.4,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(0, 0, 14, 10),
        IsHitTestVisible = false,
    };

    private void StartGame()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _splash.IsVisible = false;
        _viewModel.StartPlayback();
        _surface.Focus();
    }

    private void OnPlayerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            if (WindowState == WindowState.FullScreen)
            {
                WindowState = WindowState.Normal;
            }
            else
            {
                Close();
            }

            e.Handled = true;
            return;
        }

        if (_started && KeyMapping.ToKeyCode(e.Key) is { } code)
        {
            InputState.SetKey(code, true);
            e.Handled = true;
        }
    }

    private void OnPlayerKeyUp(object? sender, KeyEventArgs e)
    {
        if (_started && KeyMapping.ToKeyCode(e.Key) is { } code)
        {
            InputState.SetKey(code, false);
            e.Handled = true;
        }
    }
}

/// <summary>Ventana de error si el juego no está bien empaquetado.</summary>
public sealed class ErrorWindow : Window
{
    public ErrorWindow(string message)
    {
        Title = "Ollin — Error";
        Width = 520;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#151922"));

        Content = new TextBlock
        {
            Text = message,
            Margin = new Thickness(24),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
