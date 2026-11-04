using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Views;

/// <summary>Editor de vértices, triángulos y pesos de SkinnedMesh2D.</summary>
public sealed class SkinEditorWindow : Window
{
    private readonly SkinnedMesh2D _draft;
    private readonly ObservableCollection<string> _vertexLabels = new();
    private readonly ObservableCollection<string> _triangleLabels = new();
    private readonly ListBox _vertices;
    private readonly ListBox _triangles;
    private readonly NumericUpDown _x, _y, _boneA, _boneB, _boneC, _boneD, _weightB, _weightC, _weightD;
    private readonly NumericUpDown _a, _b, _c;
    private int _vertexIndex = -1, _triangleIndex = -1;

    public SkinEditorWindow(SkinnedMesh2D mesh)
    {
        _draft = (SkinnedMesh2D)mesh.CloneFor(new Entity());
        Title = "Editor de skinning"; Width = 820; Height = 640; MinWidth = 680; MinHeight = 500;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#121722"));
        Foreground = new SolidColorBrush(Color.Parse("#E6ECF7"));

        _vertices = new ListBox { ItemsSource = _vertexLabels, Height = 230 };
        _vertices.SelectionChanged += (_, _) => SelectVertex(_vertices.SelectedIndex);
        _triangles = new ListBox { ItemsSource = _triangleLabels, Height = 230 };
        _triangles.SelectionChanged += (_, _) => SelectTriangle(_triangles.SelectedIndex);
        _x = Number(); _y = Number(); _boneA = Index(); _boneB = Index(); _boneC = Index(); _boneD = Index();
        _weightB = Weight(); _weightC = Weight(); _weightD = Weight();
        _a = Index(); _b = Index(); _c = Index();

        var addVertex = new Button { Content = "+ Vértice" }; addVertex.Click += (_, _) => AddVertex();
        var removeVertex = new Button { Content = "Quitar" }; removeVertex.Click += (_, _) => RemoveVertex();
        var addTriangle = new Button { Content = "+ Triángulo" }; addTriangle.Click += (_, _) => AddTriangle();
        var removeTriangle = new Button { Content = "Quitar" }; removeTriangle.Click += (_, _) => RemoveTriangle();
        var save = new Button { Content = "Guardar skin", Background = new SolidColorBrush(Color.Parse("#F2A93B")), Foreground = new SolidColorBrush(Color.Parse("#1A1206")) };
        save.Click += (_, _) => { Apply(); Result = _draft; Close(true); };
        var cancel = new Button { Content = "Cancelar" }; cancel.Click += (_, _) => Close(false);

        Content = new StackPanel
        {
            Margin = new Thickness(20), Spacing = 8,
            Children =
            {
                new TextBlock { Text = "Editor de skinning", FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#F2A93B")) },
                new TextBlock { Text = "Hasta 4 huesos por vértice. WeightB/C/D; el resto va al hueso A.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) },
                new Grid
                {
                    ColumnDefinitions = new ColumnDefinitions("*,*"),
                    Children =
                    {
                        new StackPanel { Spacing = 5, Children = { new TextBlock { Text = "Vértices", Classes = { "hint" } }, _vertices, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { addVertex, removeVertex } }, Row("X", _x), Row("Y", _y), Row("Bone A", _boneA), Row("Bone B", _boneB), Row("Bone C", _boneC), Row("Bone D", _boneD), Row("Peso B", _weightB), Row("Peso C", _weightC), Row("Peso D", _weightD) } },
                        new StackPanel { Margin = new Thickness(14, 0, 0, 0), Spacing = 5, Children = { new TextBlock { Text = "Triángulos", Classes = { "hint" } }, _triangles, new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, Children = { addTriangle, removeTriangle } }, Row("Índice A", _a), Row("Índice B", _b), Row("Índice C", _c) } },
                    },
                },
                new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8, Children = { cancel, save } },
            },
        };

