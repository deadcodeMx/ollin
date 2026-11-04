using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Segunda cámara dibujada en un rectángulo normalizado del viewport.</summary>
public sealed class SubViewport2D : Component
{
    public string CameraTag { get; set; } = "main";
    public Vector4 Rect { get; set; } = new(0.65f, 0.65f, 0.3f, 0.3f);
    public bool Clear { get; set; } = true;
    /// <summary>Renderiza a un framebuffer y compone la textura.</summary>
    public bool UseTexture { get; set; } = true;
    public bool DebugBorder { get; set; } = true;
    public Vector4 BorderColor { get; set; } = new(0.91f, 0.64f, 0.23f, 1f);

    public override Component CloneFor(Entity newOwner) => new SubViewport2D
    {
        Enabled = Enabled, CameraTag = CameraTag, Rect = Rect, Clear = Clear, UseTexture = UseTexture, DebugBorder = DebugBorder, BorderColor = BorderColor, Owner = newOwner,
    };
}
