using System.Text.Json.Serialization;
using Ollin.Core.Hud;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Puerta sin código: se abre con E si el jugador tiene la llave. La caja
/// <see cref="Wall3D"/> sube para dejar pasar.
/// </summary>
public sealed class Door : Component, ITickable
{
    public bool Open { get; set; }

    public string KeyItemId { get; set; } = "llave";

    public float InteractionRadius { get; set; } = 2.8f;

    public float OpenHeight { get; set; } = 3f;

    public float Speed { get; set; } = 2f;

    public string PlayerTag { get; set; } = "player";

    public string OpenMessage { get; set; } = "Puerta abierta";

    public string LockedMessage { get; set; } = "Necesitas una llave";

    private float _baseY;
    private bool _initialized;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene || Owner.GetComponent<Wall3D>() is not { } wall)
        {
            return;
        }

        if (!_initialized)
        {
            _baseY = wall.BaseY;
            _initialized = true;
        }

        if (!Open)
        {
            var player = FindPlayer(scene);

            if (player is null)
            {
                return;
            }

            var distance = scene.WorldDistance(Owner, player);
            HudHost.SetLabel("Puerta", distance <= InteractionRadius ? "E para abrir" : string.Empty);

            if (distance > InteractionRadius || !InputState.WasKeyPressed(KeyCode.E))
            {
                return;
            }

            var inventory = player.GetComponent<Inventory>();
            var hasKey = string.IsNullOrWhiteSpace(KeyItemId) || inventory?.Has(KeyItemId, 1) == true;

            if (!hasKey)
            {
                HudHost.ShowMessage(LockedMessage, 2f);
                return;
            }

            Open = true;
            HudHost.ShowMessage(OpenMessage, 2f);
        }

        wall.BaseY = MathF.Min(_baseY + OpenHeight, wall.BaseY + Speed * deltaTime);
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

    public override Component CloneFor(Entity newOwner) => new Door
    {
        Enabled = Enabled,
        Open = Open,
        KeyItemId = KeyItemId,
        InteractionRadius = InteractionRadius,
        OpenHeight = OpenHeight,
        Speed = Speed,
        PlayerTag = PlayerTag,
        OpenMessage = OpenMessage,
        LockedMessage = LockedMessage,
        Owner = newOwner,
    };
}
