using System.Numerics;
using Ollin.Core.Model.Components;

namespace Ollin.Runtime;

/// <summary>
/// Penetración entre formas 2D. Cajas, áreas, cápsulas y polígonos son
/// convexos; el círculo se resuelve contra polígonos y contra otro círculo.
/// </summary>
internal static class CollisionMath
{
    public static bool TryGetPenetration(
        Collider2D a, Matrix4x4 worldA,
        Collider2D b, Matrix4x4 worldB,
        out Vector2 normal, out float depth)
    {
        normal = Vector2.Zero;
        depth = 0f;

        var circleA = a as CircleCollider2D;
        var circleB = b as CircleCollider2D;

        if (circleA is not null && circleB is not null)
        {
            var centerA = Vector2.Transform(circleA.Offset, worldA);
            var centerB = Vector2.Transform(circleB.Offset, worldB);
            var delta = centerA - centerB;
            var distance = delta.Length();
            var radii = MathF.Max(0.01f, circleA.Radius) + MathF.Max(0.01f, circleB.Radius);
            if (distance >= radii) return false;
            normal = distance < 0.0001f ? Vector2.UnitY : delta / distance;
            depth = radii - distance;
            return true;
        }

        if (circleA is not null)
        {
            var center = Vector2.Transform(circleA.Offset, worldA);
            var points = Polygon(b, worldB);
            if (!CirclePolygon(center, MathF.Max(0.01f, circleA.Radius), points, fromFirst: true, out normal, out depth)) return false;
            return true;
        }

        if (circleB is not null)
        {
            var center = Vector2.Transform(circleB.Offset, worldB);
            var points = Polygon(a, worldA);
            if (!CirclePolygon(center, MathF.Max(0.01f, circleB.Radius), points, fromFirst: false, out normal, out depth)) return false;
            return true;
        }

        var polyA = Polygon(a, worldA);
        var polyB = Polygon(b, worldB);
        return PolygonPolygon(polyA, polyB, out normal, out depth);
    }

    private static bool CirclePolygon(Vector2 center, float radius, IReadOnlyList<Vector2> polygon,
        bool fromFirst, out Vector2 normal, out float depth)
    {
        normal = Vector2.Zero;
        depth = 0f;
        if (polygon.Count < 3) return false;

        var inside = true;
        var bestDistance = float.MaxValue;
        var bestPoint = Vector2.Zero;

        for (var i = 0; i < polygon.Count; i++)
        {
            var p = polygon[i];
            var q = polygon[(i + 1) % polygon.Count];
            var edge = q - p;
            var t = Vector2.Dot(center - p, edge) / MathF.Max(0.0001f, edge.LengthSquared());
            t = Math.Clamp(t, 0f, 1f);
            var closest = p + edge * t;
            var distance = Vector2.Distance(center, closest);
            if (distance < bestDistance)
            {
                bestDistance = distance;
                bestPoint = closest;
            }

            if (Cross(edge, center - p) < 0f) inside = false;
        }

        if (!inside && bestDistance >= radius) return false;

        if (inside)
        {
            var nearest = bestPoint;
            normal = bestDistance < 0.0001f ? Vector2.UnitY : Vector2.Normalize(center - nearest);
            depth = radius + bestDistance;
            if (!fromFirst) normal = -normal;
            return true;
        }

        normal = bestDistance < 0.0001f ? Vector2.UnitY : Vector2.Normalize(center - bestPoint);
        depth = radius - bestDistance;
        if (!fromFirst) normal = -normal;
        return depth > 0f;
    }

    private static bool PolygonPolygon(IReadOnlyList<Vector2> a, IReadOnlyList<Vector2> b,
        out Vector2 normal, out float depth)
    {
        normal = Vector2.Zero;
        depth = float.MaxValue;
        if (a.Count < 3 || b.Count < 3) { depth = 0f; return false; }

        foreach (var polygon in new[] { a, b })
        {
            for (var i = 0; i < polygon.Count; i++)
            {
                var edge = polygon[(i + 1) % polygon.Count] - polygon[i];
                if (edge.LengthSquared() < 0.0001f) continue;
                var axis = Vector2.Normalize(new Vector2(-edge.Y, edge.X));
                Project(a, axis, out var minA, out var maxA);
                Project(b, axis, out var minB, out var maxB);
                var overlap = MathF.Min(maxA, maxB) - MathF.Max(minA, minB);
                if (overlap <= 0f) { depth = 0f; return false; }
                if (overlap < depth)
                {
                    depth = overlap;
                    normal = axis * (Center(a).X < Center(b).X ? -1f : 1f);
                    var centerDelta = Center(a) - Center(b);
                    if (Vector2.Dot(normal, centerDelta) < 0f) normal = -normal;
                }
            }
        }

        return depth > 0f && float.IsFinite(depth);
    }

    private static IReadOnlyList<Vector2> Polygon(Collider2D collider, Matrix4x4 world)
    {
        if (collider is PolygonCollider2D polygon)
            return polygon.Points.Select(point => Vector2.Transform(point + polygon.Offset, world)).ToArray();

        return collider.GetWorldCorners(world);
    }

    private static void Project(IReadOnlyList<Vector2> points, Vector2 axis, out float min, out float max)
    {
        min = float.MaxValue; max = float.MinValue;
        foreach (var point in points)
        {
            var value = Vector2.Dot(point, axis);
            min = MathF.Min(min, value);
            max = MathF.Max(max, value);
        }
    }

    private static Vector2 Center(IReadOnlyList<Vector2> points)
    {
        var sum = Vector2.Zero;
        foreach (var point in points) sum += point;
        return sum / MathF.Max(1, points.Count);
    }

    private static float Cross(Vector2 a, Vector2 b) => a.X * b.Y - a.Y * b.X;
}
