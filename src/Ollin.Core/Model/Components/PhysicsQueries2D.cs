using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Physics;
using Ollin.Core.Scenes;

namespace Ollin.Core.Model.Components;

/// <summary>Rayo 2D que actualiza su impacto cada frame.</summary>
public sealed class RayCast2D : Component, ITickable
{
    public Vector2 Origin { get; set; }
    public Vector2 Direction { get; set; } = Vector2.UnitX;
    public float Length { get; set; } = 5f;
    public int LayerMask { get; set; } = ~0;
    public bool IncludeTriggers { get; set; }
    [JsonIgnore] public RaycastHit2D? Hit { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        Hit = PhysicsQuery.Raycast(scene, scene.GetWorldPosition(Owner) + Origin, Direction, Length, LayerMask, Owner, IncludeTriggers);
    }

    public override Component CloneFor(Entity newOwner) => new RayCast2D
    {
        Enabled = Enabled, Origin = Origin, Direction = Direction, Length = Length,
        LayerMask = LayerMask, IncludeTriggers = IncludeTriggers, Owner = newOwner,
    };
}

/// <summary>Barrido de una caja 2D; devuelve el primer impacto.</summary>
public sealed class ShapeCast2D : Component, ITickable
{
    public Vector2 Size { get; set; } = Vector2.One;
    public Vector2 Direction { get; set; } = Vector2.UnitX;
    public float Length { get; set; } = 5f;
    public int LayerMask { get; set; } = ~0;
    public bool IncludeTriggers { get; set; }
    [JsonIgnore] public RaycastHit2D? Hit { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        Hit = PhysicsQuery.BoxCast(scene, scene.GetWorldPosition(Owner), Size, Direction, Length, LayerMask, Owner, IncludeTriggers);
    }

    public override Component CloneFor(Entity newOwner) => new ShapeCast2D
    {
        Enabled = Enabled, Size = Size, Direction = Direction, Length = Length,
        LayerMask = LayerMask, IncludeTriggers = IncludeTriggers, Owner = newOwner,
    };
}
