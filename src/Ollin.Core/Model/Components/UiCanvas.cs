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
}

public enum UiLayout
{
    Free,
    Vertical,
    Horizontal,
}

public sealed class UiTheme
{
    public Vector4 PanelColor { get; set; } = new(0.08f, 0.1f, 0.14f, 0.9f);
    public Vector4 TextColor { get; set; } = new(0.95f, 0.96f, 1f, 1f);
    public Vector4 ButtonColor { get; set; } = new(0.15f, 0.18f, 0.24f, 1f);
    public Vector4 AccentColor { get; set; } = new(0.91f, 0.64f, 0.23f, 1f);
    public float FontSize { get; set; } = 14f;
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
    public Vector4 Color { get; set; } = new(0.95f, 0.95f, 1f, 1f);
    public bool Visible { get; set; } = true;
    public string? Action { get; set; }
    public float Value { get; set; }
    public float Max { get; set; } = 100f;
    public string ParentId { get; set; } = string.Empty;
    public bool Focusable { get; set; }
    public string AccessibilityLabel { get; set; } = string.Empty;
    public UiLayout Layout { get; set; } = UiLayout.Free;
    public float Gap { get; set; } = 6f;
    public float Padding { get; set; } = 8f;
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
        foreach (var element in Elements)
        {
            var copy = new UiElement
            {
                Id = element.Id, Kind = element.Kind, Text = element.Text, Anchor = element.Anchor,
                X = element.X, Y = element.Y, Width = element.Width, Height = element.Height,
                FontSize = element.FontSize, Color = element.Color, Visible = element.Visible,
                Action = element.Action, Value = element.Value, Max = element.Max,
                ParentId = element.ParentId, Layout = element.Layout, Gap = element.Gap, Padding = element.Padding,
                Focusable = element.Focusable, AccessibilityLabel = element.AccessibilityLabel,
            };
            if (!string.IsNullOrWhiteSpace(element.ParentId) && Elements.FirstOrDefault(parent => parent.Id == element.ParentId) is { } parent)
            {
                copy.X += parent.X + parent.Padding;
                copy.Y += parent.Y + parent.Padding;
                var siblings = Elements.Where(item => item.ParentId == parent.Id).ToList();
                var index = siblings.IndexOf(element);
                var offset = index * (parent.Layout == UiLayout.Horizontal ? element.Width + parent.Gap : element.Height + parent.Gap);
                if (parent.Layout == UiLayout.Vertical) copy.Y += offset;
                if (parent.Layout == UiLayout.Horizontal) copy.X += offset;
                if (!parent.Visible) copy.Visible = false;
            }
            result.Add(copy);
        }
        return result;
    }

    public override Component CloneFor(Entity newOwner) => new UiCanvas
    {
        Enabled = Enabled,
        Visible = Visible,
        Theme = Theme,
        Elements = Elements.Select(element => new UiElement
        {
            Id = element.Id, Kind = element.Kind, Text = element.Text, Anchor = element.Anchor, X = element.X, Y = element.Y,
            Width = element.Width, Height = element.Height, FontSize = element.FontSize, Color = element.Color,
            Visible = element.Visible, Action = element.Action, Value = element.Value, Max = element.Max,
            ParentId = element.ParentId, Layout = element.Layout, Gap = element.Gap, Padding = element.Padding,
            Focusable = element.Focusable, AccessibilityLabel = element.AccessibilityLabel,
        }).ToList(),
        Owner = newOwner,
    };
}
