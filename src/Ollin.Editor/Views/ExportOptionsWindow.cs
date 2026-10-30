using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;

namespace Ollin.Editor.Views;

public sealed record ExportOptions(bool Splash, bool Watermark);

/// <summary>Opciones de exportación: pantalla de inicio y marca de agua.</summary>
public sealed class ExportOptionsWindow : Window
{
    private readonly CheckBox _splash;
    private readonly CheckBox _watermark;

    public ExportOptionsWindow()
    {
        Title = "Exportar juego";
        Width = 440;
        Height = 250;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));

        _splash = new CheckBox
        {
            Content = "Mostrar pantalla de inicio con el logo de Ollin",
            IsChecked = true,
            FontSize = 12.5,
            Foreground = Brushes.White,
        };

        _watermark = new CheckBox
        {
            Content = "Mostrar marca de agua «Hecho con Ollin»",
            IsChecked = true,
            FontSize = 12.5,
            Foreground = Brushes.White,
        };

        var export = new Button { Content = "Exportar…", MinWidth = 110 };
        export.Click += (_, _) => Close(new ExportOptions(_splash.IsChecked == true, _watermark.IsChecked == true));

        var cancel = new Button { Content = "Cancelar", MinWidth = 90 };
        cancel.Click += (_, _) => Close(null);

        Content = new StackPanel
        {
            Margin = new Thickness(24, 22),
            Spacing = 14,
            Children =
            {
                new TextBlock
                {
                    Text = "El juego se empaqueta con sus escenas, assets y scripts compilados.",
                    TextWrapping = TextWrapping.Wrap,
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#B9C2D0")),
                },
                _splash,
                _watermark,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    HorizontalAlignment = HorizontalAlignment.Right,
                    Spacing = 8,
                    Margin = new Thickness(0, 10, 0, 0),
                    Children = { cancel, export },
                },
            },
        };
    }
}
