using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Editor.ViewModels;

namespace Ollin.Editor.Views;

/// <summary>Editor de atajos del teclado, persistido en EditorSettings.</summary>
public sealed class ShortcutsWindow : Window
{
    private static readonly (string Action, string Label)[] Actions =
    {
        ("save", "Guardar escena"),
        ("undo", "Deshacer"),
        ("redo", "Rehacer"),
        ("copy", "Copiar"),
        ("paste", "Pegar"),
        ("delete", "Eliminar"),
        ("focus", "Enfocar selección"),
        ("home", "Centrar cámara"),
        ("play", "Reproducir"),
        ("pause", "Pausar"),
        ("newscene", "Nueva escena"),
        ("openscene", "Abrir escena"),
    };

    private readonly EditorSettings _settings;
    private readonly Dictionary<string, TextBox> _boxes = new();
    private readonly TextBlock _error;

    public ShortcutsWindow(EditorSettings settings)
    {
        _settings = settings;
        Title = "Atajos del teclado";
        Width = 460;
        Height = 560;
        MinHeight = 400;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _error = new TextBlock
        {
            FontSize = 11.5,
            TextWrapping = TextWrapping.Wrap,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            IsVisible = false,
        };

        var rows = new StackPanel { Spacing = 6 };
        foreach (var (action, label) in Actions)
        {
            var box = new TextBox
            {
                Text = settings.GetShortcut(action, EditorSettings.DefaultShortcuts[action]),
                Watermark = "Ctrl+S",
                Width = 150,
            };
            _boxes[action] = box;

            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,150") };
            row.Children.Add(new TextBlock
            {
                Text = label,
                VerticalAlignment = VerticalAlignment.Center,
                Foreground = new SolidColorBrush(Color.Parse("#C7D0DE")),
            });
            Grid.SetColumn(box, 1);
            row.Children.Add(box);
            rows.Children.Add(row);
        }

        var reset = new Button { Content = "Restablecer" };
        reset.Click += (_, _) =>
        {
            foreach (var (action, _) in Actions)
            {
                _boxes[action].Text = EditorSettings.DefaultShortcuts[action];
            }
            _error.IsVisible = false;
        };

        var save = new Button
        {
            Content = "Guardar atajos",
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        save.Click += (_, _) => Accept();

        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
            Children = { reset, cancel, save },
        };

        Content = new StackPanel
        {
            Margin = new Thickness(22),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Atajos del teclado",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock
                {
                    Text = "Ejemplos: Ctrl+S, F5, Ctrl+Shift+N. Los cambios se guardan para las próximas sesiones.",
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new ScrollViewer { Content = rows, MaxHeight = 390 },
                _error,
                buttons,
            },
        };
    }

    private void Accept()
    {
        foreach (var (action, label) in Actions)
        {
            var text = _boxes[action].Text?.Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                _error.Text = $"El atajo de «{label}» no puede estar vacío.";
                _error.IsVisible = true;
                return;
            }

            try
            {
                _ = KeyGesture.Parse(text);
            }
            catch
            {
                _error.Text = $"«{text}» no es un atajo válido para «{label}».";
                _error.IsVisible = true;
                return;
            }

            _settings.Shortcuts[action] = text;
        }

        _settings.Save();
        Close(true);
    }
}
