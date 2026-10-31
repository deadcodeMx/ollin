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
}

public static class ActionExecutor
{
    public static void Execute(Scenes.Scene scene, ActionKind action, string targetName, string message, string playerTag = "player")
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
                    respawn.SetCheckpoint(player.Transform.Position);
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

/// <summary>Botón que se activa con E y ejecuta una acción sobre otra entidad.</summary>
public sealed class Button3D : Component, ITickable
{
    public float Radius { get; set; } = 2.5f;

    public string Prompt { get; set; } = "E para usar";

    public string TargetName { get; set; } = string.Empty;

    public ActionKind Action { get; set; } = ActionKind.OpenDoor;

    public string Message { get; set; } = string.Empty;

    public string PlayerTag { get; set; } = "player";

    public float CooldownSeconds { get; set; } = 0.5f;

    [JsonIgnore]
    public bool Used { get; private set; }

    private float _cooldown;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        if (_cooldown > 0f)
        {
            _cooldown -= deltaTime;
        }

        var player = ActionExecutor.FindPlayer(scene, PlayerTag);

        if (player is null)
        {
            return;
        }

        var distance = System.Numerics.Vector2.Distance(Owner.Transform.Position, player.Transform.Position);
        HudHost.SetLabel("Botón", distance <= Radius ? Prompt : string.Empty);

        if (distance > Radius || _cooldown > 0f || !InputState.WasKeyPressed(KeyCode.E))
        {
            return;
        }

        Used = true;
        _cooldown = MathF.Max(0.1f, CooldownSeconds);
        ActionExecutor.Execute(scene, Action, TargetName, Message, PlayerTag);
    }

    public override Component CloneFor(Entity newOwner) => new Button3D
    {
        Enabled = Enabled,
        Radius = Radius,
        Prompt = Prompt,
        TargetName = TargetName,
        Action = Action,
        Message = Message,
        PlayerTag = PlayerTag,
        CooldownSeconds = CooldownSeconds,
        Owner = newOwner,
    };
}

/// <summary>Zona que dispara una acción cuando el jugador entra.</summary>
public sealed class Trigger3D : Component, ITickable
{
    public float Radius { get; set; } = 2f;

    public string PlayerTag { get; set; } = "player";

    public ActionKind Action { get; set; } = ActionKind.Message;

    public string TargetName { get; set; } = string.Empty;

    public string Message { get; set; } = string.Empty;

    public bool Once { get; set; } = true;

    [JsonIgnore]
    public bool Activated { get; private set; }

    private bool _inside;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene || (Once && Activated))
        {
            return;
        }

        var player = ActionExecutor.FindPlayer(scene, PlayerTag);

        if (player is null)
        {
            return;
        }

        var inside = System.Numerics.Vector2.Distance(Owner.Transform.Position, player.Transform.Position) <= Radius;

        if (!inside)
        {
            _inside = false;
            return;
        }

        if (_inside)
        {
            return;
        }

        _inside = true;
        Activated = true;
        ActionExecutor.Execute(scene, Action, TargetName, Message, PlayerTag);
    }

    public override Component CloneFor(Entity newOwner) => new Trigger3D
    {
        Enabled = Enabled,
        Radius = Radius,
        PlayerTag = PlayerTag,
        Action = Action,
        TargetName = TargetName,
        Message = Message,
        Once = Once,
        Owner = newOwner,
    };
}

/// <summary>Genera copias de una entidad plantilla (normalmente inactiva).</summary>
public sealed class Spawner3D : Component, ITickable
{
    public string TemplateName { get; set; } = string.Empty;

    public float Interval { get; set; } = 4f;

    public int MaxAlive { get; set; } = 3;

    public System.Numerics.Vector2 SpawnOffset { get; set; } = System.Numerics.Vector2.Zero;

    public bool AutoStart { get; set; } = true;

    [JsonIgnore]
    public int SpawnedAlive => _spawned.Count(entity => entity.Active);

    private readonly List<Entity> _spawned = new();
    private float _timer;

    public void SpawnNow()
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        var template = scene.Find(TemplateName);

        if (template is null || (MaxAlive > 0 && SpawnedAlive >= MaxAlive))
        {
            return;
        }

        _spawned.RemoveAll(entity => !entity.Active);
        var copy = template.Clone();
        copy.Name = $"{template.Name} {_spawned.Count + 1}";
        copy.Active = true;
        copy.Transform.Position = Owner.Transform.Position + SpawnOffset;
        scene.Add(copy);
        _spawned.Add(copy);
    }

    public void OnUpdate(float deltaTime)
    {
        if (!AutoStart)
        {
            return;
        }

        _timer -= deltaTime;

        if (_timer > 0f)
        {
            return;
        }

        _timer = MathF.Max(0.5f, Interval);
        SpawnNow();
    }

    public override Component CloneFor(Entity newOwner) => new Spawner3D
    {
        Enabled = Enabled,
        TemplateName = TemplateName,
        Interval = Interval,
        MaxAlive = MaxAlive,
        SpawnOffset = SpawnOffset,
        AutoStart = AutoStart,
        Owner = newOwner,
    };
}

/// <summary>Punto de control que actualiza la reaparición del jugador.</summary>
public sealed class Checkpoint3D : Component, ITickable
{
    public float Radius { get; set; } = 2f;

    public string PlayerTag { get; set; } = "player";

    public string Message { get; set; } = "Punto de control";

    public bool Heal { get; set; } = true;

    public bool Once { get; set; } = true;

    [JsonIgnore]
    public bool Activated { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene || (Once && Activated))
        {
            return;
        }

        var player = ActionExecutor.FindPlayer(scene, PlayerTag);

        if (player is null ||
            System.Numerics.Vector2.Distance(Owner.Transform.Position, player.Transform.Position) > Radius)
        {
            return;
        }

        Activated = true;
        player.GetComponent<Respawn>()?.SetCheckpoint(player.Transform.Position, Heal);

        if (!string.IsNullOrWhiteSpace(Message))
        {
            HudHost.ShowMessage(Message, 2f);
        }
    }

    public override Component CloneFor(Entity newOwner) => new Checkpoint3D
    {
        Enabled = Enabled,
        Radius = Radius,
        PlayerTag = PlayerTag,
        Message = Message,
        Heal = Heal,
        Once = Once,
        Owner = newOwner,
    };
}
