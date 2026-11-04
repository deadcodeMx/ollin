using System.Text.Json.Serialization;
using Ollin.Core.Hud;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

public enum ActionKind
{
    Message,
    OpenDoor,
    Activate,
    Deactivate,
    Spawn,
    Checkpoint,
    Win,
    Damage,
    Heal,
    Teleport,
    SetVariable,
    AddVariable,
    ToggleFlag,
}

public static class ActionExecutor
{
    public static void Execute(Scenes.Scene scene, ActionKind action, string targetName, string message, string playerTag = "player", float amount = 0f)
    {
        if (!string.IsNullOrWhiteSpace(message))
        {
            HudHost.ShowMessage(message, 2.5f);
        }

        var target = string.IsNullOrWhiteSpace(targetName) ? null : scene.Find(targetName);

        switch (action)
        {
            case ActionKind.OpenDoor:
                if (target?.GetComponent<Door>() is { } door)
                {
                    door.Open = true;
                }
                break;

            case ActionKind.Activate:
                if (target is not null) target.Active = true;
                break;

            case ActionKind.Deactivate:
                if (target is not null) target.Active = false;
                break;

            case ActionKind.Spawn:
                target?.GetComponent<Spawner3D>()?.SpawnNow();
                break;

            case ActionKind.Checkpoint:
                if (FindPlayer(scene, playerTag) is { } player &&
                    player.GetComponent<Respawn>() is { } respawn)
                {
                    respawn.SetCheckpoint(scene.GetWorldPosition(player));
                }
                break;

            case ActionKind.Damage:
                target?.GetComponent<Health>()?.TakeDamage(amount > 0f ? amount : 20f);
                break;

            case ActionKind.Heal:
                (target ?? FindPlayer(scene, playerTag))?.GetComponent<Health>()?.Heal(amount > 0f ? amount : 20f);
                break;

            case ActionKind.Teleport:
                if (target is not null && FindPlayer(scene, playerTag) is { } targetPlayer)
                {
                    scene.SetWorldPosition(targetPlayer, scene.GetWorldPosition(target));
                }
                break;

            case ActionKind.Win:
                HudHost.GameOver(string.IsNullOrWhiteSpace(message) ? "¡Has ganado!" : message);
                break;
        }
    }

    public static Entity? FindPlayer(Scenes.Scene scene, string tag = "player")
    {
        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.Tags.Any(value => value.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                return entity;
            }
        }

        return null;
    }
}

