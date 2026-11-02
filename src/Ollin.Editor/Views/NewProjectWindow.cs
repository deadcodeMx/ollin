using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ollin.Core.Projects;

namespace Ollin.Editor.Views;

/// <summary>Resultado del diálogo de proyecto nuevo.</summary>
public sealed record NewProjectResult(
    string Name,
    string ParentFolder,
    ProjectTemplate Template,
    ProjectContentMode ContentMode,
    ProjectGenre Genre);

/// <summary>Diálogo para crear un proyecto con su propia carpeta.</summary>
public sealed class NewProjectWindow : Window
{
    private readonly TextBox _name;
    private readonly TextBox _folder;
    private readonly ComboBox _mode;
    private readonly ComboBox _genre;
    private readonly TextBlock _error;

    public NewProjectWindow()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var suggested = Path.Combine(documents, "Ollin Projects");

        Title = "Nuevo proyecto";
        Width = 600;
        Height = 540;
        MinHeight = 420;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _name = new TextBox { Text = "MiJuego", Watermark = "Nombre del proyecto" };
        _folder = new TextBox { Text = suggested };
        _mode = new ComboBox
        {
            ItemsSource = new[]
            {
                "Vacío — solo componentes esenciales",
                "Por género — con componentes recomendados",
                "Completo — todos los componentes",
            },
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        _genre = new ComboBox
        {
            ItemsSource = new[] { "Plataformas", "Top-down / RPG", "Terror", "Shooter", "Puzzle" },
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            IsEnabled = false,
        };
        _mode.SelectionChanged += (_, _) => _genre.IsEnabled = _mode.SelectedIndex == 1;

        _error = new TextBlock
        {
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            IsVisible = false,
        };

        var browse = new Button { Content = "Examinar…", Margin = new Thickness(8, 0, 0, 0) };
        browse.Click += async (_, _) =>
        {
            var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
            {
                Title = "Carpeta donde crear el proyecto",
                AllowMultiple = false,
            });

            var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            if (!string.IsNullOrWhiteSpace(path)) _folder.Text = path;
        };

        var create = new Button
        {
            Content = "Crear proyecto",
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        create.Click += (_, _) => Accept();

        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(null);

        Content = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Nuevo proyecto",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock
                {
                    Text = "Elige cuánta ayuda quieres: componentes mínimos para programar o un conjunto guiado por género.",
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new TextBlock { Text = "Modo", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _mode,
                new TextBlock { Text = "Género (solo modo por género)", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _genre,
                new TextBlock { Text = "Nombre", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _name,
                new TextBlock { Text = "Carpeta", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children = { _folder, browse },
                },
                _error,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Margin = new Thickness(0, 10, 0, 0),
                    Children = { cancel, create },
                },
            },
        };

        var panel = Content as Control;
        Content = null;
        Content = new ScrollViewer
        {
            Content = panel,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };
    }

    private void Accept()
    {
        var name = _name.Text?.Trim();
        var folder = _folder.Text?.Trim();

        if (!ProjectService.CanCreate(folder, name, out var error))
        {
            ShowError(error ?? "No se puede crear el proyecto.");
            return;
        }

        try { Directory.CreateDirectory(folder!); }
        catch (Exception exception)
        {
            ShowError($"No se puede usar esa carpeta: {exception.Message}");
            return;
        }

        var mode = _mode.SelectedIndex switch
        {
            1 => ProjectContentMode.Genre,
            2 => ProjectContentMode.Full,
            _ => ProjectContentMode.Empty,
        };
        var genre = mode == ProjectContentMode.Genre
            ? _genre.SelectedIndex switch
            {
                1 => ProjectGenre.TopDown,
                2 => ProjectGenre.Horror,
                3 => ProjectGenre.Shooter,
                4 => ProjectGenre.Puzzle,
                _ => ProjectGenre.Platformer,
            }
            : ProjectGenre.None;

        var template = genre switch
        {
            ProjectGenre.Platformer => ProjectTemplate.Platformer,
            ProjectGenre.Horror => ProjectTemplate.Horror,
            _ => ProjectTemplate.Empty,
        };

        Close(new NewProjectResult(name!, folder!, template, mode, genre));
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.IsVisible = true;
    }
}
