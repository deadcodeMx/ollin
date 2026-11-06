using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Runtime;

/// <summary>Resuelve uniones simples después de la física de contacto.</summary>
internal sealed class JointSystem
{
    public void Step(Scene scene, float deltaTime)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Joint2D>() is not { Enabled: true } joint) continue;
            var target = joint.FindTarget(scene);
            if (target is null || target.Id == entity.Id) continue;
            var body = entity.GetComponent<Rigidbody2D>();
            if (body is null || body.IsStatic) continue;

            var targetPosition = scene.GetWorldPosition(target) + joint.Anchor;
            var position = scene.GetWorldPosition(entity);
            var delta = targetPosition - position;

            switch (joint)
            {
                case SpringJoint2D spring:
                {
                    var direction = delta.LengthSquared() > 0.0001f ? Vector2.Normalize(delta) : Vector2.Zero;
                    var force = direction * ((delta.Length() - spring.RestLength) * spring.Stiffness) -
                                body.Velocity * spring.Damping;
                    body.Velocity += force * deltaTime;
                    break;
                }
                case PinJoint2D pin:
                {
                    if (delta.Length() > pin.Distance)
                        entity.Transform.Position += delta - Vector2.Normalize(delta) * pin.Distance;
                    break;
                }
                case DistanceJoint2D distanceJoint:
                {
                    var length = delta.Length();
                    if (length > 0.0001f)
                    {
                        var direction = delta / length;
                        entity.Transform.Position += direction * (length - distanceJoint.Distance);
                        var radial = Vector2.Dot(body.Velocity, direction);
                        body.Velocity -= direction * radial;
                    }
                    break;
                }
                case RopeJoint2D rope:
                {
                    var length = delta.Length();
                    if (length > rope.MaxDistance)
                    {
                        var direction = delta / length;
                        entity.Transform.Position += direction * (length - rope.MaxDistance);
                        var radial = Vector2.Dot(body.Velocity, direction);
                        if (radial < 0f) body.Velocity -= direction * radial;
                    }
                    break;
                }
            }
        }
    }
}
