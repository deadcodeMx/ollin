using System.Numerics;
using System.Reflection;
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
    SetComponentProperty,
    Win,
    OpenDoor,
    Spawn,
    Checkpoint,
    Damage,
    Heal,
}

/// <summary>Puente por reflexión para que el grafo y las expresiones no dependan de sistemas RPG.</summary>
public static class GameplayReflection
{
    public static bool IsDead(Entity? entity) => Bool(entity, "Health", "IsDead");
    public static float Health(Entity? entity) => Number(entity, "Health", "Current");
    public static float HealthMax(Entity? entity) => Number(entity, "Health", "Max");
    public static float Lives(Entity? entity) => Number(entity, "Respawn", "LivesRemaining");
    public static bool HealthBelow(Entity? entity, float threshold) => Health(entity) <= threshold;

    public static bool HasItem(Entity? entity, string itemId, int quantity = 1) =>
        Invoke(entity, "Inventory", "Has", itemId, quantity) is true;

    private static object? Component(Entity? entity, string name) =>
        entity?.Components.FirstOrDefault(c => c.GetType().Name == name);

    private static float Number(Entity? entity, string component, string property) =>
        Component(entity, component)?.GetType().GetProperty(property)?.GetValue(Component(entity, component)) is float value ? value : 0f;

    private static bool Bool(Entity? entity, string component, string property) =>
        Component(entity, component)?.GetType().GetProperty(property)?.GetValue(Component(entity, component)) is true;

    private static object? Invoke(Entity? entity, string component, string method, params object[] args) =>
        Component(entity, component)?.GetType().GetMethod(method)?.Invoke(Component(entity, component), args);
}
/// <summary>Ejecutor genérico: no depende de componentes 3D ni de gameplay.</summary>
public static class ActionExecutor
{
    public static void Execute(Scenes.Scene scene, ActionKind action, string targetName, string message, string playerTag = "player", float amount = 0f, string componentName = "", string propertyName = "", Vector2 vectorValue = default)
    {
        if (!string.IsNullOrWhiteSpace(message)) HudHost.ShowMessage(message, 2.5f);
        var target = string.IsNullOrWhiteSpace(targetName) ? null : scene.Find(targetName);

        switch (action)
        {
            case ActionKind.SetComponentProperty when target is not null:
                SetComponentProperty(target, componentName, propertyName, amount, message, vectorValue);
                break;
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
    private static Vector2 ParseVector(string text, float fallback)
    {
        if (string.IsNullOrWhiteSpace(text)) return new Vector2(fallback, fallback);
        var parts = text.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length >= 2 &&
            float.TryParse(parts[0], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var x) &&
            float.TryParse(parts[1], System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var y))
        {
            return new Vector2(x, y);
        }
        return new Vector2(fallback, fallback);
    }
    private static void SetComponentProperty(Entity target, string componentName, string propertyName, float amount, string text, Vector2 vector)
    {
        var component = target.Components.FirstOrDefault(item => item.GetType().Name.Equals(componentName, StringComparison.OrdinalIgnoreCase));
        var property = component?.GetType().GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);
        if (property is null || !property.CanWrite) return;
        try
        {
            object? value = property.PropertyType switch
            {
                var type when type == typeof(Vector2) => vector != default ? vector : ParseVector(text, amount),
                var type when type == typeof(float) => amount,
                var type when type == typeof(double) => (double)amount,
                var type when type == typeof(int) => (int)amount,
                var type when type == typeof(bool) => amount != 0f,
                var type when type == typeof(string) => text,
                var type when type.IsEnum => Enum.ToObject(type, (int)amount),
                _ => Convert.ChangeType(amount, property.PropertyType, System.Globalization.CultureInfo.InvariantCulture),
            };
            property.SetValue(component, value);
        }
        catch { }
    }
    public static Entity? FindPlayer(Scenes.Scene scene, string tag = "player") =>
        scene.Entities.FirstOrDefault(e => e.Active && e.Tags.Any(t => t.Equals(tag, StringComparison.OrdinalIgnoreCase)));
}