        Rebuild();
        if (_vertexLabels.Count > 0) _vertices.SelectedIndex = 0;
        if (_triangleLabels.Count > 0) _triangles.SelectedIndex = 0;
    }

    public SkinnedMesh2D? Result { get; private set; }

    private static NumericUpDown Number() => new() { Minimum = -10000m, Maximum = 10000m, Increment = 0.1m, FormatString = "0.##" };
    private static NumericUpDown Index() => new() { Minimum = 0, Maximum = 64, Increment = 1, FormatString = "0" };
    private static NumericUpDown Weight() => new() { Minimum = 0, Maximum = 1, Increment = 0.05m, FormatString = "0.##" };

    private static Control Row(string label, Control field)
    {
        var grid = new Grid { ColumnDefinitions = new ColumnDefinitions("82,*") };
        grid.Children.Add(new TextBlock { Text = label, Classes = { "hint" }, VerticalAlignment = VerticalAlignment.Center });
        Grid.SetColumn(field, 1); grid.Children.Add(field); return grid;
    }

    private void Rebuild()
    {
        _vertexLabels.Clear();
        for (var i = 0; i < _draft.Vertices.Count; i++) _vertexLabels.Add($"{i}: {_draft.Vertices[i].X:0.##}, {_draft.Vertices[i].Y:0.##}");
        _triangleLabels.Clear();
        for (var i = 0; i + 2 < _draft.Triangles.Count; i += 3) _triangleLabels.Add($"{i / 3}: {_draft.Triangles[i]}, {_draft.Triangles[i + 1]}, {_draft.Triangles[i + 2]}");
    }

    private void SelectVertex(int index)
    {
        Apply();
        _vertexIndex = index;
        if (index < 0 || index >= _draft.Vertices.Count) return;
        _x.Value = (decimal)_draft.Vertices[index].X; _y.Value = (decimal)_draft.Vertices[index].Y;
        _boneA.Value = (int)Get(_draft.BoneA, index); _boneB.Value = (int)Get(_draft.BoneB, index);
        _boneC.Value = (int)Get(_draft.BoneC, index); _boneD.Value = (int)Get(_draft.BoneD, index);
        _weightB.Value = (decimal)Get(_draft.Weight, index); _weightC.Value = (decimal)Get(_draft.WeightC, index); _weightD.Value = (decimal)Get(_draft.WeightD, index);
    }

    private void SelectTriangle(int index)
    {
        Apply();
        _triangleIndex = index;
        var offset = index * 3;
        if (index < 0 || offset + 2 >= _draft.Triangles.Count) return;
        _a.Value = _draft.Triangles[offset]; _b.Value = _draft.Triangles[offset + 1]; _c.Value = _draft.Triangles[offset + 2];
    }

    private void AddVertex()
    {
        Apply();
        _draft.Vertices.Add(Vector2.Zero);
        _draft.BoneA.Add(0); _draft.BoneB.Add(0); _draft.BoneC.Add(0); _draft.BoneD.Add(0);
        _draft.Weight.Add(0f); _draft.WeightC.Add(0f); _draft.WeightD.Add(0f);
        Rebuild(); _vertices.SelectedIndex = _vertexLabels.Count - 1;
    }

    private void RemoveVertex()
    {
        if (_vertexIndex < 0 || _vertexIndex >= _draft.Vertices.Count) return;
        _draft.Vertices.RemoveAt(_vertexIndex);
        foreach (var list in new[] { _draft.BoneA, _draft.BoneB, _draft.BoneC, _draft.BoneD }) if (_vertexIndex < list.Count) list.RemoveAt(_vertexIndex);
        foreach (var list in new[] { _draft.Weight, _draft.WeightC, _draft.WeightD }) if (_vertexIndex < list.Count) list.RemoveAt(_vertexIndex);
        _draft.Triangles.RemoveAll(index => index == _vertexIndex);
        Rebuild(); _vertexIndex = -1;
    }

    private void AddTriangle()
    {
        Apply();
        _draft.Triangles.Add(0); _draft.Triangles.Add(0); _draft.Triangles.Add(0);
        Rebuild(); _triangles.SelectedIndex = _triangleLabels.Count - 1;
    }

    private void RemoveTriangle()
    {
        var offset = _triangleIndex * 3;
        if (_triangleIndex < 0 || offset + 2 >= _draft.Triangles.Count) return;
        _draft.Triangles.RemoveRange(offset, 3);
        Rebuild(); _triangleIndex = -1;
    }

    private void Apply()
    {
        if (_vertexIndex >= 0 && _vertexIndex < _draft.Vertices.Count)
        {
            _draft.Vertices[_vertexIndex] = new Vector2((float)(_x.Value ?? 0m), (float)(_y.Value ?? 0m));
            Set(_draft.BoneA, _vertexIndex, (int)(_boneA.Value ?? 0m)); Set(_draft.BoneB, _vertexIndex, (int)(_boneB.Value ?? 0m));
            Set(_draft.BoneC, _vertexIndex, (int)(_boneC.Value ?? 0m)); Set(_draft.BoneD, _vertexIndex, (int)(_boneD.Value ?? 0m));
            Set(_draft.Weight, _vertexIndex, (float)(_weightB.Value ?? 0m)); Set(_draft.WeightC, _vertexIndex, (float)(_weightC.Value ?? 0m)); Set(_draft.WeightD, _vertexIndex, (float)(_weightD.Value ?? 0m));
        }
        var offset = _triangleIndex * 3;
        if (_triangleIndex >= 0 && offset + 2 < _draft.Triangles.Count)
        {
            _draft.Triangles[offset] = (int)(_a.Value ?? 0m); _draft.Triangles[offset + 1] = (int)(_b.Value ?? 0m); _draft.Triangles[offset + 2] = (int)(_c.Value ?? 0m);
        }
        Rebuild();
    }

    private static int Get(List<int> list, int index) => index < list.Count ? list[index] : 0;
    private static float Get(List<float> list, int index) => index < list.Count ? list[index] : 0f;
    private static void Set(List<int> list, int index, int value) { while (list.Count <= index) list.Add(0); list[index] = value; }
    private static void Set(List<float> list, int index, float value) { while (list.Count <= index) list.Add(0f); list[index] = value; }
}
