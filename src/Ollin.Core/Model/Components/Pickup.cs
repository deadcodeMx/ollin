using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Hud;

namespace Ollin.Core.Model.Components;

public enum PickupKind
{
    Item,
    Health,
}

/// <summary>Objeto recogible: añade un objeto al inventario o cura al jugador.</summary>
public sealed class Pickup : Component, ITickable
{
    public PickupKind Kind { get; set; } = PickupKind.Item;

    public string ItemId { get; set; } = "llave";

    public int Quantity { get; set; } = 1;

    public float HealthAmount { get; set; } = 25f;

    public float Radius { get; set; } = 1.2f;

    public string Message { get; set; } = "Objeto recogido";

    public string PlayerTag { get; set; } = "player";

    [JsonIgnore]
    public bool Collected { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Collected || Owner?.Scene is not { } scene)
        {
            return;
        }

        var player = FindPlayer(scene);

        if (player is null || Vector2.Distance(Owner.Transform.Position, player.Transform.Position) > Radius)
        {
            return;
        }

        if (Kind == PickupKind.Health)
        {
            var health = player.GetComponent<Health>();

            if (health is null || health.Current >= health.Max)
            {
                return;
            }

            health.Heal(HealthAmount);
        }
        else
        {
            var inventory = player.GetComponent<Inventory>();

            if (inventory is null || inventory.Add(ItemId, Quantity) <= 0)
            {
                return;
            }
        }

        Collected = true;
        HudHost.ShowMessage(Message, 2f);
        Owner.Active = false;
    }

    private Entity? FindPlayer(Scenes.Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.Tags.Any(tag => tag.Equals(PlayerTag, StringComparison.OrdinalIgnoreCase)))
            {
                return entity;
            }
        }

        return null;
    }

    public override Component CloneFor(Entity newOwner) => new Pickup
    {
        Enabled = Enabled,
        Kind = Kind,
        ItemId = ItemId,
        Quantity = Quantity,
        HealthAmount = HealthAmount,
        Radius = Radius,
        Message = Message,
        PlayerTag = PlayerTag,
        Owner = newOwner,
    };
}
