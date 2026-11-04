using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

/// <summary>Diálogo para añadir un script existente o crear uno nuevo desde el inspector.</summary>
public sealed class AddScriptWindow : Window
{
    private readonly ListBox _scripts;
    private readonly TextBox _name;
    private readonly TextBox _folder;
    private readonly TextBlock _error;

    public string? SelectedPath => _scripts.SelectedItem as string;

    public bool CreateNew { get; private set; }

    public string ScriptName => _name.Text?.Trim() ?? string.Empty;

    public string ScriptFolder => _folder.Text?.Trim() ?? "scripts";

    public AddScriptWindow(IReadOnlyList<string> scripts, string suggestedFolder)
    {
        Title = "Añadir script";
        Width = 540;
        Height = 560;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#121722"));
        Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));

        _scripts = new ListBox
        {
            ItemsSource = scripts,
            Height = 190,
        };
        _name = new TextBox { Watermark = "NuevoScript" };
        _folder = new TextBox { Text = suggestedFolder, Watermark = "scripts" };
        _error = new TextBlock
        {
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            IsVisible = false,
        };

        var addExisting = new Button
        {
            Content = "Añadir script seleccionado",
            Background = new SolidColorBrush(Color.Parse("#F2A93B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        addExisting.Click += (_, _) =>
        {
            if (SelectedPath is null)
            {
                Error("Selecciona un script de la lista.");
                return;
            }

            CreateNew = false;
            Close(true);
        };

        var create = new Button { Content = "Crear nuevo y añadir", HorizontalAlignment = HorizontalAlignment.Stretch };
        create.Click += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(ScriptName) || !char.IsLetter(ScriptName[0]) ||
                ScriptName.Any(c => !char.IsLetterOrDigit(c) && c != '_'))
            {
                Error("El nombre debe empezar por letra y usar solo letras, números o _.");
                return;
            }

            CreateNew = true;
            Close(true);
        };

        var cancel = new Button { Content = "Cancelar", HorizontalAlignment = HorizontalAlignment.Right };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Añadir script",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#F2A93B")),
                },
                new TextBlock
                {
                    Text = "Scripts existentes en la carpeta scripts/",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#93A4BC")),
                },
                _scripts,
                addExisting,
                new Border
                {
                    Height = 1,
                    Background = new SolidColorBrush(Color.Parse("#2B3342")),
                    Margin = new Thickness(0, 8),
                },
                new TextBlock
                {
                    Text = "Crear script nuevo",
                    FontSize = 13,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#F2A93B")),
                },
                new TextBlock { Text = "Nombre", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                _name,
                new TextBlock { Text = "Carpeta de guardado (relativa al proyecto)", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                _folder,
                _error,
                create,
                cancel,
            },
        };
    }

    private void Error(string message)
    {
        _error.Text = message;
        _error.IsVisible = true;
    }
}
