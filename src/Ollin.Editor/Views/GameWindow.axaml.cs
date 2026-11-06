using Avalonia.Controls;
using Avalonia.Input;
using Ollin.Editor.ViewModels;

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
    }

    public GameWindow(MainViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        GameHost.Viewport.ViewModel = viewModel;
        GameHost.Viewport.CameraPreview = true;
        GameHost.Viewport.FreezeWhenPlaying = false;
    }

    protected override void OnKeyDown(KeyEventArgs e)
    {
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
}
