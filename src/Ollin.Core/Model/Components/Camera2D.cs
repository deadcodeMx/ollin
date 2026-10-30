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

    public override Component CloneFor(Entity newOwner) => new Camera2D
    {
        Enabled = Enabled,
        OrthographicSize = OrthographicSize,
        Zoom = Zoom,
        Background = Background,
        IsMain = IsMain,
        Owner = newOwner,
    };
}
