namespace Ollin.Core.Model.Components;

/// <summary>Capa de render 2D: orden, visibilidad y Y-sort.</summary>
public sealed class CanvasLayer : Component
{
    public int Order { get; set; }

    public bool Visible { get; set; } = true;

    /// <summary>Ordena los sprites de la capa por su Y de mundo.</summary>
    public bool YSort { get; set; }

    public override Component CloneFor(Entity newOwner) => new CanvasLayer
    {
        Enabled = Enabled, Order = Order, Visible = Visible, YSort = YSort, Owner = newOwner,
    };
}
