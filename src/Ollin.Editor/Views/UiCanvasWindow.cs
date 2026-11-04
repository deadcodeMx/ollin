using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor de elementos de un UiCanvas.</summary>
public sealed class UiCanvasWindow : Window
{
    private readonly UiCanvas _draft;
    private readonly ObservableCollection<string> _labels = new();
    private readonly ListBox _list;
    private readonly ComboBox _kind;
    private readonly ComboBox _anchor;
    private readonly TextBox _id;
    private readonly TextBox _text;
    private readonly NumericUpDown _x, _y, _w, _h, _font;
    private readonly TextBox _action;
    private readonly CheckBox _visible;
    private int _selected = -1;

    public UiCanvasWindow(UiCanvas canvas)
    {
        _draft = (UiCanvas)canvas.CloneFor(new Entity());
        Title = "Editor de UI"; Width = 720; Height = 600; MinWidth = 600; MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _list = new ListBox { ItemsSource = _labels, Height = 360 };
        _list.SelectionChanged += (_, _) => Select(_list.SelectedIndex);
        _kind = new ComboBox { ItemsSource = Enum.GetValues<UiElementKind>(), SelectedIndex = 1 };
        _anchor = new ComboBox { ItemsSource = Enum.GetValues<UiAnchor>(), SelectedIndex = 0 };
        _id = new TextBox { Watermark = "id" };
        _text = new TextBox { Watermark = "texto" };
        _x = Number(); _y = Number(); _w = Number(220); _h = Number(36); _font = Number(14);
        _action = new TextBox { Watermark = "acción / flag" };
        _visible = new CheckBox { Content = "Visible", IsChecked = true };

        var add = new Button { Content = "+ Elemento" };
        add.Click += (_, _) => Add();
        var remove = new Button { Content = "Quitar" };
        remove.Click += (_, _) => Remove();
        var save = new Button { Content = "Guardar UI", Background = new SolidColorBrush(Color.Parse("#E9A23B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Editor de UI", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#E9A23B")) },
                new TextBlock { Text = "Elementos del UiCanvas. Se guardan en la escena.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("220,*"),
                    Children =
                    {
                        new StackPanel { Spacing = 6, Children = { _list, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { add, remove } } } },
                        new StackPanel
                        {
                            Margin = new Thickness(14, 0, 0, 0), Spacing = 5,
                            Children =
                            {
                                Row("Id", _id), Row("Texto", _text), Row("Tipo", _kind), Row("Anclaje", _anchor),
                                Row("X", _x), Row("Y", _y), Row("Ancho", _w), Row("Alto", _h), Row("Fuente", _font),
                                Row("Acción", _action), _visible,
                            },
                        },
                    },
                },
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right, Children = { cancel, save } },
            },
        };

        Rebuild();
        if (_labels.Count > 0) _list.SelectedIndex = 0;
    }

    public UiCanvas? Result { get; private set; }

    private static NumericUpDown Number(decimal value = 0) => new() { Minimum = -10000m, Maximum = 10000m, Increment = 1m, Value = value, FormatString = "0.##" };

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("78,*") };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1); grid.Children.Add(field); return grid;
    }

    private UiElement? Selected => _selected >= 0 && _selected < _draft.Elements.Count ? _draft.Elements[_selected] : null;

    private void Rebuild()
    {
        _labels.Clear();
        foreach (var element in _draft.Elements) _labels.Add($"{element.Id} · {element.Kind}");
    }

    private void Select(int index)
    {
        Apply();
        _selected = index;
        if (Selected is not { } element) return;
        _id.Text = element.Id; _text.Text = element.Text; _kind.SelectedItem = element.Kind; _anchor.SelectedItem = element.Anchor;
        _x.Value = (decimal)element.X; _y.Value = (decimal)element.Y; _w.Value = (decimal)element.Width; _h.Value = (decimal)element.Height;
        _font.Value = (decimal)element.FontSize; _action.Text = element.Action ?? string.Empty; _visible.IsChecked = element.Visible;
    }

    private void Add()
    {
        Apply();
        var element = new UiElement { Id = $"element{_draft.Elements.Count + 1}", Kind = UiElementKind.Button, Text = "Botón" };
        _draft.Elements.Add(element);
        Rebuild();
        _list.SelectedIndex = _labels.Count - 1;
    }

    private void Remove()
    {
        if (Selected is null) return;
        _draft.Elements.RemoveAt(_selected);
        Rebuild();
        _list.SelectedIndex = System.Math.Min(_selected, _labels.Count - 1);
        _selected = -1;
    }

    private void Apply()
    {
        if (Selected is not { } element) return;
        element.Id = _id.Text ?? "element";
        element.Text = _text.Text ?? string.Empty;
        if (_kind.SelectedItem is UiElementKind kind) element.Kind = kind;
        if (_anchor.SelectedItem is UiAnchor anchor) element.Anchor = anchor;
        element.X = (float)(_x.Value ?? 0m); element.Y = (float)(_y.Value ?? 0m);
        element.Width = MathF.Max(1f, (float)(_w.Value ?? 220m)); element.Height = MathF.Max(1f, (float)(_h.Value ?? 36m));
        element.FontSize = MathF.Max(6f, (float)(_font.Value ?? 14m));
        element.Action = string.IsNullOrWhiteSpace(_action.Text) ? null : _action.Text;
        element.Visible = _visible.IsChecked == true;
        Rebuild();
    }
}
