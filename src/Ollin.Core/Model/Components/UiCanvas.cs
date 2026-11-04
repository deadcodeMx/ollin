using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

public enum UiElementKind
{
    Panel,
    Label,
    Button,
    Bar,
}

public sealed class UiElement
{
    public string Id { get; set; } = "element";
    public UiElementKind Kind { get; set; } = UiElementKind.Label;
    public string Text { get; set; } = string.Empty;
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
}

/// <summary>Canvas de UI en pantalla, independiente del mundo y la cámara.</summary>
public sealed class UiCanvas : Component
{
    public bool Visible { get; set; } = true;
    public List<UiElement> Elements { get; set; } = new();

    public UiElement? Find(string id) => Elements.FirstOrDefault(element => element.Id == id);

    public UiElement Add(string id, UiElementKind kind, string text, float x, float y)
    {
        var element = new UiElement { Id = id, Kind = kind, Text = text, X = x, Y = y };
        Elements.Add(element);
        return element;
    }

    public override Component CloneFor(Entity newOwner) => new UiCanvas
    {
        Enabled = Enabled,
        Visible = Visible,
        Elements = Elements.Select(element => new UiElement
        {
            Id = element.Id, Kind = element.Kind, Text = element.Text, X = element.X, Y = element.Y,
            Width = element.Width, Height = element.Height, FontSize = element.FontSize, Color = element.Color,
            Visible = element.Visible, Action = element.Action, Value = element.Value, Max = element.Max,
        }).ToList(),
        Owner = newOwner,
    };
}
