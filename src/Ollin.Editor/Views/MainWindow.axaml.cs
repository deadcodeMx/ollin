using System.Collections.Specialized;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ollin.Editor.ViewModels;

namespace Ollin.Editor.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType SceneFileType = new("Escena de Ollin")
    {
        Patterns = new[] { "*.ollin.json", "*.json" },
    };

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;


    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        ViewportHost.Viewport.ViewModel = vm;
        vm.Logs.CollectionChanged += OnLogsChanged;
        vm.TexturesReloadRequested += () => ViewportHost.Viewport.RequestTextureReload();
    }

    private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || LogList.ItemCount == 0)
        {
            return;
        }

        // Se difiere al final del layout: con virtualización, desplazar antes de medir no surte efecto.
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () =>
            {
                if (LogList.ItemCount > 0)
                {
                    LogList.ScrollIntoView(LogList.ItemCount - 1);
                }
            },
            Avalonia.Threading.DispatcherPriority.Background);
    }

    // ---------- Archivo ----------

    private void OnNewScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.NewScene();

    private async void OnOpenScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir escena",
            AllowMultiple = false,
            FileTypeFilter = new[] { SceneFileType },
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (!string.IsNullOrEmpty(path))
        {
            ViewModel.OpenScene(path);
        }
    }

    private async void OnSaveScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(ViewModel.ScenePath))
        {
            ViewModel.SaveScene(ViewModel.ScenePath);
            return;
        }

        await SaveAsAsync();
    }

    private async void OnSaveSceneAs(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await SaveAsAsync();

    private async Task SaveAsAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar escena",
            SuggestedFileName = $"{ViewModel.Scene.Name}.ollin.json",
            DefaultExtension = "json",
            FileTypeChoices = new[] { SceneFileType },
        });

        var path = file?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            ViewModel.SaveScene(path);
        }
    }

    private void OnExit(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    // ---------- Edición ----------

    private void OnAddSprite(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AddEntity(true);

    private void OnAddEmpty(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AddEntity(false);

    private void OnDuplicate(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DuplicateSelected();

    private void OnDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DeleteSelected();

    private void OnClearConsole(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Logs.Clear();

    private void OnUndo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Undo();

    private void OnRedo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Redo();

    // ---------- Assets ----------

    private async void OnImportAsset(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar textura",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" },
                },
            },
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                ViewModel.ImportTexture(path);
            }
        }
    }

    private void OnReloadAssets(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.RefreshAssets();
        ViewModel.RequestTextureReload();
    }

    private void OnAssignAsset(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewModel?.AssignAssetToSelected();

    private void OnAssetListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            ViewModel?.AssignAssetToSelected();
            e.Handled = true;
        }
    }

    // ---------- Scripts ----------

    private void OnReloadScripts(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ReloadScripts();

    private void OnCreateScript(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewModel?.CreateScriptForSelected();

    // ---------- Modo juego ----------

    private void OnPlay(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.TogglePlay();

    private void OnPause(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.TogglePause();

    private void OnStopPlay(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is { IsPlaying: true })
        {
            ViewModel.TogglePlay();
        }
    }

    // ---------- Ver ----------

    private void OnResetCamera(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ResetCamera();

    private void OnFrameSelected(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.FrameSelected();

    // ---------- Ayuda ----------

    private void OnAbout(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var about = new Window
        {
            Title = "Acerca de Ollin",
            Width = 460,
            Height = 280,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#151922")),
        };

        about.Content = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Ollin",
                    FontSize = 26,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock
                {
                    Text = "Motor de juego 2D · versión 0.1 (etapa 1)",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new TextBlock
                {
                    Text = "Editor construido con Avalonia y renderizado 2D con OpenGL (Silk.NET). " +
                           "La escritura de scripts en C# llegará en la etapa 3.",
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
                },
                new TextBlock
                {
                    Text = "«Ollin» significa movimiento en náhuatl.",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#47B8A8")),
                },
            },
        };

        about.ShowDialog(this);
    }

    // ---------- Atajos ----------

    protected override void OnKeyDown(KeyEventArgs e)
    {
        if (ViewModel is null || IsTextInputFocused())
        {
            base.OnKeyDown(e);
            return;
        }

        var control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        switch (e.Key)
        {
            case Key.Z when control:
                ViewModel.Undo();
                e.Handled = true;
                break;
            case Key.Y when control:
                ViewModel.Redo();
                e.Handled = true;
                break;
            case Key.N when control:
                ViewModel.NewScene();
                e.Handled = true;
                break;
            case Key.O when control:
                OnOpenScene(this, new Avalonia.Interactivity.RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.S when control:
                OnSaveScene(this, new Avalonia.Interactivity.RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.Delete:
                ViewModel.DeleteSelected();
                e.Handled = true;
                break;
            case Key.F:
                ViewModel.FrameSelected();
                e.Handled = true;
                break;
            case Key.Home:
                ViewModel.ResetCamera();
                e.Handled = true;
                break;
            case Key.F5:
                ViewModel.TogglePlay();
                e.Handled = true;
                break;
            case Key.F6:
                ViewModel.TogglePause();
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }

    private bool IsTextInputFocused() =>
        FocusManager?.GetFocusedElement() is TextBox or NumericUpDown ||
        FocusManager?.GetFocusedElement() is Control { Name: "PART_TextBox" };
}
