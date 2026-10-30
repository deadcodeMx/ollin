using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Caja de colisión alineada a los ejes, en espacio local de la entidad.</summary>
public sealed class BoxCollider : Component
{
    public Vector2 Size { get; set; } = Vector2.One;

    public Vector2 Offset { get; set; }

    /// <summary>Si es un disparador no bloquea el paso; solo notifica.</summary>
    public bool IsTrigger { get; set; }

    /// <summary>Dibuja la caja en el viewport (útil para depurar).</summary>
    public bool ShowInEditor { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new BoxCollider
    {
        Enabled = Enabled,
        Size = Size,
        Offset = Offset,
        IsTrigger = IsTrigger,
        ShowInEditor = ShowInEditor,
        Owner = newOwner,
    };

    /// <summary>Esquinas en mundo (TL, TR, BR, BL), sin rotación.</summary>
    public Vector2[] GetWorldCorners(Matrix4x4 world)
    {
        var half = Size * 0.5f;

        return new[]
        {
            Vector2.Transform(new Vector2(-half.X, half.Y) + Offset, world),
            Vector2.Transform(new Vector2(half.X, half.Y) + Offset, world),
            Vector2.Transform(new Vector2(half.X, -half.Y) + Offset, world),
            Vector2.Transform(new Vector2(-half.X, -half.Y) + Offset, world),
        };
    }
}


/// <summary>
/// Cuerpo con velocidad para física 2D simple: integración, gravedad y resolución
/// contra colisiones estáticas por ejes separados.
/// </summary>
public sealed class Rigidbody2D : Component, ITickable
{
    public Vector2 Velocity { get; set; }

    public bool AffectedByGravity { get; set; }

    public float GravityScale { get; set; } = 1f;

    /// <summary>Los cuerpos estáticos no se mueven, pero bloquean a los demás.</summary>
    public bool IsStatic { get; set; }

    public float Mass { get; set; } = 1f;

    public override Component CloneFor(Entity newOwner) => new Rigidbody2D
    {
        Enabled = Enabled,
        Velocity = Velocity,
        AffectedByGravity = AffectedByGravity,
        GravityScale = GravityScale,
        IsStatic = IsStatic,
        Mass = Mass,
        Owner = newOwner,
    };

    public void OnUpdate(float deltaTime)
    {
        if (IsStatic || Owner is null)
        {
            return;
        }

        var velocity = Velocity;
        if (AffectedByGravity)
        {
            velocity.Y -= 9.8f * GravityScale * deltaTime;
        }

        Velocity = velocity;
    }
}
