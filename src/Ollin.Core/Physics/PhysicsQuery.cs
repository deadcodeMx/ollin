using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Physics;

public readonly record struct RaycastHit2D(Entity Entity, Vector2 Point, Vector2 Normal, float Distance);

/// <summary>Consultas físicas 2D por AABB, independientes del motor de render.</summary>
public static class PhysicsQuery
{
    public static RaycastHit2D? Raycast(
        Scene scene,
        Vector2 origin,
        Vector2 direction,
        float distance,
        int layerMask = ~0,
        Entity? ignore = null,
        bool includeTriggers = false,
        Func<Entity, bool>? filter = null)
    {
        if (distance <= 0f || direction.LengthSquared() < 0.000001f) return null;
        direction = Vector2.Normalize(direction);

        RaycastHit2D? best = null;
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || ReferenceEquals(entity, ignore)) continue;
            if (entity.GetComponent<Collider2D>() is not { Enabled: true } collider) continue;
            if (!includeTriggers && collider.IsTrigger) continue;
            if ((layerMask & (1 << System.Math.Clamp(collider.Layer, 0, 31))) == 0) continue;
            if (filter is not null && !filter(entity)) continue;

            var corners = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
            if (corners.Length == 0) continue;
            var min = corners[0];
            var max = corners[0];
            foreach (var corner in corners) { min = Vector2.Min(min, corner); max = Vector2.Max(max, corner); }

            if (TryRayAabb(origin, direction, distance, min, max, out var hitDistance, out var normal))
            {
                if (best is null || hitDistance < best.Value.Distance)
                    best = new RaycastHit2D(entity, origin + direction * hitDistance, normal, hitDistance);
            }
        }
        return best;
    }

    public static RaycastHit2D? BoxCast(
        Scene scene,
        Vector2 center,
        Vector2 size,
        Vector2 direction,
        float distance,
        int layerMask = ~0,
        Entity? ignore = null,
        bool includeTriggers = false)
    {
        if (distance <= 0f || direction.LengthSquared() < 0.000001f) return null;
        direction = Vector2.Normalize(direction);
        var half = Vector2.Abs(size) * 0.5f;

        RaycastHit2D? best = null;
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || ReferenceEquals(entity, ignore)) continue;
            if (entity.GetComponent<Collider2D>() is not { Enabled: true } collider) continue;
            if (!includeTriggers && collider.IsTrigger) continue;
            if ((layerMask & (1 << System.Math.Clamp(collider.Layer, 0, 31))) == 0) continue;

            var corners = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
            if (corners.Length == 0) continue;
            var min = corners[0];
            var max = corners[0];
            foreach (var corner in corners) { min = Vector2.Min(min, corner); max = Vector2.Max(max, corner); }

            // Minkowski: expande el destino por el tamaño de la caja que se desplaza.
            var expandedMin = min - half;
            var expandedMax = max + half;
            if (TryRayAabb(center, direction, distance, expandedMin, expandedMax, out var hitDistance, out var normal))
            {
                if (best is null || hitDistance < best.Value.Distance)
                    best = new RaycastHit2D(entity, center + direction * hitDistance, normal, hitDistance);
            }
        }
        return best;
    }

    private static bool TryRayAabb(Vector2 origin, Vector2 direction, float distance,
        Vector2 min, Vector2 max, out float hitDistance, out Vector2 normal)
    {
        hitDistance = 0f;
        normal = Vector2.Zero;
        var tMin = 0f;
        var tMax = distance;
        Vector2 hitNormal = Vector2.Zero;

        for (var axis = 0; axis < 2; axis++)
        {
            var o = axis == 0 ? origin.X : origin.Y;
            var d = axis == 0 ? direction.X : direction.Y;
            var lo = axis == 0 ? min.X : min.Y;
            var hi = axis == 0 ? max.X : max.Y;

            if (MathF.Abs(d) < 0.000001f)
            {
                if (o < lo || o > hi) return false;
                continue;
            }

            var t1 = (lo - o) / d;
            var t2 = (hi - o) / d;
            var sign = -1f;
            if (t1 > t2) { (t1, t2) = (t2, t1); sign = 1f; }
            if (t1 > tMin) { tMin = t1; hitNormal = axis == 0 ? new Vector2(sign, 0f) : new Vector2(0f, sign); }
            tMax = MathF.Min(tMax, t2);
            if (tMin > tMax) return false;
        }

        hitDistance = tMin;
        normal = hitNormal;
        return tMin >= 0f && tMin <= distance;
    }
}
