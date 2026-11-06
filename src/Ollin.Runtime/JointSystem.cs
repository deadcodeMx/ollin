using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Runtime;

/// <summary>
/// Solver iterativo de uniones: aplica impulsos de velocidad y corrección de
/// posición para mantener distancias estables sin teletransportar los cuerpos.
/// </summary>
internal sealed class JointSystem
{
    private const int Iterations = 8;
    private const float PositionCorrection = 0.8f;

    public void Step(Scene scene, float deltaTime)
    {
        if (deltaTime <= 0f) return;

        var joints = new List<(Entity Entity, Entity Target, Joint2D Joint, Rigidbody2D Body)>();
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Joint2D>() is not { Enabled: true } joint) continue;
            var target = joint.FindTarget(scene);
            if (target is null || target.Id == entity.Id) continue;
            var body = entity.GetComponent<Rigidbody2D>();
            if (body is null || body.IsStatic) continue;
            joints.Add((entity, target, joint, body));
        }

        // Los muelles aplican fuerza una sola vez por paso.
        foreach (var (entity, target, joint, body) in joints)
        {
            if (joint is not SpringJoint2D spring) continue;
            var targetPosition = scene.GetWorldPosition(target) + spring.Anchor;
            var delta = targetPosition - scene.GetWorldPosition(entity);
            var direction = delta.LengthSquared() > 0.0001f ? Vector2.Normalize(delta) : Vector2.Zero;
            var force = direction * ((delta.Length() - spring.RestLength) * spring.Stiffness) -
                        body.Velocity * spring.Damping;
            body.Velocity += force * deltaTime;
        }

        for (var iteration = 0; iteration < Iterations; iteration++)
        {
            foreach (var (entity, target, joint, body) in joints)
            {
                var targetPosition = scene.GetWorldPosition(target) + joint.Anchor;
                var position = scene.GetWorldPosition(entity);
                var delta = targetPosition - position;
                var distance = delta.Length();

                switch (joint)
                {
                    case DistanceJoint2D fixedDistance:
                        SolveDistance(scene, entity, target, body, delta, distance, fixedDistance.Distance, deltaTime);
                        break;

                    case RopeJoint2D rope when distance > rope.MaxDistance:
                        SolveDistance(scene, entity, target, body, delta, distance, rope.MaxDistance, deltaTime);
                        break;

                    case PinJoint2D pin when distance > pin.Distance:
                        SolveDistance(scene, entity, target, body, delta, distance, pin.Distance, deltaTime);
                        break;
                }
            }
        }
    }

    private static void SolveDistance(
        Scene scene,
        Entity entity,
        Entity target,
        Rigidbody2D body,
        Vector2 delta,
        float distance,
        float restLength,
        float deltaTime)
    {
        if (distance < 0.0001f) return;

        var direction = delta / distance;
        var targetBody = target.GetComponent<Rigidbody2D>();
        var inverseA = body.IsStatic || body.IsKinematic ? 0f : 1f / MathF.Max(0.0001f, body.Mass);
        var inverseB = targetBody is { IsStatic: false, IsKinematic: false }
            ? 1f / MathF.Max(0.0001f, targetBody.Mass)
            : 0f;
        var inverseMass = inverseA + inverseB;
        if (inverseMass <= 0f) return;

        var targetVelocity = targetBody is { IsStatic: false } ? targetBody.Velocity : Vector2.Zero;
        var relativeVelocity = Vector2.Dot(body.Velocity - targetVelocity, direction);
        var error = distance - restLength;
        var lambda = -(relativeVelocity + PositionCorrection / deltaTime * error) / inverseMass;

        body.Velocity += direction * (lambda * inverseA);
        if (targetBody is { IsStatic: false, IsKinematic: false })
            targetBody.Velocity -= direction * (lambda * inverseB);

        var correction = direction * error;
        entity.Transform.Position += correction * (inverseA / inverseMass);
        if (targetBody is { IsStatic: false, IsKinematic: false })
            target.Transform.Position -= correction * (inverseB / inverseMass);
    }
}
