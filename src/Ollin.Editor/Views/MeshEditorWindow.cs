using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor de vértices y triángulos de Mesh2D.</summary>
public sealed class MeshEditorWindow : Window
{
    private readonly Mesh2D _draft;
    private readonly ObservableCollection<string> _vertexLabels = new();
    private readonly ObservableCollection<string> _triangleLabels = new();
    private readonly ListBox _vertices;
    private readonly ListBox _triangles;
    private readonly NumericUpDown _x, _y;
    private readonly NumericUpDown _a, _b, _c;
    private int _vertexIndex = -1, _triangleIndex = -1;

    public MeshEditorWindow(Mesh2D mesh)
    {
        _draft = (Mesh2D)mesh.CloneFor(new Entity());
        Title = "Editor de malla"; Width = 700; Height = 600; MinWidth = 560; MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _vertices = new ListBox { ItemsSource = _vertexLabels, Height = 280 };
        _vertices.SelectionChanged += (_, _) => SelectVertex(_vertices.SelectedIndex);
        _triangles = new ListBox { ItemsSource = _triangleLabels, Height = 280 };
        _triangles.SelectionChanged += (_, _) => SelectTriangle(_triangles.SelectedIndex);
        _x = Number(); _y = Number();
        _a = Number(); _b = Number(); _c = Number();

        var addVertex = new Button { Content = "+ Vértice" };
        addVertex.Click += (_, _) => AddVertex();
        var removeVertex = new Button { Content = "Quitar" };
        removeVertex.Click += (_, _) => RemoveVertex();
        var addTriangle = new Button { Content = "+ Triángulo" };
        addTriangle.Click += (_, _) => AddTriangle();
        var removeTriangle = new Button { Content = "Quitar" };
        removeTriangle.Click += (_, _) => RemoveTriangle();
        var save = new Button { Content = "Guardar malla", Background = new SolidColorBrush(Color.Parse("#E9A23B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Editor de malla 2D", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#E9A23B")) },
                new TextBlock { Text = "Vértices en espacio local y triángulos por índices.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#8B94A7")) },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    Children =
                    {
                        new StackPanel { Spacing = 6, Children = { new TextBlock { Text = "Vértices", Classes = { "hint" } }, _vertices, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { addVertex, removeVertex } }, Row("X", _x), Row("Y", _y) } },
                        new StackPanel { Margin = new Thickness(14, 0, 0, 0), Spacing = 6, Children = { new TextBlock { Text = "Triángulos", Classes = { "hint" } }, _triangles, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { addTriangle, removeTriangle } }, Row("Índice A", _a), Row("Índice B", _b), Row("Índice C", _c) } },
                    },
                },
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, save } },
            },
        };

        Rebuild();
        if (_vertexLabels.Count > 0) _vertices.SelectedIndex = 0;
        if (_triangleLabels.Count > 0) _triangles.SelectedIndex = 0;
    }

    public Mesh2D? Result { get; private set; }

    private static NumericUpDown Number() => new() { Minimum = -10000m, Maximum = 10000m, Increment = 0.1m, FormatString = "0.##" };

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("78,*") };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1); grid.Children.Add(field); return grid;
    }

    private void Rebuild()
    {
        _vertexLabels.Clear();
        for (var i = 0; i < _draft.Mesh.Vertices.Count; i++) _vertexLabels.Add($"{i}: {_draft.Mesh.Vertices[i].X:0.##}, {_draft.Mesh.Vertices[i].Y:0.##}");
        _triangleLabels.Clear();
        for (var i = 0; i + 2 < _draft.Mesh.Triangles.Count; i += 3) _triangleLabels.Add($"{i / 3}: {_draft.Mesh.Triangles[i]}, {_draft.Mesh.Triangles[i + 1]}, {_draft.Mesh.Triangles[i + 2]}");
    }

    private void SelectVertex(int index)
    {
        Apply();
        _vertexIndex = index;
        if (index < 0 || index >= _draft.Mesh.Vertices.Count) return;
        _x.Value = (decimal)_draft.Mesh.Vertices[index].X;
        _y.Value = (decimal)_draft.Mesh.Vertices[index].Y;
    }

    private void SelectTriangle(int index)
    {
        Apply();
        _triangleIndex = index;
        var offset = index * 3;
        if (index < 0 || offset + 2 >= _draft.Mesh.Triangles.Count) return;
        _a.Value = _draft.Mesh.Triangles[offset];
        _b.Value = _draft.Mesh.Triangles[offset + 1];
        _c.Value = _draft.Mesh.Triangles[offset + 2];
    }

    private void AddVertex()
    {
        Apply();
        _draft.Mesh.Vertices.Add(Vector2.Zero);
        Rebuild();
        _vertices.SelectedIndex = _vertexLabels.Count - 1;
    }

    private void RemoveVertex()
    {
        if (_vertexIndex < 0 || _vertexIndex >= _draft.Mesh.Vertices.Count) return;
        _draft.Mesh.Vertices.RemoveAt(_vertexIndex);
        _draft.Mesh.Triangles.RemoveAll(index => index == _vertexIndex);
        Rebuild();
        _vertexIndex = -1;
    }

    private void AddTriangle()
    {
        Apply();
        _draft.Mesh.Triangles.Add(0); _draft.Mesh.Triangles.Add(0); _draft.Mesh.Triangles.Add(0);
        Rebuild();
        _triangles.SelectedIndex = _triangleLabels.Count - 1;
    }

    private void RemoveTriangle()
    {
        var offset = _triangleIndex * 3;
        if (_triangleIndex < 0 || offset + 2 >= _draft.Mesh.Triangles.Count) return;
        _draft.Mesh.Triangles.RemoveRange(offset, 3);
        Rebuild();
        _triangleIndex = -1;
    }

    private void Apply()
    {
        if (_vertexIndex >= 0 && _vertexIndex < _draft.Mesh.Vertices.Count)
            _draft.Mesh.Vertices[_vertexIndex] = new Vector2((float)(_x.Value ?? 0m), (float)(_y.Value ?? 0m));
        var offset = _triangleIndex * 3;
        if (_triangleIndex >= 0 && offset + 2 < _draft.Mesh.Triangles.Count)
        {
            _draft.Mesh.Triangles[offset] = (int)(_a.Value ?? 0m);
            _draft.Mesh.Triangles[offset + 1] = (int)(_b.Value ?? 0m);
            _draft.Mesh.Triangles[offset + 2] = (int)(_c.Value ?? 0m);
        }
        Rebuild();
    }
}
