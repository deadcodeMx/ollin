using System.Globalization;
using System.IO;
using System.Numerics;
using System.Reflection;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Editor.ViewModels;

namespace Ollin.Editor.Views;

/// <summary>Inspector 2D genérico: solo componentes del núcleo, editados por reflexión.</summary>
public sealed class ComponentInspectorView : UserControl
{
    private static readonly Type[] Catalog =
    {
        typeof(SpriteRenderer), typeof(Camera2D), typeof(CanvasItem2D), typeof(CanvasLayer),
        typeof(SubViewport2D), typeof(BoxCollider), typeof(Rigidbody2D), typeof(CharacterBody2D),
        typeof(Area2D), typeof(Tilemap), typeof(Light2D), typeof(ParticleEmitter2D), typeof(Line2D),
        typeof(Polygon2D), typeof(Mesh2D), typeof(SkinnedMesh2D), typeof(Skeleton2D),
        typeof(AnimationPlayer2D), typeof(AnimationStateMachine), typeof(AudioSource), typeof(UiCanvas),
        typeof(NavigationPolygon2D), typeof(NavigationAgent2D), typeof(ScriptComponent), typeof(NodeGraph),
    };

    private readonly MainViewModel _viewModel;
    private Entity? _entity;
    private readonly StackPanel _root = new() { Spacing = 8, Margin = new Thickness(12) };

