using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Cámara 2D ortográfica. Su posición es la del transform de la entidad;
/// <see cref="OrthographicSize"/> es la mitad de la altura visible en unidades.
/// </summary>
public sealed class Camera2D : Component
{
    public float OrthographicSize { get; set; } = 6f;

    public float Zoom { get; set; } = 1f;

    /// <summary>Color de fondo de la escena.</summary>
    public Vector4 Background { get; set; } = new(0.07f, 0.08f, 0.11f, 1f);

    public bool IsMain { get; set; } = true;

    /// <summary>Luz ambiente de la escena (blanco = sin oscurecer).</summary>
    public Vector4 Ambient { get; set; } = Vector4.One;

    /// <summary>Etiqueta de la entidad a la que sigue la cámara (vacío = fija).</summary>
    public string? FollowTag { get; set; }

    /// <summary>Velocidad de seguimiento (mayor = más pegada).</summary>
    public float FollowSmoothing { get; set; } = 6f;

    /// <summary>Zona muerta alrededor del centro; el objetivo puede moverse dentro sin mover la cámara.</summary>
    public Vector2 Deadzone { get; set; } = new(0.75f, 0.5f);

    public bool UseBounds { get; set; }

    public Vector2 BoundsMin { get; set; } = new(-100f, -100f);

    public Vector2 BoundsMax { get; set; } = new(100f, 100f);

    public override Component CloneFor(Entity newOwner) => new Camera2D
    {
        Enabled = Enabled,
        OrthographicSize = OrthographicSize,
        Zoom = Zoom,
        Background = Background,
        IsMain = IsMain,
        Ambient = Ambient,
        FollowTag = FollowTag,
        FollowSmoothing = FollowSmoothing,
        Deadzone = Deadzone,
        UseBounds = UseBounds,
        BoundsMin = BoundsMin,
        BoundsMax = BoundsMax,
        Owner = newOwner,
    };
}
