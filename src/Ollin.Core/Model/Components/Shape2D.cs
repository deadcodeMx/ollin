using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Polilínea 2D en espacio local.</summary>
public sealed class Line2D : Component
{
    public List<Vector2> Points { get; set; } = new() { new(-1f, 0f), new(1f, 0f) };
    public float Width { get; set; } = 0.05f;
    public Vector4 Color { get; set; } = Vector4.One;
    public bool Loop { get; set; }
    public bool Visible { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new Line2D
    {
        Enabled = Enabled, Points = new List<Vector2>(Points), Width = Width, Color = Color, Loop = Loop, Visible = Visible, Owner = newOwner,
    };
}

/// <summary>Polígono de contorno 2D. El relleno se limita a trazos por ahora.</summary>
public sealed class Polygon2D : Component
{
    public List<Vector2> Points { get; set; } = new()
    {
        new(-1f, -1f), new(1f, -1f), new(1f, 1f), new(-1f, 1f),
    };
    public float Width { get; set; } = 0.05f;
    public Vector4 Color { get; set; } = Vector4.One;
    public bool Filled { get; set; }
    public bool Visible { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new Polygon2D
    {
        Enabled = Enabled, Points = new List<Vector2>(Points), Width = Width, Color = Color, Filled = Filled, Visible = Visible, Owner = newOwner,
    };
}
