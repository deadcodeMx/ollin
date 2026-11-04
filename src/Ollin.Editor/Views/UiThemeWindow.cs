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
        Background = new SolidColorBrush(Color.Parse("#121722"));
        Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));

        var theme = _draft.Theme;
        _panelR = Number(theme.PanelColor.X); _panelG = Number(theme.PanelColor.Y); _panelB = Number(theme.PanelColor.Z); _panelA = Number(theme.PanelColor.W);
        _textR = Number(theme.TextColor.X); _textG = Number(theme.TextColor.Y); _textB = Number(theme.TextColor.Z); _textA = Number(theme.TextColor.W);
        _buttonR = Number(theme.ButtonColor.X); _buttonG = Number(theme.ButtonColor.Y); _buttonB = Number(theme.ButtonColor.Z); _buttonA = Number(theme.ButtonColor.W);
        _accentR = Number(theme.AccentColor.X); _accentG = Number(theme.AccentColor.Y); _accentB = Number(theme.AccentColor.Z); _accentA = Number(theme.AccentColor.W);
        _fontSize = Number(theme.FontSize); _fontSize.Minimum = 6; _fontSize.Maximum = 96;
        _fontFamily = new TextBox { Text = theme.FontFamily };

        var dark = new Button { Content = "Oscuro" };
        dark.Click += (_, _) => Preset(new Vector4(0.08f, 0.09f, 0.12f, 0.96f), new Vector4(0.93f, 0.94f, 0.96f, 1f), new Vector4(0.16f, 0.18f, 0.23f, 1f), new Vector4(0.91f, 0.64f, 0.23f, 1f));
        var light = new Button { Content = "Claro" };
        light.Click += (_, _) => Preset(new Vector4(0.93f, 0.94f, 0.97f, 0.98f), new Vector4(0.08f, 0.09f, 0.12f, 1f), new Vector4(0.80f, 0.83f, 0.89f, 1f), new Vector4(0.75f, 0.39f, 0.10f, 1f));
        var contrast = new Button { Content = "Alto contraste" };
        contrast.Click += (_, _) => Preset(new Vector4(0f, 0f, 0f, 0.98f), new Vector4(1f, 1f, 0f, 1f), new Vector4(0.1f, 0.1f, 0.1f, 1f), new Vector4(0f, 1f, 1f, 1f));
        var presets = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, Children = { new TextBlock { Text = "Presets", Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center }, dark, light, contrast } };
        var save = new Button { Content = "Guardar tema", Background = new SolidColorBrush(Color.Parse("#F2A93B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" }; cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Tema de UI", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#F2A93B")) },
                new TextBlock { Text = "Colores RGBA 0..1 y fuente del tema.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                ColorRow("Panel", _panelR, _panelG, _panelB, _panelA),
                ColorRow("Texto", _textR, _textG, _textB, _textA),
                ColorRow("Botón", _buttonR, _buttonG, _buttonB, _buttonA),
                ColorRow("Acento", _accentR, _accentG, _accentB, _accentA),
                Row("Fuente", _fontFamily),
                Row("Tamaño", _fontSize),
                presets,
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

    private void Preset(Vector4 panel, Vector4 text, Vector4 button, Vector4 accent)
    {
        _panelR.Value = (decimal)panel.X; _panelG.Value = (decimal)panel.Y; _panelB.Value = (decimal)panel.Z; _panelA.Value = (decimal)panel.W;
        _textR.Value = (decimal)text.X; _textG.Value = (decimal)text.Y; _textB.Value = (decimal)text.Z; _textA.Value = (decimal)text.W;
        _buttonR.Value = (decimal)button.X; _buttonG.Value = (decimal)button.Y; _buttonB.Value = (decimal)button.Z; _buttonA.Value = (decimal)button.W;
        _accentR.Value = (decimal)accent.X; _accentG.Value = (decimal)accent.Y; _accentB.Value = (decimal)accent.Z; _accentA.Value = (decimal)accent.W;
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
