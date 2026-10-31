using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Caja 3D para paredes, suelos, techos y puertas del modo 2.5D. Se coloca con el
/// transform (X = ancho, Y = profundidad) y su base descansa en el suelo.
/// </summary>
public sealed class Wall3D : Component
{
    public Vector3 Size { get; set; } = new(1f, 3f, 1f);

    public Vector3 Color { get; set; } = new(0.55f, 0.56f, 0.6f);

    public string? TexturePath { get; set; }

    public bool Visible { get; set; } = true;

    public bool Collides { get; set; } = true;

    [JsonIgnore]
    public Vector3 Center => new(
        Owner?.Transform.Position.X ?? 0f,
        Size.Y * 0.5f,
        Owner?.Transform.Position.Y ?? 0f);

    [JsonIgnore]
    public (Vector3 Min, Vector3 Max) Bounds
    {
        get
        {
            var center = Center;
            var half = Vector3.Abs(Size) * 0.5f;
            return (center - half, center + half);
        }
    }

    public override Component CloneFor(Entity newOwner) => new Wall3D
    {
        Enabled = Enabled,
        Size = Size,
        Color = Color,
        TexturePath = TexturePath,
        Visible = Visible,
        Collides = Collides,
        Owner = newOwner,
    };
}
