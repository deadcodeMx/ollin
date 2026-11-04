using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor del UiTheme de un UiCanvas.</summary>
public sealed class UiThemeWindow : Window
{
    private readonly UiCanvas _draft;
    private readonly NumericUpDown _panelR, _panelG, _panelB, _panelA;
    private readonly NumericUpDown _textR, _textG, _textB, _textA;
    private readonly NumericUpDown _buttonR, _buttonG, _buttonB, _buttonA;
    private readonly NumericUpDown _accentR, _accentG, _accentB, _accentA;
    private readonly NumericUpDown _fontSize;
    private readonly TextBox _fontFamily;

    public UiThemeWindow(UiCanvas canvas)
    {
        _draft = (UiCanvas)canvas.CloneFor(new Entity());
        Title = "Tema de UI"; Width = 580; Height = 520; MinWidth = 500; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        var theme = _draft.Theme;
        _panelR = Number(theme.PanelColor.X); _panelG = Number(theme.PanelColor.Y); _panelB = Number(theme.PanelColor.Z); _panelA = Number(theme.PanelColor.W);
        _textR = Number(theme.TextColor.X); _textG = Number(theme.TextColor.Y); _textB = Number(theme.TextColor.Z); _textA = Number(theme.TextColor.W);
        _buttonR = Number(theme.ButtonColor.X); _buttonG = Number(theme.ButtonColor.Y); _buttonB = Number(theme.ButtonColor.Z); _buttonA = Number(theme.ButtonColor.W);
        _accentR = Number(theme.AccentColor.X); _accentG = Number(theme.AccentColor.Y); _accentB = Number(theme.AccentColor.Z); _accentA = Number(theme.AccentColor.W);
        _fontSize = Number(theme.FontSize); _fontSize.Minimum = 6; _fontSize.Maximum = 96;
        _fontFamily = new TextBox { Text = theme.FontFamily };

        var save = new Button { Content = "Guardar tema", Background = new SolidColorBrush(Color.Parse("#E9A23B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" }; cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Tema de UI", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#E9A23B")) },
                new TextBlock { Text = "Colores RGBA 0..1 y fuente del tema.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                ColorRow("Panel", _panelR, _panelG, _panelB, _panelA),
                ColorRow("Texto", _textR, _textG, _textB, _textA),
                ColorRow("Botón", _buttonR, _buttonG, _buttonB, _buttonA),
                ColorRow("Acento", _accentR, _accentG, _accentB, _accentA),
                Row("Fuente", _fontFamily),
                Row("Tamaño", _fontSize),
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, save } },
            },
        };
    }

    public UiCanvas? Result { get; private set; }

    private static NumericUpDown Number(float value) => new() { Minimum = 0, Maximum = 1, Increment = 0.05m, Value = (decimal)value, FormatString = "0.##" };

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("82,*") };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1); grid.Children.Add(field); return grid;
    }

    private static Control ColorRow(string label, params NumericUpDown[] values)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("82,*,*,*,*") };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        for (var i = 0; i < values.Length; i++) { Grid.SetColumn(values[i], i + 1); grid.Children.Add(values[i]); }
        return grid;
    }

    private void Apply()
    {
        _draft.Theme.PanelColor = new Vector4((float)(_panelR.Value ?? 0m), (float)(_panelG.Value ?? 0m), (float)(_panelB.Value ?? 0m), (float)(_panelA.Value ?? 1m));
        _draft.Theme.TextColor = new Vector4((float)(_textR.Value ?? 1m), (float)(_textG.Value ?? 1m), (float)(_textB.Value ?? 1m), (float)(_textA.Value ?? 1m));
        _draft.Theme.ButtonColor = new Vector4((float)(_buttonR.Value ?? 0m), (float)(_buttonG.Value ?? 0m), (float)(_buttonB.Value ?? 0m), (float)(_buttonA.Value ?? 1m));
        _draft.Theme.AccentColor = new Vector4((float)(_accentR.Value ?? 1m), (float)(_accentG.Value ?? 1m), (float)(_accentB.Value ?? 1m), (float)(_accentA.Value ?? 1m));
        _draft.Theme.FontSize = MathF.Max(6f, (float)(_fontSize.Value ?? 14m));
        _draft.Theme.FontFamily = string.IsNullOrWhiteSpace(_fontFamily.Text) ? "Segoe UI" : _fontFamily.Text;
    }
}
