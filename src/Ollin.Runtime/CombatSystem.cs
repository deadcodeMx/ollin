using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Runtime;

/// <summary>Aplica daño de Hitbox contra Hurtbox usando las formas 2D.</summary>
internal sealed class CombatSystem
{
    public void Step(Scene scene, float deltaTime)
    {
        var hitboxes = new List<(Entity Entity, Hitbox Hitbox, Collider2D Collider)>();
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            if (entity.GetComponent<Hitbox>() is { Enabled: true } hitbox &&
                entity.GetComponent<Collider2D>() is { Enabled: true } collider)
                hitboxes.Add((entity, hitbox, collider));
        }
        if (hitboxes.Count == 0) return;

        foreach (var (source, hitbox, sourceCollider) in hitboxes)
        {
            if (!hitbox.CanHit) continue;
            foreach (var target in scene.Entities)
            {
                if (!target.Active || target.Id == source.Id) continue;
                if (target.GetComponent<Hurtbox>() is not { Enabled: true } hurtbox) continue;
                if (target.GetComponent<Health>() is not { IsDead: false } health) continue;
                if (string.Equals(hurtbox.Team, hitbox.Team, StringComparison.OrdinalIgnoreCase)) continue;
                if (target.GetComponent<Collider2D>() is not { Enabled: true } targetCollider) continue;
                if (!CollisionMath.TryGetPenetration(
                        sourceCollider, scene.GetWorldMatrix(source),
                        targetCollider, scene.GetWorldMatrix(target),
                        out var normal, out var depth) || depth <= 0.0001f) continue;

                health.TakeDamage(hitbox.Damage * MathF.Max(0f, hurtbox.DamageMultiplier));
                if (source.GetComponent<Knockback2D>() is { } knockback && target.GetComponent<Rigidbody2D>() is { } body)
                    body.Velocity += normal * knockback.Force + Vector2.UnitY * knockback.Upward;
                hitbox.TryConsume();
                break;
            }
        }
    }
}
