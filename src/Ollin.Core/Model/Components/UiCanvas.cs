using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

public enum UiAnchor
{
    TopLeft, TopCenter, TopRight,
    MiddleLeft, Center, MiddleRight,
    BottomLeft, BottomCenter, BottomRight,
}

public enum UiElementKind
{
    Panel,
    Label,
    Button,
    Bar,
    Container,
    List,
}

public enum UiLayout
{
    Free,
    Vertical,
    Horizontal,
    Grid,
}

public sealed class UiTheme
{
    public Vector4 PanelColor { get; set; } = new(0.08f, 0.1f, 0.14f, 0.9f);
    public Vector4 TextColor { get; set; } = new(0.95f, 0.96f, 1f, 1f);
    public Vector4 ButtonColor { get; set; } = new(0.15f, 0.18f, 0.24f, 1f);
    public Vector4 AccentColor { get; set; } = new(0.91f, 0.64f, 0.23f, 1f);
    public float FontSize { get; set; } = 14f;
    public string FontFamily { get; set; } = "Segoe UI";
}

public sealed class UiElement
{
    public string Id { get; set; } = "element";
    public UiElementKind Kind { get; set; } = UiElementKind.Label;
    public string Text { get; set; } = string.Empty;

    public UiAnchor Anchor { get; set; } = UiAnchor.TopLeft;
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; } = 220f;
    public float Height { get; set; } = 36f;
    public float FontSize { get; set; } = 14f;
    public string FontFamily { get; set; } = "Segoe UI";
    public Vector4 Color { get; set; } = new(0.95f, 0.95f, 1f, 1f);
    public bool Visible { get; set; } = true;
    public string? Action { get; set; }
    public float Value { get; set; }
    public float Max { get; set; } = 100f;
    public string ParentId { get; set; } = string.Empty;
    public bool Focusable { get; set; }
    public List<string> Items { get; set; } = new();
    public bool Scrollable { get; set; } = true;
    public string AccessibilityLabel { get; set; } = string.Empty;
    public UiLayout Layout { get; set; } = UiLayout.Free;
    public float Gap { get; set; } = 6f;
    public float Padding { get; set; } = 8f;

    /// <summary>Columnas cuando Layout es Grid.</summary>
    public int GridColumns { get; set; } = 2;

    /// <summary>Margen derecho cuando el anclaje es a la derecha.</summary>
    public float MarginRight { get; set; }

    /// <summary>Margen inferior cuando el anclaje es abajo.</summary>
    public float MarginBottom { get; set; }

    /// <summary>Texto de ayuda al pasar el cursor.</summary>
    public string Tooltip { get; set; } = string.Empty;
}

/// <summary>Canvas de UI en pantalla, independiente del mundo y la cámara.</summary>
public sealed class UiCanvas : Component
{
    public bool Visible { get; set; } = true;
    public UiTheme Theme { get; set; } = new();
    public List<UiElement> Elements { get; set; } = new();

    public UiElement? Find(string id) => Elements.FirstOrDefault(element => element.Id == id);

    public UiElement Add(string id, UiElementKind kind, string text, float x, float y)
    {
        var element = new UiElement { Id = id, Kind = kind, Text = text, X = x, Y = y };
        Elements.Add(element);
        return element;
    }

    /// <summary>Aplana contenedores y calcula posiciones absolutas para el render.</summary>
    public List<UiElement> Flatten()
    {
        var result = new List<UiElement>();
        var roots = Elements.Where(element => string.IsNullOrWhiteSpace(element.ParentId) ||
            Elements.All(candidate => candidate.Id != element.ParentId)).ToList();

        foreach (var root in roots)
            Layout(root, 0f, 0f, true, 0, result);

        return result;
    }

    private void Layout(UiElement element, float parentX, float parentY, bool parentVisible, int index, List<UiElement> result)
    {
        var parent = Elements.FirstOrDefault(candidate => candidate.Id == element.ParentId);
        var offsetX = parent is null ? 0f : parent.Padding;
        var offsetY = parent is null ? 0f : parent.Padding;
        if (parent is not null)
        {
            var siblings = Elements.Where(candidate => candidate.ParentId == parent.Id).ToList();
            var siblingIndex = System.Math.Max(0, siblings.IndexOf(element));
            if (parent.Layout == UiLayout.Vertical)
                offsetY += siblingIndex * (element.Height + parent.Gap);
            else if (parent.Layout == UiLayout.Horizontal)
                offsetX += siblingIndex * (element.Width + parent.Gap);
            else if (parent.Layout == UiLayout.Grid)
            {
                var columns = System.Math.Max(1, parent.GridColumns);
                offsetX += siblingIndex % columns * (element.Width + parent.Gap);
                offsetY += siblingIndex / columns * (element.Height + parent.Gap);
            }
        }

        var copy = new UiElement
        {
            Id = element.Id, Kind = element.Kind, Text = element.Text, Anchor = element.Anchor,
            X = parentX + element.X + offsetX,
            Y = parentY + element.Y + offsetY,
            Width = element.Width, Height = element.Height,
            FontSize = element.FontSize, FontFamily = element.FontFamily, Color = element.Color,
            Visible = element.Visible && parentVisible,
            Action = element.Action, Value = element.Value, Max = element.Max,
            ParentId = element.ParentId, Layout = element.Layout, Gap = element.Gap, Padding = element.Padding,
            GridColumns = element.GridColumns,
            Focusable = element.Focusable, AccessibilityLabel = element.AccessibilityLabel,
            Items = new List<string>(element.Items), Scrollable = element.Scrollable,
            MarginRight = element.MarginRight, MarginBottom = element.MarginBottom, Tooltip = element.Tooltip,
        };
        result.Add(copy);

        var children = Elements.Where(candidate => candidate.ParentId == element.Id).ToList();
        for (var i = 0; i < children.Count; i++)
            Layout(children[i], copy.X, copy.Y, copy.Visible, i, result);
    }

    public override Component CloneFor(Entity newOwner) => new UiCanvas
    {
        Enabled = Enabled,
        Visible = Visible,
        Theme = Theme,
        Elements = Elements.Select(element => new UiElement
        {
            Id = element.Id, Kind = element.Kind, Text = element.Text, Anchor = element.Anchor, X = element.X, Y = element.Y,
            Width = element.Width, Height = element.Height, FontSize = element.FontSize, FontFamily = element.FontFamily, Color = element.Color,
            Visible = element.Visible, Action = element.Action, Value = element.Value, Max = element.Max,
            ParentId = element.ParentId, Layout = element.Layout, Gap = element.Gap, Padding = element.Padding,
            GridColumns = element.GridColumns,
            Focusable = element.Focusable, AccessibilityLabel = element.AccessibilityLabel,
            Items = new List<string>(element.Items), Scrollable = element.Scrollable,
            MarginRight = element.MarginRight, MarginBottom = element.MarginBottom, Tooltip = element.Tooltip,
        }).ToList(),
        Owner = newOwner,
    };
}
