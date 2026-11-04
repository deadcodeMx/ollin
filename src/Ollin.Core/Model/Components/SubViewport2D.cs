using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Segunda cámara dibujada en un rectángulo normalizado del viewport.</summary>
public sealed class SubViewport2D : Component
{
    public bool Enabled { get; set; } = true;
    public string CameraTag { get; set; } = "main";
    public Vector4 Rect { get; set; } = new(0.65f, 0.65f, 0.3f, 0.3f);
    public bool Clear { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new SubViewport2D
    {
        Enabled = Enabled, CameraTag = CameraTag, Rect = Rect, Clear = Clear, Owner = newOwner,
    };
}
