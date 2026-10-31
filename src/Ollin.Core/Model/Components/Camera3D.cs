using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Cámara en perspectiva para el puente 2.5D. El plano del mundo es X/Z: la X del
/// transform es el ancho y la Y del transform es la profundidad. La altura de los
/// ojos se configura aparte.
/// </summary>
public sealed class Camera3D : Component
{
    /// <summary>Altura de los ojos sobre el suelo.</summary>
    public float Height { get; set; } = 1.7f;

    /// <summary>Giro horizontal en radianes (0 = mirando hacia +Z).</summary>
    public float Yaw { get; set; }

    /// <summary>Inclinación vertical en radianes (positivo = mirando arriba).</summary>
    public float Pitch { get; set; }

    public float FovDegrees { get; set; } = 70f;

    public float Near { get; set; } = 0.1f;

    public float Far { get; set; } = 80f;

    public Vector4 Background { get; set; } = new(0.04f, 0.05f, 0.08f, 1f);

    public Vector4 FogColor { get; set; } = new(0.08f, 0.09f, 0.13f, 1f);

    public float FogStart { get; set; } = 6f;

    public float FogEnd { get; set; } = 28f;

    public bool FogEnabled { get; set; } = true;

    /// <summary>Luz ambiente (blanco = sin oscurecer).</summary>
    public Vector4 Ambient { get; set; } = new(0.55f, 0.56f, 0.62f, 1f);

    public bool IsMain { get; set; } = true;

    [JsonIgnore]
    public Vector3 Eye => Owner is { Scene: { } scene }
        ? new Vector3(scene.GetWorldPosition(Owner).X, Height, scene.GetWorldPosition(Owner).Y)
        : new Vector3(Owner?.Transform.Position.X ?? 0f, Height, Owner?.Transform.Position.Y ?? 0f);

    [JsonIgnore]
    public Vector3 Forward
    {
        get
        {
            var cosPitch = MathF.Cos(Pitch);
            return new Vector3(
                MathF.Sin(Yaw) * cosPitch,
                MathF.Sin(Pitch),
                MathF.Cos(Yaw) * cosPitch);
        }
    }

    [JsonIgnore]
    public Vector3 Right => new(MathF.Cos(Yaw), 0f, -MathF.Sin(Yaw));

    [JsonIgnore]
    public Vector3 Up => Vector3.Normalize(Vector3.Cross(Forward, Right));

    public float FocalLength(int viewportHeight)
    {
        var fov = System.Math.Clamp(FovDegrees, 20f, 140f) * (MathF.PI / 180f);
        return viewportHeight / (2f * MathF.Tan(fov * 0.5f));
    }

    /// <summary>Proyecta un punto del mundo a pantalla. Devuelve false si está detrás.</summary>
    public bool Project(Vector3 point, int width, int height, out Vector2 screen, out float depth)
    {
        screen = Vector2.Zero;
        var relative = point - Eye;
        depth = Vector3.Dot(relative, Forward);

        if (depth <= Near)
        {
            return false;
        }

        var focal = FocalLength(height);
        var x = Vector3.Dot(relative, Right);
        var y = Vector3.Dot(relative, Up);
        screen = new Vector2(
            width * 0.5f + x * focal / depth,
            height * 0.5f - y * focal / depth);
        return true;
    }

    /// <summary>Dirección del rayo que pasa por un punto de la pantalla.</summary>
    public Vector3 RayDirection(Vector2 screen, int width, int height)
    {
        var focal = FocalLength(height);
        var x = (screen.X - width * 0.5f) / focal;
        var y = -(screen.Y - height * 0.5f) / focal;
        return Vector3.Normalize(Forward + Right * x + Up * y);
    }

    /// <summary>Píxeles de pantalla por unidad de mundo a una profundidad dada.</summary>
    public float ScaleAt(float depth, int viewportHeight) =>
        FocalLength(viewportHeight) / MathF.Max(0.0001f, depth);

    public override Component CloneFor(Entity newOwner) => new Camera3D
    {
        Enabled = Enabled,
        Height = Height,
        Yaw = Yaw,
        Pitch = Pitch,
        FovDegrees = FovDegrees,
        Near = Near,
        Far = Far,
        Background = Background,
        FogColor = FogColor,
        FogStart = FogStart,
        FogEnd = FogEnd,
        FogEnabled = FogEnabled,
        Ambient = Ambient,
        IsMain = IsMain,
        Owner = newOwner,
    };
}
