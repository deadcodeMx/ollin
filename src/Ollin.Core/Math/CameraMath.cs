using System.Numerics;

namespace Ollin.Core.Math;

/// <summary>
/// Conversión entre coordenadas de mundo (Y hacia arriba) y de pantalla
/// (Y hacia abajo). Es la única fuente de verdad para el editor y el renderer.
/// </summary>
public static class CameraMath
{
    /// <summary>Píxeles de pantalla por unidad de mundo.</summary>
    public static float PixelsPerUnit(float orthographicSize, float zoom, int viewportHeight)
    {
        if (viewportHeight <= 0)
        {
            return 1f;
        }

        var halfHeight = System.MathF.Max(0.001f, orthographicSize);
        var z = System.MathF.Max(0.001f, zoom);
        return viewportHeight / (2f * halfHeight) * z;
    }

    public static Matrix4x4 GetViewProjection(Vector2 cameraPosition, float orthographicSize, float zoom, int width, int height)
    {
        if (width <= 0 || height <= 0)
        {
            return Matrix4x4.Identity;
        }

        var ppu = PixelsPerUnit(orthographicSize, zoom, height);
        var halfWidth = width / 2f / ppu;
        var halfHeight = height / 2f / ppu;

        var projection = Matrix4x4.CreateOrthographicOffCenter(-halfWidth, halfWidth, -halfHeight, halfHeight, -1000f, 1000f);
        var view = Matrix4x4.CreateTranslation(-cameraPosition.X, -cameraPosition.Y, 0f);
        return view * projection;
    }

    public static Vector2 ScreenToWorld(Vector2 cameraPosition, float orthographicSize, float zoom, int width, int height, Vector2 screen)
    {
        var ppu = PixelsPerUnit(orthographicSize, zoom, height);
        return new Vector2(
            cameraPosition.X + (screen.X - width / 2f) / ppu,
            cameraPosition.Y - (screen.Y - height / 2f) / ppu);
    }

    public static Vector2 WorldToScreen(Vector2 cameraPosition, float orthographicSize, float zoom, int width, int height, Vector2 world)
    {
        var ppu = PixelsPerUnit(orthographicSize, zoom, height);
        return new Vector2(
            width / 2f + (world.X - cameraPosition.X) * ppu,
            height / 2f - (world.Y - cameraPosition.Y) * ppu);
    }
}
