using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Base de una unión entre la entidad propietaria y otra entidad.</summary>
public abstract class Joint2D : Component
{
    public Guid TargetId { get; set; }
    public string TargetTag { get; set; } = string.Empty;
    public Vector2 Anchor { get; set; }

    public Entity? FindTarget(Scenes.Scene scene)
    {
        if (TargetId != Guid.Empty) return scene.Find(TargetId);
        if (string.IsNullOrWhiteSpace(TargetTag)) return null;
        return scene.Entities.FirstOrDefault(entity => entity.Active &&
            entity.Tags.Any(tag => tag.Equals(TargetTag, StringComparison.OrdinalIgnoreCase)));
    }
}

/// <summary>Unión rígida que mantiene una distancia fija entre dos cuerpos.</summary>
public sealed class DistanceJoint2D : Joint2D
{
    public float Distance { get; set; } = 1f;

    public override Component CloneFor(Entity newOwner) => new DistanceJoint2D
    {
        Enabled = Enabled, TargetId = TargetId, TargetTag = TargetTag, Anchor = Anchor,
        Distance = Distance, Owner = newOwner,
    };
}

/// <summary>Cuerda: limita la distancia máxima sin empujar cuando están más cerca.</summary>
public sealed class RopeJoint2D : Joint2D
{
    public float MaxDistance { get; set; } = 2f;

    public override Component CloneFor(Entity newOwner) => new RopeJoint2D
    {
        Enabled = Enabled, TargetId = TargetId, TargetTag = TargetTag, Anchor = Anchor,
        MaxDistance = MaxDistance, Owner = newOwner,
    };
}

/// <summary>Unión de distancia con muelle.</summary>
public sealed class SpringJoint2D : Joint2D
{
    public float RestLength { get; set; } = 1f;
    public float Stiffness { get; set; } = 20f;
    public float Damping { get; set; } = 2f;

    public override Component CloneFor(Entity newOwner) => new SpringJoint2D
    {
        Enabled = Enabled, TargetId = TargetId, TargetTag = TargetTag, Anchor = Anchor,
        RestLength = RestLength, Stiffness = Stiffness, Damping = Damping, Owner = newOwner,
    };
}

/// <summary>Unión rígida simple que mantiene la distancia actual.</summary>
public sealed class PinJoint2D : Joint2D
{
    public float Distance { get; set; } = 1f;

    public override Component CloneFor(Entity newOwner) => new PinJoint2D
    {
        Enabled = Enabled, TargetId = TargetId, TargetTag = TargetTag, Anchor = Anchor,
        Distance = Distance, Owner = newOwner,
    };
}
