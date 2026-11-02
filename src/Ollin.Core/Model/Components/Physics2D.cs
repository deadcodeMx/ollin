using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Base común de todas las formas de colisión 2D.</summary>
public abstract class Collider2D : Component
{
    public Vector2 Offset { get; set; }

    /// <summary>Si es disparador no bloquea el paso; solo notifica.</summary>
    public bool IsTrigger { get; set; }

    public bool ShowInEditor { get; set; } = true;

    /// <summary>Esquinas en mundo. La física usa su caja envolvente.</summary>
    public abstract Vector2[] GetWorldCorners(Matrix4x4 world);
}

/// <summary>Caja de colisión alineada a los ejes, en espacio local de la entidad.</summary>
public sealed class BoxCollider : Collider2D
{
    public Vector2 Size { get; set; } = Vector2.One;

    public override Component CloneFor(Entity newOwner) => new BoxCollider
    {
        Enabled = Enabled,
        Size = Size,
        Offset = Offset,
        IsTrigger = IsTrigger,
        ShowInEditor = ShowInEditor,
        Owner = newOwner,
    };

    public override Vector2[] GetWorldCorners(Matrix4x4 world)
    {
        var half = Size * 0.5f;
        var radius = MathF.Max(0.01f, MathF.Min(half.X, half.Y));
        var topY = half.Y - radius;
        var bottomY = -half.Y + radius;
        const int segments = 12;
        var points = new List<Vector2>(segments * 4);

        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI * i / segments;
            points.Add(new Vector2(MathF.Cos(angle) * radius, topY + MathF.Sin(angle) * radius) + Offset);
        }
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI + MathF.PI * i / segments;
            points.Add(new Vector2(MathF.Cos(angle) * radius, bottomY + MathF.Sin(angle) * radius) + Offset);
        }

        return points.Select(point => Vector2.Transform(point, world)).ToArray();
    }
}

/// <summary>Colisión circular exacta.</summary>
public sealed class CircleCollider2D : Collider2D
{
    public float Radius { get; set; } = 0.5f;

    public override Component CloneFor(Entity newOwner) => new CircleCollider2D
    {
        Enabled = Enabled,
        Radius = Radius,
        Offset = Offset,
        IsTrigger = IsTrigger,
        ShowInEditor = ShowInEditor,
        Owner = newOwner,
    };

    public override Vector2[] GetWorldCorners(Matrix4x4 world)
    {
        var r = MathF.Max(0.01f, Radius);
        const int segments = 32;
        var points = new Vector2[segments];
        for (var i = 0; i < segments; i++)
        {
            var angle = MathF.Tau * i / segments;
            points[i] = Vector2.Transform(new Vector2(MathF.Cos(angle) * r, MathF.Sin(angle) * r) + Offset, world);
        }
        return points;
    }
}

/// <summary>Cápsula 2D representada por un contorno convexo.</summary>
public sealed class CapsuleCollider2D : Collider2D
{
    public Vector2 Size { get; set; } = new(0.7f, 1.4f);

    public override Component CloneFor(Entity newOwner) => new CapsuleCollider2D
    {
        Enabled = Enabled,
        Size = Size,
        Offset = Offset,
        IsTrigger = IsTrigger,
        ShowInEditor = ShowInEditor,
        Owner = newOwner,
    };

    public override Vector2[] GetWorldCorners(Matrix4x4 world)
    {
        var half = Size * 0.5f;
        var radius = MathF.Max(0.01f, MathF.Min(half.X, half.Y));
        var topY = half.Y - radius;
        var bottomY = -half.Y + radius;
        const int segments = 12;
        var points = new List<Vector2>(segments * 4);

        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI * i / segments;
            points.Add(new Vector2(MathF.Cos(angle) * radius, topY + MathF.Sin(angle) * radius) + Offset);
        }
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI + MathF.PI * i / segments;
            points.Add(new Vector2(MathF.Cos(angle) * radius, bottomY + MathF.Sin(angle) * radius) + Offset);
        }

        return points.Select(point => Vector2.Transform(point, world)).ToArray();
    }
}

/// <summary>Polígono de colisión. La física usa su caja envolvente.</summary>
public sealed class PolygonCollider2D : Collider2D
{
    public List<Vector2> Points { get; set; } = new()
    {
        new(-0.5f, 0.5f), new(0.5f, 0.5f), new(0.5f, -0.5f), new(-0.5f, -0.5f),
    };

    public override Component CloneFor(Entity newOwner) => new PolygonCollider2D
    {
        Enabled = Enabled,
        Points = new List<Vector2>(Points),
        Offset = Offset,
        IsTrigger = IsTrigger,
        ShowInEditor = ShowInEditor,
        Owner = newOwner,
    };

    public override Vector2[] GetWorldCorners(Matrix4x4 world) =>
        Points.Select(point => Vector2.Transform(point + Offset, world)).ToArray();
}

/// <summary>Zona de detección: no bloquea, solo notifica solapamientos.</summary>
public sealed class Area2D : Collider2D
{
    public Vector2 Size { get; set; } = new(2f, 2f);

    public Area2D() => IsTrigger = true;

    public override Component CloneFor(Entity newOwner) => new Area2D
    {
        Enabled = Enabled,
        Size = Size,
        Offset = Offset,
        IsTrigger = true,
        ShowInEditor = ShowInEditor,
        Owner = newOwner,
    };

    public override Vector2[] GetWorldCorners(Matrix4x4 world)
    {
        var half = Size * 0.5f;
        var radius = MathF.Max(0.01f, MathF.Min(half.X, half.Y));
        var topY = half.Y - radius;
        var bottomY = -half.Y + radius;
        const int segments = 12;
        var points = new List<Vector2>(segments * 4);

        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI * i / segments;
            points.Add(new Vector2(MathF.Cos(angle) * radius, topY + MathF.Sin(angle) * radius) + Offset);
        }
        for (var i = 0; i <= segments; i++)
        {
            var angle = MathF.PI + MathF.PI * i / segments;
            points.Add(new Vector2(MathF.Cos(angle) * radius, bottomY + MathF.Sin(angle) * radius) + Offset);
        }

        return points.Select(point => Vector2.Transform(point, world)).ToArray();
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

    public bool IsStatic { get; set; }

    public float Mass { get; set; } = 1f;

    public bool IsKinematic { get; set; }

    public override Component CloneFor(Entity newOwner) => new Rigidbody2D
    {
        Enabled = Enabled,
        Velocity = Velocity,
        AffectedByGravity = AffectedByGravity,
        GravityScale = GravityScale,
        IsStatic = IsStatic,
        Mass = Mass,
        IsKinematic = IsKinematic,
        Owner = newOwner,
    };

    public void OnUpdate(float deltaTime)
    {
        if (IsStatic || Owner is null) return;
        var velocity = Velocity;
        if (AffectedByGravity) velocity.Y -= 9.8f * GravityScale * deltaTime;
        Velocity = velocity;
    }
}
