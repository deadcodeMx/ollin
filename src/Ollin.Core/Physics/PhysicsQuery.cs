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

    public static IReadOnlyList<RaycastHit2D> RaycastAll(
        Scene scene,
        Vector2 origin,
        Vector2 direction,
        float distance,
        int layerMask = ~0,
        Entity? ignore = null,
        bool includeTriggers = false)
    {
        if (distance <= 0f || direction.LengthSquared() < 0.000001f) return Array.Empty<RaycastHit2D>();
        direction = Vector2.Normalize(direction);
        var hits = new List<RaycastHit2D>();

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || ReferenceEquals(entity, ignore)) continue;
            if (entity.GetComponent<Collider2D>() is not { Enabled: true } collider) continue;
            if (!includeTriggers && collider.IsTrigger) continue;
            if ((layerMask & (1 << System.Math.Clamp(collider.Layer, 0, 31))) == 0) continue;

            var (min, max) = Bounds(scene, entity, collider);
            if (TryRayAabb(origin, direction, distance, min, max, out var hitDistance, out var normal))
                hits.Add(new RaycastHit2D(entity, origin + direction * hitDistance, normal, hitDistance));
        }

        hits.Sort((a, b) => a.Distance.CompareTo(b.Distance));
        return hits;
    }

    public static IReadOnlyList<Entity> OverlapCircle(
        Scene scene,
        Vector2 center,
        float radius,
        int layerMask = ~0,
        bool includeTriggers = false)
    {
        if (radius <= 0f) return Array.Empty<Entity>();
        var hits = new List<Entity>();

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            if (entity.GetComponent<Collider2D>() is not { Enabled: true } collider) continue;
            if (!includeTriggers && collider.IsTrigger) continue;
            if ((layerMask & (1 << System.Math.Clamp(collider.Layer, 0, 31))) == 0) continue;

            var corners = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
            if (CircleTouchesPolygon(center, radius, corners)) hits.Add(entity);
        }

        return hits;
    }

    public static IReadOnlyList<Entity> OverlapBox(
        Scene scene,
        Vector2 center,
        Vector2 size,
        int layerMask = ~0,
        bool includeTriggers = false)
    {
        var half = Vector2.Abs(size) * 0.5f;
        var minQuery = center - half;
        var maxQuery = center + half;
        var hits = new List<Entity>();

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            if (entity.GetComponent<Collider2D>() is not { Enabled: true } collider) continue;
            if (!includeTriggers && collider.IsTrigger) continue;
            if ((layerMask & (1 << System.Math.Clamp(collider.Layer, 0, 31))) == 0) continue;

            var (min, max) = Bounds(scene, entity, collider);
            if (minQuery.X <= max.X && maxQuery.X >= min.X &&
                minQuery.Y <= max.Y && maxQuery.Y >= min.Y) hits.Add(entity);
        }

        return hits;
    }

    /// <summary>Barrido de una caja; alias explícito de BoxCast.</summary>
    public static RaycastHit2D? ShapeCast(
        Scene scene,
        Vector2 center,
        Vector2 size,
        Vector2 direction,
        float distance,
        int layerMask = ~0,
        Entity? ignore = null,
        bool includeTriggers = false) =>
        BoxCast(scene, center, size, direction, distance, layerMask, ignore, includeTriggers);

    private static (Vector2 Min, Vector2 Max) Bounds(Scene scene, Entity entity, Collider2D collider)
    {
        var corners = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
        var min = new Vector2(float.MaxValue);
        var max = new Vector2(float.MinValue);
        foreach (var corner in corners) { min = Vector2.Min(min, corner); max = Vector2.Max(max, corner); }
        return (min, max);
    }

    private static bool CircleTouchesPolygon(Vector2 center, float radius, IReadOnlyList<Vector2> polygon)
    {
        if (polygon.Count == 0) return false;
        if (polygon.Count == 1) return Vector2.Distance(center, polygon[0]) <= radius;

        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if (DistanceToSegment(center, a, b) <= radius) return true;
        }

        return PointInPolygon(center, polygon);
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var edge = b - a;
        var lengthSquared = edge.LengthSquared();
        if (lengthSquared < 0.000001f) return Vector2.Distance(point, a);
        var t = System.Math.Clamp(Vector2.Dot(point - a, edge) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, a + edge * t);
    }

    private static bool PointInPolygon(Vector2 point, IReadOnlyList<Vector2> polygon)
    {
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
            {
                inside = !inside;
            }
        }

        return inside;
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
