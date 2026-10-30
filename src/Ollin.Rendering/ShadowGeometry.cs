using System.Numerics;

namespace Ollin.Rendering;

/// <summary>Geometría de sombras 2D: proyecta una arista lejos de la luz.</summary>
public static class ShadowGeometry
{
    /// <summary>Proyecta una arista (a, b) desde el centro de la luz hasta el radio.</summary>
    public static (Vector2 A, Vector2 B, Vector2 Bp, Vector2 Ap) ProjectEdge(
        Vector2 center, Vector2 a, Vector2 b, float radius)
    {
        var directionA = a - center;
        var directionB = b - center;

        var lengthA = directionA.Length();
        var lengthB = directionB.Length();

        var normalizedA = lengthA < 0.0001f ? Vector2.UnitY : directionA / lengthA;
        var normalizedB = lengthB < 0.0001f ? Vector2.UnitY : directionB / lengthB;

        return (a, b, normalizedB * radius, normalizedA * radius);
    }

    /// <summary>Distancia mínima de un punto a una arista (para descartar sombras lejanas).</summary>
    public static float DistanceToEdge(Vector2 center, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();

        if (lengthSquared < 0.000001f)
        {
            return Vector2.Distance(center, a);
        }

        var t = Math.Clamp(Vector2.Dot(center - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(center, a + ab * t);
    }
}