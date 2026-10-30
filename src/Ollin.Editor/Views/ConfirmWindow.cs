using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

/// <summary>Confirmación modal simple (por ejemplo, cerrar una escena con cambios).</summary>
public sealed class ConfirmWindow : Window
{
    public ConfirmWindow(string message, string confirmText, string? detail = null)
    {
        Title = "Confirmar";
        Width = 460;
        Height = 210;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));

        var confirm = new Button
        {
            Content = confirmText,
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        confirm.Click += (_, _) => Close(true);

        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        var panel = new StackPanel
        {
            Margin = new Thickness(24),
            Spacing = 12,
            Children =
            {
                new TextBlock
                {
                    Text = message,
                    FontSize = 15,
                    FontWeight = FontWeight.SemiBold,
                    TextWrapping = TextWrapping.Wrap,
                },
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Margin = new Thickness(0, 14, 0, 0),
                    Children = { cancel, confirm },
                },
            },
        };

        if (!string.IsNullOrWhiteSpace(detail))
        {
            panel.Children.Insert(1, new TextBlock
            {
                Text = detail,
                FontSize = 11.5,
                TextWrapping = TextWrapping.Wrap,
                Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
            });
        }

        Content = panel;
    }
}
