using Ollin.Core.Hud;

namespace Ollin.Core.Model.Components;

/// <summary>Acciones genéricas del grafo visual.</summary>
public enum ActionKind
{
    Message,
    Activate,
    Deactivate,
    Teleport,
    SetVariable,
    AddVariable,
    ToggleFlag,
    Win,
    OpenDoor,
    Spawn,
    Checkpoint,
    Damage,
    Heal,
}

/// <summary>Ejecutor genérico: no depende de componentes 3D ni de gameplay.</summary>
public static class ActionExecutor
{
    public static void Execute(Scenes.Scene scene, ActionKind action, string targetName, string message, string playerTag = "player", float amount = 0f)
    {
        if (!string.IsNullOrWhiteSpace(message)) HudHost.ShowMessage(message, 2.5f);
        var target = string.IsNullOrWhiteSpace(targetName) ? null : scene.Find(targetName);

        switch (action)
        {
            case ActionKind.Activate when target is not null:
                target.Active = true;
                break;
            case ActionKind.Deactivate when target is not null:
                target.Active = false;
                break;
            case ActionKind.Teleport when target is not null && FindPlayer(scene, playerTag) is { } player:
                scene.SetWorldPosition(player, scene.GetWorldPosition(target));
                break;
            case ActionKind.OpenDoor when target is not null:
                SetProperty(target, "Door", "Open", true);
                break;
            case ActionKind.Spawn when target is not null:
                Invoke(target, "Spawner3D", "SpawnNow");
                break;
            case ActionKind.Checkpoint when FindPlayer(scene, playerTag) is { } checkpointPlayer:
                Invoke(checkpointPlayer, "Respawn", "SetCheckpoint", scene.GetWorldPosition(checkpointPlayer));
                break;
            case ActionKind.Damage when target is not null:
                Invoke(target, "Health", "TakeDamage", amount > 0f ? amount : 20f);
                break;
            case ActionKind.Heal:
                Invoke(target ?? FindPlayer(scene, playerTag), "Health", "Heal", amount > 0f ? amount : 20f);
                break;            case ActionKind.Win:
                HudHost.GameOver(string.IsNullOrWhiteSpace(message) ? "¡Has ganado!" : message);
                break;
            default:
                // OpenDoor, Spawn, Checkpoint, Damage y Heal son acciones de gameplay;
                // el grafo las representa, pero el usuario las implementa en su script.
                break;
        }
    }

    private static void SetProperty(Entity? entity, string componentName, string propertyName, object value)
    {
        var component = entity?.Components.FirstOrDefault(c => c.GetType().Name == componentName);
        component?.GetType().GetProperty(propertyName)?.SetValue(component, value);
    }

    private static void Invoke(Entity? entity, string componentName, string methodName, params object[] args)
    {
        var component = entity?.Components.FirstOrDefault(c => c.GetType().Name == componentName);
        component?.GetType().GetMethod(methodName)?.Invoke(component, args);
    }
    public static Entity? FindPlayer(Scenes.Scene scene, string tag = "player") =>
        scene.Entities.FirstOrDefault(e => e.Active && e.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
}
