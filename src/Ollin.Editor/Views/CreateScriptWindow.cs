using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

/// <summary>Diálogo para elegir nombre y carpeta de un script nuevo.</summary>
public sealed class CreateScriptWindow : Window
{
    private readonly TextBox _name;
    private readonly TextBox _folder;
    private readonly TextBlock _error;

    public string ScriptName => _name.Text?.Trim() ?? string.Empty;

    public string ScriptFolder => _folder.Text?.Trim() ?? "scripts";

    public CreateScriptWindow(string suggestedName, string suggestedFolder)
    {
        Title = "Nuevo script";
        Width = 480;
        Height = 300;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _name = new TextBox { Text = suggestedName, Watermark = "JugadorScript" };
        _folder = new TextBox { Text = suggestedFolder, Watermark = "scripts" };
        _error = new TextBlock
        {
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            IsVisible = false,
        };

        var create = new Button
        {
            Content = "Crear script",
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        create.Click += (_, _) => Accept();

        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Nuevo script C#",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock
                {
                    Text = "El nombre debe ser un identificador C# y la ruta es relativa al proyecto.",
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new TextBlock { Text = "Nombre", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _name,
                new TextBlock { Text = "Carpeta de guardado", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _folder,
                _error,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, create },
                },
            },
        };
    }

    private void Accept()
    {
        var name = ScriptName;
        if (string.IsNullOrWhiteSpace(name) || !char.IsLetter(name[0]) || name.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
        {
            _error.Text = "Usa letras, números o guion bajo; debe empezar por letra.";
            _error.IsVisible = true;
            return;
        }

        var folder = ScriptFolder.Replace('\\', '/').Trim('/');
        if (folder.Split('/').Any(part => part is ".." or "."))
        {
            _error.Text = "La carpeta no puede contener «..» ni «.».";
            _error.IsVisible = true;
            return;
        }

        Close(true);
    }
}
