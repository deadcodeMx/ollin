using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor de puntos de NavigationPolygon2D.</summary>
public sealed class NavigationPolygonWindow : Window
{
    private readonly NavigationPolygon2D _draft;
    private readonly ObservableCollection<string> _labels = new();
    private readonly ListBox _list;
    private readonly NumericUpDown _x, _y;
    private int _selected = -1;

    public NavigationPolygonWindow(NavigationPolygon2D polygon)
    {
        _draft = (NavigationPolygon2D)polygon.CloneFor(new Entity());
        Title = "Editor de navegación"; Width = 520; Height = 520; MinWidth = 460; MinHeight = 400;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _list = new ListBox { ItemsSource = _labels, Height = 260 };
        _list.SelectionChanged += (_, _) => Select(_list.SelectedIndex);
        _x = Number(); _y = Number();
        var add = new Button { Content = "+ Punto" };
        add.Click += (_, _) => Add();
        var remove = new Button { Content = "Quitar" };
        remove.Click += (_, _) => Remove();
        var save = new Button { Content = "Guardar polígono", Background = new SolidColorBrush(Color.Parse("#E9A23B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Polígono navegable", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#E9A23B")) },
                new TextBlock { Text = "Puntos en espacio local de la entidad.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                _list,
                new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { add, remove } },
                Row("X", _x), Row("Y", _y),
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, save } },
            },
        };

        Rebuild();
        if (_labels.Count > 0) _list.SelectedIndex = 0;
    }

    public NavigationPolygon2D? Result { get; private set; }

    private static NumericUpDown Number() => new() { Minimum = -10000m, Maximum = 10000m, Increment = 0.1m, FormatString = "0.##" };

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("78,*") };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1); grid.Children.Add(field); return grid;
    }

    private void Rebuild()
    {
        _labels.Clear();
        for (var i = 0; i < _draft.Points.Count; i++) _labels.Add($"{i}: {_draft.Points[i].X:0.##}, {_draft.Points[i].Y:0.##}");
    }

    private void Select(int index)
    {
        Apply();
        _selected = index;
        if (index < 0 || index >= _draft.Points.Count) return;
        _x.Value = (decimal)_draft.Points[index].X;
        _y.Value = (decimal)_draft.Points[index].Y;
    }

    private void Add()
    {
        Apply();
        _draft.Points.Add(Vector2.Zero);
        Rebuild();
        _list.SelectedIndex = _labels.Count - 1;
    }

    private void Remove()
    {
        if (_selected < 0 || _selected >= _draft.Points.Count) return;
        _draft.Points.RemoveAt(_selected);
        Rebuild();
        _selected = -1;
    }

    private void Apply()
    {
        if (_selected >= 0 && _selected < _draft.Points.Count)
            _draft.Points[_selected] = new Vector2((float)(_x.Value ?? 0m), (float)(_y.Value ?? 0m));
        Rebuild();
    }
}
