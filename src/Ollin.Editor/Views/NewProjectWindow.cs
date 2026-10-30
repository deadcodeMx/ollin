using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Ollin.Core.Projects;

namespace Ollin.Editor.Views;

/// <summary>Resultado del diálogo de proyecto nuevo.</summary>
public sealed record NewProjectResult(string Name, string ParentFolder, ProjectTemplate Template);

/// <summary>Diálogo para crear un proyecto con su propia carpeta.</summary>
public sealed class NewProjectWindow : Window
{
    private readonly TextBox _name;
    private readonly TextBox _folder;
    private readonly ComboBox _template;
    private readonly TextBlock _error;

    public NewProjectWindow()
    {
        var documents = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        var suggested = Path.Combine(documents, "Ollin Projects");

        Title = "Nuevo proyecto";
        Width = 560;
        Height = 300;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _name = new TextBox { Text = "MiJuego", Watermark = "Nombre del proyecto" };
        _folder = new TextBox { Text = suggested };
        _template = new ComboBox
        {
            ItemsSource = new[] { "Vacío (cámara + jugador)", "Plataformas", "Terror" },
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };

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
            if (!string.IsNullOrWhiteSpace(path))
            {
                _folder.Text = path;
            }
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
                    Text = "Se crearán las carpetas scenes, scripts, assets y prefabs.",
                    FontSize = 11.5,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new TextBlock { Text = "Plantilla", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _template,
                new TextBlock { Text = "Nombre", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _name,
                new TextBlock { Text = "Carpeta", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,Auto"),
                    Children =
                    {
                        _folder,
                        browse,
                    },
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

        try
        {
            Directory.CreateDirectory(folder!);
        }
        catch (Exception exception)
        {
            ShowError($"No se puede usar esa carpeta: {exception.Message}");
            return;
        }

        var template = _template.SelectedIndex switch
        {
            1 => ProjectTemplate.Platformer,
            2 => ProjectTemplate.Horror,
            _ => ProjectTemplate.Empty,
        };

        Close(new NewProjectResult(name, folder, template));
    }

    private void ShowError(string message)
    {
        _error.Text = message;
        _error.IsVisible = true;
    }
}
