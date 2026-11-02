using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

/// <summary>Diálogo simple para pedir un nombre.</summary>
public sealed class NamePromptWindow : Window
{
    private readonly TextBox _value;
    private readonly TextBlock _error;

    public string Value => _value.Text?.Trim() ?? string.Empty;

    public NamePromptWindow(string title, string label, string suggested)
    {
        Title = title;
        Width = 420;
        Height = 220;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _value = new TextBox { Text = suggested };
        _error = new TextBlock
        {
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            IsVisible = false,
        };

        var accept = new Button
        {
            Content = "Crear",
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        accept.Click += (_, _) => Accept();

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
                    Text = title,
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock { Text = label, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _value,
                _error,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Children = { cancel, accept },
                },
            },
        };
    }

    private void Accept()
    {
        if (string.IsNullOrWhiteSpace(Value) || Value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
        {
            _error.Text = "Escribe un nombre de carpeta válido.";
            _error.IsVisible = true;
            return;
        }

        Close(true);
    }
}
