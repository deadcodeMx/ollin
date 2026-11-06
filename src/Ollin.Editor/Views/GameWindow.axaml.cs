using Avalonia.Controls;
using Avalonia.Input;
using Ollin.Editor.ViewModels;
using Ollin.Editor.Controls;
using Ollin.Core.Input;

namespace Ollin.Editor.Views;

/// <summary>
/// Ventana independiente para ver/ejecutar el juego. Se puede mover a otro
/// monitor y poner a pantalla completa con F11.
/// </summary>
public sealed partial class GameWindow : Window
{
    public GameWindow()
    {
        InitializeComponent();
        Loaded += (_, _) => GameHost.Viewport.GameView = true;
    }

    public GameWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        GameHost.Viewport.ViewModel = viewModel;
        GameHost.Viewport.CameraPreview = true;
        GameHost.Viewport.GameView = true;
        GameHost.Viewport.GameInputEnabled = true;
        GameHost.Viewport.FreezeWhenPlaying = false;
        Opened += (_, _) => FocusGame();
        Activated += (_, _) => FocusGame();
    }

    public void FocusGame()
    {
        Activate();
        GameHost.Focus();
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (DataContext is MainViewModel { IsPlaying: true } && KeyMapping.ToKeyCode(e.Key) is { } playKey)
        {
            InputState.SetKey(playKey, true);
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F11)
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && WindowState == WindowState.FullScreen)
        {
            WindowState = WindowState.Normal;
            e.Handled = true;
            return;
        }

        base.OnKeyDown(e);
    }

    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (DataContext is MainViewModel { IsPlaying: true } && KeyMapping.ToKeyCode(e.Key) is { } playKey)
        {
            InputState.SetKey(playKey, false);
            e.Handled = true;
        }

        base.OnKeyUp(e);
    }
}
