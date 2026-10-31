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

    /// <summary>Unidades de mundo por repetición de la textura.</summary>
    public float TextureScale { get; set; } = 1f;

    public bool Visible { get; set; } = true;

    public bool Collides { get; set; } = true;

    /// <summary>Altura de la base de la caja (para techos y plataformas).</summary>
    public float BaseY { get; set; }

    /// <summary>Giro alrededor del eje Y en radianes.</summary>
    public float Yaw { get; set; }

    [JsonIgnore]
    public Vector3 Center => new(
        Owner?.Transform.Position.X ?? 0f,
        BaseY + Size.Y * 0.5f,
        Owner?.Transform.Position.Y ?? 0f);

    [JsonIgnore]
    public (Vector3 Min, Vector3 Max) Bounds
    {
        get
        {
            var center = Center;
            var half = Vector3.Abs(Size) * 0.5f;
            var cos = MathF.Abs(MathF.Cos(Yaw));
            var sin = MathF.Abs(MathF.Sin(Yaw));
            var extentX = cos * half.X + sin * half.Z;
            var extentZ = sin * half.X + cos * half.Z;
            return (
                new Vector3(center.X - extentX, center.Y - half.Y, center.Z - extentZ),
                new Vector3(center.X + extentX, center.Y + half.Y, center.Z + extentZ));
        }
    }

    public override Component CloneFor(Entity newOwner) => new Wall3D
    {
        Enabled = Enabled,
        Size = Size,
        Color = Color,
        TexturePath = TexturePath,
        TextureScale = TextureScale,
        BaseY = BaseY,
        Yaw = Yaw,
        Collides = Collides,
        Owner = newOwner,
    };
}
