using System.Numerics;
using Ollin.Core.Scenes;

namespace Ollin.Core.Model.Components;

/// <summary>Polígono navegable definido en espacio local de la entidad.</summary>
public sealed class NavigationPolygon2D : Component
{
    public List<Vector2> Points { get; set; } = new()
    {
        new(-5f, -3f), new(5f, -3f), new(5f, 3f), new(-5f, 3f),
    };

    public IReadOnlyList<Vector2> WorldPoints(Scene scene)
    {
        if (Owner is null) return Points;
        var world = scene.GetWorldMatrix(Owner);
        return Points.Select(point => Vector2.Transform(point, world)).ToList();
    }

    public bool Contains(Scene scene, Vector2 worldPoint)
    {
        if (Owner is null) return false;
        var matrix = scene.GetWorldMatrix(Owner);
        if (!Matrix4x4.Invert(matrix, out var inverse)) return false;
        var local = Vector2.Transform(worldPoint, inverse);
        return NavigationGeometry.Contains(Points, local);
    }

    public override Component CloneFor(Entity newOwner) => new NavigationPolygon2D
    {
        Enabled = Enabled, Points = new List<Vector2>(Points), Owner = newOwner,
    };
}

/// <summary>Agente que sigue una ruta poligonal simple.</summary>
public sealed class NavigationAgent2D : Component, ITickable
{
    public Vector2 Target { get; set; }
    public float Speed { get; set; } = 3f;
    public float StopDistance { get; set; } = 0.1f;
    [System.Text.Json.Serialization.JsonIgnore] public List<Vector2> Path { get; private set; } = new();
    private int _index;

    public void SetTarget(Vector2 target)
    {
        Target = target;
        Path.Clear();
        _index = 0;
    }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        if (Path.Count == 0)
        {
            Path = NavigationGeometry.FindPath(scene, scene.GetWorldPosition(Owner), Target);
            _index = Path.Count > 1 ? 1 : 0;
        }
        if (_index >= Path.Count) return;
        var position = scene.GetWorldPosition(Owner);
        var delta = Path[_index] - position;
        if (delta.Length() <= StopDistance)
        {
            _index++;
            return;
        }
        var step = Vector2.Normalize(delta) * Speed * deltaTime;
        if (step.Length() > delta.Length()) step = delta;
        Owner.Transform.Position += step;
    }

    public override Component CloneFor(Entity newOwner) => new NavigationAgent2D
    {
        Enabled = Enabled, Target = Target, Speed = Speed, StopDistance = StopDistance, Owner = newOwner,
    };
}

/// <summary>Pathfinding por grafo de visibilidad sobre un polígono.</summary>
public static class NavigationGeometry
{
    public static List<Vector2> FindPath(Scene scene, Vector2 start, Vector2 end)
    {
        var polygon = scene.Entities
            .Where(entity => entity.Active && entity.GetComponent<NavigationPolygon2D>() is { Enabled: true })
            .Select(entity => entity.GetComponent<NavigationPolygon2D>()!)
            .FirstOrDefault(shape => shape.Contains(scene, start)) ??
            scene.Entities.Select(entity => entity.GetComponent<NavigationPolygon2D>()).FirstOrDefault(shape => shape is not null);
        if (polygon is null) return new List<Vector2> { start, end };
        var points = polygon.WorldPoints(scene).ToList();
        if (points.Count < 3) return new List<Vector2> { start, end };

        var nodes = new List<Vector2> { start, end };
        nodes.AddRange(points);
        var count = nodes.Count;
        var distances = Enumerable.Repeat(float.PositiveInfinity, count).ToArray();
        var previous = Enumerable.Repeat(-1, count).ToArray();
        var visited = new bool[count];
        distances[0] = 0f;

        for (var iteration = 0; iteration < count; iteration++)
        {
            var current = -1;
            for (var i = 0; i < count; i++)
                if (!visited[i] && (current < 0 || distances[i] < distances[current])) current = i;
            if (current < 0 || float.IsPositiveInfinity(distances[current])) break;
            if (current == 1) break;
            visited[current] = true;
            for (var next = 0; next < count; next++)
            {
                if (next == current || visited[next]) continue;
                if (!Visible(points, nodes[current], nodes[next])) continue;
                var distance = distances[current] + Vector2.Distance(nodes[current], nodes[next]);
                if (distance >= distances[next]) continue;
                distances[next] = distance;
                previous[next] = current;
            }
        }

        if (float.IsPositiveInfinity(distances[1])) return new List<Vector2> { start, end };
        var path = new List<Vector2>();
        for (var node = 1; node >= 0; node = previous[node])
        {
            path.Add(nodes[node]);
            if (node == 0) break;
        }
        path.Reverse();
        return path;
    }

    public static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        var inside = false;
        for (var i = 0; i < polygon.Count; i++)
        {
            var a = polygon[i];
            var b = polygon[(i + 1) % polygon.Count];
            if ((a.Y > point.Y) != (b.Y > point.Y) &&
                point.X < (b.X - a.X) * (point.Y - a.Y) / (b.Y - a.Y) + a.X)
                inside = !inside;
        }
        return inside;
    }

    private static bool Visible(IReadOnlyList<Vector2> polygon, Vector2 a, Vector2 b)
    {
        for (var i = 0; i < polygon.Count; i++)
        {
            var c = polygon[i];
            var d = polygon[(i + 1) % polygon.Count];
            if (DistanceToSegment(a, c, d) < 0.001f || DistanceToSegment(b, c, d) < 0.001f) continue;
            if (SegmentsIntersect(a, b, c, d)) return false;
        }
        return Contains(polygon, (a + b) * 0.5f);
    }

    private static bool SegmentsIntersect(Vector2 a, Vector2 b, Vector2 c, Vector2 d)
    {
        static float Cross(Vector2 p, Vector2 q, Vector2 r) => (q.X - p.X) * (r.Y - p.Y) - (q.Y - p.Y) * (r.X - p.X);
        var d1 = Cross(c, d, a);
        var d2 = Cross(c, d, b);
        var d3 = Cross(a, b, c);
        var d4 = Cross(a, b, d);
        return ((d1 > 0 && d2 < 0) || (d1 < 0 && d2 > 0)) && ((d3 > 0 && d4 < 0) || (d3 < 0 && d4 > 0));
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var length = ab.LengthSquared();
        if (length < 0.000001f) return Vector2.Distance(point, a);
        var t = System.Math.Clamp(Vector2.Dot(point - a, ab) / length, 0f, 1f);
        return Vector2.Distance(point, a + ab * t);
    }
}
