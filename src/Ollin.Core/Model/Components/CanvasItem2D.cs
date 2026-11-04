using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Equivalente conceptual a CanvasItem de Godot: propiedades comunes de dibujo
/// y ordenación. Ollin mantiene el modelo ECS, no copia el árbol de nodos.
/// </summary>
public sealed class CanvasItem2D : Component
{
    public bool Visible { get; set; } = true;
    public Vector4 Modulate { get; set; } = Vector4.One;
    public Vector4 SelfModulate { get; set; } = Vector4.One;
    public int ZIndex { get; set; }
    public bool ZAsRelative { get; set; } = true;
    public bool YSort { get; set; }
    public int LightMask { get; set; } = 1;

    public Vector4 EffectiveModulate => new(
        Modulate.X * SelfModulate.X,
        Modulate.Y * SelfModulate.Y,
        Modulate.Z * SelfModulate.Z,
        Modulate.W * SelfModulate.W);

    public override Component CloneFor(Entity newOwner) => new CanvasItem2D
    {
        Enabled = Enabled, Visible = Visible, Modulate = Modulate, SelfModulate = SelfModulate,
        ZIndex = ZIndex, ZAsRelative = ZAsRelative, YSort = YSort, LightMask = LightMask, Owner = newOwner,
    };
}
