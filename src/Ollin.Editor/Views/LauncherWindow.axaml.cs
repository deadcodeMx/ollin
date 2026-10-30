using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Ollin.Core.Assets;
using Ollin.Core.Projects;
using Ollin.Editor.Styles;
using Ollin.Editor.ViewModels;

namespace Ollin.Editor.Views;

/// <summary>Elemento de la lista de proyectos recientes.</summary>
public sealed class RecentProjectItem
{
    public RecentProjectItem(string name, string path)
    {
        Name = name;
        Path = path;
    }

    public string Name { get; }

    public string Path { get; }
}

/// <summary>Ventana inicial: abrir un proyecto existente o crear uno nuevo.</summary>
public sealed partial class LauncherWindow : Window
{
    private readonly EditorSettings _settings;
    private readonly ObservableCollection<RecentProjectItem> _recent = new();

    public LauncherWindow()
        : this(EditorSettings.Load())
    {
    }

    public LauncherWindow(EditorSettings settings)
    {
        _settings = settings;
        InitializeComponent();

        try
        {
            Logo.Source = new Bitmap(AssetLoader.Open(new Uri("avares://Ollin.Editor/Assets/ollin.png")));
        }
        catch
        {
            // Sin logo la ventana sigue funcionando.
        }

        RecentList.ItemsSource = _recent;

        foreach (var path in _settings.RecentProjects)
        {
            if (File.Exists(path) || Directory.Exists(path))
            {
                _recent.Add(new RecentProjectItem(DisplayName(path), path));
            }
        }

        EmptyHint.IsVisible = _recent.Count == 0;
        RecentList.SelectedIndex = _recent.Count > 0 ? 0 : -1;
    }

    private static string DisplayName(string path)
    {
        try
        {
            var project = ProjectService.Find(path) ?? ProjectService.OpenFolder(Directory.Exists(path) ? path : Path.GetDirectoryName(path)!);
            return project.Name;
        }
        catch
        {
            return Path.GetFileName(path.TrimEnd(Path.DirectorySeparatorChar));
        }
    }

    private void OnOpenSelected(object? sender, RoutedEventArgs e)
    {
        if (RecentList.SelectedItem is RecentProjectItem item)
        {
            OpenEditor(item.Path);
        }
    }

    private async void OnOpenFolder(object? sender, RoutedEventArgs e)
    {
        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Abrir proyecto de Ollin",
            AllowMultiple = false,
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;

        if (!string.IsNullOrWhiteSpace(path))
        {
            OpenEditor(path);
        }
    }

    private async void OnCreateProject(object? sender, RoutedEventArgs e)
    {
        var dialog = new NewProjectWindow();
        var result = await dialog.ShowDialog<NewProjectResult?>(this);

        if (result is null)
        {
            return;
        }

        try
        {
            var templateAssets = Path.Combine(AppContext.BaseDirectory, "assets");

            if (!Directory.Exists(templateAssets))
            {
                var root = ProjectLocator.FindProjectRoot();
                templateAssets = root is null ? templateAssets : Path.Combine(root, "assets");
            }

            var project = ProjectService.Create(
                result.ParentFolder,
                result.Name,
                Directory.Exists(templateAssets) ? templateAssets : null,
                result.Template);

            OpenEditor(project.ProjectFile);
        }
        catch (Exception exception)
        {
            _ = exception;
            // Si falla, el editor mostrará el error al abrir el proyecto.
        }
    }

    private void OnExit(object? sender, RoutedEventArgs e) => Close();

    private void OpenEditor(string path)
    {
        _settings.RememberProject(path);
        _settings.Save();

        var window = new MainWindow { DataContext = new MainViewModel() };

        if (Application.Current?.ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = window;
            window.Show();
            Close();
        }
    }
}