    public ComponentInspectorView(MainViewModel viewModel)
    {
        _viewModel = viewModel;
        Content = new ScrollViewer { Content = _root, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Rebuild();
    }

    public void ShowEntity(Entity? entity)
    {
        _entity = entity;
        Rebuild();
    }

    private void Rebuild()
    {
        _root.Children.Clear();
        if (_entity is null)
        {
            _root.Children.Add(new TextBlock { Text = "Selecciona una entidad", Classes = { "hint" } });
            return;
        }
        _root.Children.Add(new TextBlock { Text = _entity.Name, FontSize = 20, FontWeight = FontWeight.Bold, Foreground = new SolidColorBrush(Color.Parse("#F2A93B")) });
        _root.Children.Add(new TextBlock { Text = "Solo componentes 2D del motor. Edita valores y guarda al cerrar.", FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#93A4BC")) });
        _root.Children.Add(new TextBlock { Text = "COMPONENTES", Classes = { "section" }, Margin = new Thickness(0, 8, 0, 0) });

        foreach (var component in _entity.Components.ToList())
        {
            var panel = new StackPanel { Spacing = 4 };
            foreach (var property in Editable(component.GetType()))
            {
                panel.Children.Add(Editor(component, property));
            }
            if (component is ScriptComponent scriptComponent)
            {
                var actions = new WrapPanel { Margin = new Thickness(0, 4, 0, 0) };
                var openScript = new Button { Content = "Abrir script", Margin = new Thickness(0, 0, 6, 4) };
                openScript.Click += (_, _) =>
                {
                    if (string.IsNullOrWhiteSpace(scriptComponent.ScriptPath)) _viewModel.OpenSelectedScript();
                    else _viewModel.OpenScript(Path.Combine(_viewModel.ProjectRoot, scriptComponent.ScriptPath));
                };
                actions.Children.Add(openScript);

                var openGraph = new Button { Content = "Abrir grafo", Margin = new Thickness(0, 0, 6, 4) };
                openGraph.Click += (_, _) =>
                {
                    var graph = _entity.GetComponent<NodeGraph>();
                    if (graph is null)
                    {
                        graph = _entity.AddComponent(new NodeGraph());
                        graph.Enabled = true;
                        _viewModel.MarkDirty("componente");
                    }
                    var window = new NodeGraphWindow(_viewModel, _entity);
                    if (TopLevel.GetTopLevel(this) is Window owner) window.Show(owner); else window.Show();
                };
                actions.Children.Add(openGraph);
                panel.Children.Add(actions);
            }
            var remove = new Button { Content = "Quitar componente", HorizontalAlignment = HorizontalAlignment.Right, Classes = { "tool" } };
            remove.Click += (_, _) => { _entity.RemoveComponent(component); _viewModel.MarkDirty("componente"); Rebuild(); };
            panel.Children.Add(remove);
            _root.Children.Add(new Expander
            {
                Header = FriendlyHeader(component.GetType()), Content = panel, IsExpanded = true,
                HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 2),
            });
        }

        var available = Catalog.Where(type => _entity.Components.All(c => c.GetType() != type)).ToList();

        if (available.Count > 0)
        {
            var combo = new ComboBox { ItemsSource = available.Select(DisplayName).ToList(), SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
            var add = new Button { Content = "Añadir componente" };
            add.Click += (_, _) =>
            {
                if (combo.SelectedIndex < 0) return;
                if (Activator.CreateInstance(available[combo.SelectedIndex]) is Component created)
                {
                    _entity.AddComponent(created); _viewModel.MarkDirty("componente"); Rebuild();
                }
            };
            _root.Children.Add(new StackPanel { Spacing = 6, Margin = new Thickness(0, 8, 0, 0), Children = { combo, add } });
        }
    }

    private static string FriendlyHeader(Type type) =>
        type == typeof(ScriptComponent) ? "Script" :
        type == typeof(NodeGraph) ? "Grafo de nodos" : type.Name;

    private static string DisplayName(Type type) =>
        type == typeof(ScriptComponent) ? "Script (ScriptComponent)" :
        type == typeof(NodeGraph) ? "Grafo de nodos" : type.Name;

    private static IEnumerable<PropertyInfo> Editable(Type type) =>
        type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(p => p.CanRead && p.CanWrite && p.GetIndexParameters().Length == 0)
            .Where(p => p.PropertyType.IsPrimitive || p.PropertyType.IsEnum || p.PropertyType == typeof(string) ||
                        p.PropertyType == typeof(Vector2) || p.PropertyType == typeof(Vector3) || p.PropertyType == typeof(Vector4))
            .OrderBy(p => p.Name);

    private Control Editor(Component component, PropertyInfo property)
    {
        var value = property.GetValue(component);
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("104,*") };
        row.Children.Add(new TextBlock { Text = property.Name, FontSize = 11, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap, Foreground = new SolidColorBrush(Color.Parse("#B8C0D0")) });

        Control field;
        if (property.PropertyType == typeof(bool))
        {
            var check = new CheckBox { IsChecked = value as bool? ?? false };
            check.IsCheckedChanged += (_, _) => Set(component, property, check.IsChecked == true);
            field = check;
        }
        else if (property.PropertyType.IsEnum)
        {
            var combo = new ComboBox { ItemsSource = Enum.GetNames(property.PropertyType), SelectedItem = value?.ToString(), FontSize = 11 };
            combo.SelectionChanged += (_, _) => { if (combo.SelectedItem is string name) Set(component, property, Enum.Parse(property.PropertyType, name)); };
            field = combo;
        }
        else if (property.PropertyType == typeof(Vector2) || property.PropertyType == typeof(Vector3) || property.PropertyType == typeof(Vector4))
        {
            var stack = new StackPanel { Spacing = 3 };
            stack.Children.Add(new TextBlock { Text = property.Name, FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#B8C0D0")) });
            stack.Children.Add(VectorEditor(component, property, value));
            return stack;
        }
        else
        {
            var box = new TextBox { Text = Format(value), FontSize = 11 };
            box.LostFocus += (_, _) => Set(component, property, Parse(box.Text, property.PropertyType, value));
            field = box;
        }
        field.HorizontalAlignment = HorizontalAlignment.Stretch;
        Grid.SetColumn(field, 1); row.Children.Add(field);
        return row;
    }

    private Control VectorEditor(Component component, PropertyInfo property, object? value)
    {
        var count = property.PropertyType == typeof(Vector2) ? 2 : property.PropertyType == typeof(Vector3) ? 3 : 4;
        var system = value ?? Activator.CreateInstance(property.PropertyType)!;
        var values = new float[count];
        for (var i = 0; i < count; i++) values[i] = (float)(property.PropertyType.GetField("X") is null ? 0 : ((dynamic)system).GetType().GetField(i switch { 0 => "X", 1 => "Y", 2 => "Z", _ => "W" })!.GetValue(system) ?? 0f);
        var panel = new WrapPanel { Orientation = Orientation.Horizontal };
        var boxes = new NumericUpDown[count];
        for (var i = 0; i < count; i++)
        {
            var box = new NumericUpDown { Value = (decimal)values[i], Increment = 0.1m, FormatString = "0.##", Width = 118, MinWidth = 106, Margin = new Thickness(0, 0, 4, 4) };
            box.ValueChanged += (_, _) =>
            {
                var v = new float[count];
                for (var j = 0; j < count; j++) v[j] = (float)(boxes[j].Value ?? 0m);
                object result = count switch
                {
                    2 => new Vector2(v[0], v[1]),
                    3 => new Vector3(v[0], v[1], v[2]),
                    _ => new Vector4(v[0], v[1], v[2], v[3]),
                };
                Set(component, property, result);
            };
            boxes[i] = box; panel.Children.Add(box);
        }
        return panel;
    }

    private void Set(Component component, PropertyInfo property, object? value)
    {
        try
        {
            if (Equals(property.GetValue(component), value)) return;
            property.SetValue(component, value);
            _viewModel.MarkDirty("inspector");
        }
        catch { }
    }

    private static string Format(object? value) => value switch
    {
        null => string.Empty,
        float f => f.ToString("0.###", CultureInfo.InvariantCulture),
        double d => d.ToString("0.###", CultureInfo.InvariantCulture),
        _ => value.ToString() ?? string.Empty,
    };

    private static object? Parse(string? text, Type type, object? fallback)
    {
        text ??= string.Empty;
        try
        {
            if (type == typeof(string)) return text;
            if (type == typeof(float)) return float.Parse(text, CultureInfo.InvariantCulture);
            if (type == typeof(double)) return double.Parse(text, CultureInfo.InvariantCulture);
            if (type == typeof(int)) return int.Parse(text, CultureInfo.InvariantCulture);
            if (type == typeof(uint)) return uint.Parse(text, CultureInfo.InvariantCulture);
            return Convert.ChangeType(text, type, CultureInfo.InvariantCulture);
        }
        catch { return fallback; }
    }
}
