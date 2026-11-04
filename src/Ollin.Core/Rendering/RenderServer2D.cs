using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Rendering;

/// <summary>Comando de sprite recolectado por el servidor de render.</summary>
public readonly record struct SpriteDrawCommand(
    Entity Entity,
    SpriteRenderer Sprite,
    CanvasLayer? Layer,
    CanvasItem2D? CanvasItem);

/// <summary>
/// Display list 2D. Separa la recolección y el orden de la escena del backend
/// OpenGL, igual que un RenderServer conceptual.
/// </summary>
public sealed class RenderServer2D
{
    private readonly List<SpriteDrawCommand> _sprites = new();

    public IReadOnlyList<SpriteDrawCommand> Sprites => _sprites;
    public int FrameId { get; private set; }
    public int SubmittedSprites => _sprites.Count;

    public void Submit(Scene scene)
    {
        FrameId++;
        _sprites.Clear();
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            var layer = entity.GetComponent<CanvasLayer>();
            var item = entity.GetComponent<CanvasItem2D>();
            if (layer is { Visible: false } || item is { Visible: false }) continue;
            if (entity.GetComponent<SpriteRenderer>() is not { Enabled: true, Visible: true } sprite) continue;
            _sprites.Add(new SpriteDrawCommand(entity, sprite, layer, item));
        }

        _sprites.Sort(Compare);
    }

    private static int Compare(SpriteDrawCommand a, SpriteDrawCommand b)
    {
        var layer = (a.Layer?.Order ?? 0).CompareTo(b.Layer?.Order ?? 0);
        if (layer != 0) return layer;
        var z = (a.CanvasItem?.ZIndex ?? 0).CompareTo(b.CanvasItem?.ZIndex ?? 0);
        if (z != 0) return z;
        if (a.Layer?.YSort == true || b.Layer?.YSort == true || a.CanvasItem?.YSort == true || b.CanvasItem?.YSort == true)
        {
            var y = b.Entity.Transform.Position.Y.CompareTo(a.Entity.Transform.Position.Y);
            if (y != 0) return y;
        }
        var order = a.Sprite.SortingOrder.CompareTo(b.Sprite.SortingOrder);
        return order != 0 ? order : string.CompareOrdinal(a.Entity.Name, b.Entity.Name);
    }
}
