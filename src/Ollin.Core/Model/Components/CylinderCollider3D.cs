using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Colisión cilíndrica para pilares y columnas en el modo 2.5D.</summary>
public sealed class CylinderCollider3D : Component
{
    public float Radius { get; set; } = 0.5f;

    public float Height { get; set; } = 2.8f;

    public float BaseY { get; set; }

    public bool Collides { get; set; } = true;

    public bool ContainsHeight(float y) => y >= BaseY && y <= BaseY + Height;

    public override Component CloneFor(Entity newOwner) => new CylinderCollider3D
    {
        Enabled = Enabled,
        Radius = Radius,
        Height = Height,
        BaseY = BaseY,
        Collides = Collides,
        Owner = newOwner,
    };
}

public static class CylinderCollision
{
    /// <summary>Empuja una posición fuera de todos los cilindros sólidos.</summary>
    public static Vector2 Resolve(Scenes.Scene scene, Vector2 position, float agentRadius, float height)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active ||
                entity.GetComponent<CylinderCollider3D>() is not { Enabled: true, Collides: true } cylinder ||
                !cylinder.ContainsHeight(height))
            {
                continue;
            }

            var center = entity.Transform.Position;
            var delta = position - center;
            var minimum = MathF.Max(0.05f, cylinder.Radius) + MathF.Max(0.05f, agentRadius);
            var distance = delta.Length();

            if (distance >= minimum)
            {
                continue;
            }

            if (distance < 0.0001f)
            {
                position = center + new Vector2(minimum, 0f);
                continue;
            }

            position = center + delta / distance * minimum;
        }

        return position;
    }
}
