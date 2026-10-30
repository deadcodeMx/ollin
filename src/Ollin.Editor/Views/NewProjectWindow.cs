using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;

namespace Ollin.Editor.Views;

/// <summary>Resultado del diálogo de proyecto nuevo.</summary>
public sealed record NewProjectResult(string Name, string ParentFolder);

/// <summary>Diálogo para crear un proyecto con su propia carpeta.</summary>
public sealed class NewProjectWindow : Window
{
    private readonly TextBox _name;
    private readonly TextBox _folder;

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

        if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(folder))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(folder);
        }
        catch
        {
            return;
        }

        Close(new NewProjectResult(name, folder));
    }
}
