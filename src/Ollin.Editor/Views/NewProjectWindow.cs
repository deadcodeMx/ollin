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
        Background = new SolidColorBrush(Color.Parse("#121722"));
        Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));

        _name = new TextBox
        {
            Text = "Mi Juego",
            Watermark = null,
            MinHeight = 32,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        _folder = new TextBox
        {
            Text = suggested,
            Watermark = null,
            FontSize = 11.5,
            MinHeight = 32,
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        ToolTip.SetTip(_folder, suggested);
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
        var genrePanel = new StackPanel
        {
            Spacing = 6,
            IsVisible = false,
            Children =
            {
                new TextBlock { Text = "Género", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                _genre,
            },
        };
        _mode.SelectionChanged += (_, _) =>
        {
            var byGenre = _mode.SelectedIndex == 1;
            _genre.IsEnabled = byGenre;
            genrePanel.IsVisible = byGenre;
        };

        _error = new TextBlock
        {
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            IsVisible = false,
        };

        var browse = new Button
        {
            Content = "Examinar…",
            Margin = new Thickness(8, 0, 0, 0),
            VerticalAlignment = VerticalAlignment.Center,
        };
        Grid.SetColumn(browse, 1);
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
            Background = new SolidColorBrush(Color.Parse("#F2A93B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        create.Click += (_, _) => Accept();

        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(null);

        var fields = new StackPanel
        {
            Margin = new Thickness(24, 20, 24, 12),
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Nuevo proyecto",
                    FontSize = 22,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#F2A93B")),
                },
                new TextBlock
                {
                    Text = "Elige cuánta ayuda quieres: componentes mínimos para programar o un conjunto guiado por género.",
                    FontSize = 11.5,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#93A4BC")),
                },
                new TextBlock { Text = "Modo", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                _mode,
                genrePanel,
                new TextBlock { Text = "Nombre", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                _name,
                new TextBlock { Text = "Carpeta", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children = { _folder, browse },
                },
                _error,
            },
        };

        var actions = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Margin = new Thickness(24, 0, 24, 20),
            Children = { cancel, create },
        };

        var scroll = new ScrollViewer
        {
            Content = fields,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
        };

        var root = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        root.Children.Add(scroll);
        Grid.SetRow(actions, 1);
        root.Children.Add(actions);
        Content = root;
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
