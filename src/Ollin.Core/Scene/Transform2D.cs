using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model;

/// <summary>
/// Transformación local 2D: posición en unidades de mundo, rotación en radianes
/// y escala. La matriz local usa convención de vectores fila (v * M), igual que
/// System.Numerics.
/// </summary>
public sealed class Transform2D
{
    public Vector2 Position { get; set; } = Vector2.Zero;

    /// <summary>Rotación en radianes (sentido antihorario en un eje Y hacia arriba).</summary>
    public float Rotation { get; set; }

    public Vector2 Scale { get; set; } = Vector2.One;

    [JsonIgnore]
    public Matrix4x4 LocalMatrix =>
        Matrix4x4.CreateScale(Scale.X, Scale.Y, 1f)
        * Matrix4x4.CreateRotationZ(Rotation)
        * Matrix4x4.CreateTranslation(Position.X, Position.Y, 0f);

    public Transform2D Clone() => new()
    {
        Position = Position,
        Rotation = Rotation,
        Scale = Scale,
    };
}
