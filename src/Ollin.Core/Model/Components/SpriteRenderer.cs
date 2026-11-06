using System.Numerics;

namespace Ollin.Core.Model.Components;

public enum SpriteShape2D
{
    Quad,
    Cylinder,
    Cone,
}

public enum BlendMode2D
{
    Normal,
    Add,
    Multiply,
}

/// <summary>
/// Dibuja un sprite (o un cuadro de color sólido si no hay textura).
/// El tamaño está en unidades de mundo y el origen es relativo al sprite
/// (0.5, 0.5 = centro).
/// </summary>
public sealed class SpriteRenderer : Component
{
    /// <summary>Ruta relativa a la textura, por ejemplo "assets/sprites/hero.png".</summary>
    public string? TexturePath { get; set; }

    /// <summary>Tinte multiplicativo RGBA.</summary>
    public Vector4 Tint { get; set; } = Vector4.One;

    public Vector2 Size { get; set; } = Vector2.One;
    public SpriteShape2D Shape { get; set; } = SpriteShape2D.Quad;

    public Vector2 Origin { get; set; } = new(0.5f, 0.5f);

    /// <summary>
    /// Recorte de la textura en píxeles (x, y, ancho, alto) para hojas de sprites.
    /// Si el ancho es 0 se dibuja la textura completa.
    /// </summary>
    public Vector4 FrameRect { get; set; }

    /// <summary>Orden de dibujo: menor se dibuja primero.</summary>
    public int SortingOrder { get; set; }

    public bool FlipX { get; set; }

    public bool FlipY { get; set; }

    public bool Visible { get; set; } = true;

    public BlendMode2D BlendMode { get; set; } = BlendMode2D.Normal;

    /// <summary>1 = mundo normal; 0 = fijo a la cámara (fondos con paralaje).</summary>
    public float Parallax { get; set; } = 1f;

    public override Component CloneFor(Entity newOwner) => new SpriteRenderer
    {
        Enabled = Enabled,
        TexturePath = TexturePath,
        Tint = Tint,
        Size = Size,
        Shape = Shape,
        Origin = Origin,
        FrameRect = FrameRect,
        SortingOrder = SortingOrder,
        FlipX = FlipX,
        FlipY = FlipY,
        Visible = Visible,
        BlendMode = BlendMode,
        Parallax = Parallax,
        Owner = newOwner,
    };
}
