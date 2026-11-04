using System.Numerics;

namespace Ollin.Rendering;

/// <summary>Triangulación por ear clipping para relleno de polígonos 2D.</summary>
public static class PolygonTriangulator
{
    public static List<(Vector2 A, Vector2 B, Vector2 C)> Triangulate(IReadOnlyList<Vector2> points)
    {
        var triangles = new List<(Vector2, Vector2, Vector2)>();
        if (points.Count < 3) return triangles;
        var indices = Enumerable.Range(0, points.Count).ToList();
        var ccw = SignedArea(points) > 0f;
        var guard = 0;
        while (indices.Count > 3 && guard++ < points.Count * 3)
        {
            var clipped = false;
            for (var i = 0; i < indices.Count; i++)
            {
                var previous = indices[(i - 1 + indices.Count) % indices.Count];
                var current = indices[i];
                var next = indices[(i + 1) % indices.Count];
                var a = points[previous];
                var b = points[current];
                var c = points[next];
                if ((ccw && Cross(a, b, c) <= 0f) || (!ccw && Cross(a, b, c) >= 0f)) continue;
                var contains = false;
                foreach (var index in indices)
                {
                    if (index == previous || index == current || index == next) continue;
                    if (Inside(a, b, c, points[index], ccw)) { contains = true; break; }
                }
                if (contains) continue;
                triangles.Add((a, b, c));
                indices.RemoveAt(i);
                clipped = true;
                break;
            }
            if (!clipped) break;
        }
        if (indices.Count == 3) triangles.Add((points[indices[0]], points[indices[1]], points[indices[2]]));
        return triangles;
    }

    private static float SignedArea(IReadOnlyList<Vector2> points)
    {
        var area = 0f;
        for (var i = 0; i < points.Count; i++)
        {
            var a = points[i];
            var b = points[(i + 1) % points.Count];
            area += a.X * b.Y - b.X * a.Y;
        }
        return area * 0.5f;
    }

    private static float Cross(Vector2 a, Vector2 b, Vector2 c) =>
        (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

    private static bool Inside(Vector2 a, Vector2 b, Vector2 c, Vector2 point, bool ccw)
    {
        var d1 = Cross(a, b, point);
        var d2 = Cross(b, c, point);
        var d3 = Cross(c, a, point);
        return ccw ? d1 >= 0f && d2 >= 0f && d3 >= 0f : d1 <= 0f && d2 <= 0f && d3 <= 0f;
    }
}
