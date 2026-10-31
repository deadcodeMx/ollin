using System.Text.Json.Serialization;
using Ollin.Core.Hud;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

public enum ScriptEvent
{
    OnStart,
    OnTimer,
    OnPlayerEnter,
    OnInteract,
    OnPlayerDeath,
}

public enum ScriptCondition
{
    None,
    HasItem,
    HealthBelow,
}

/// <summary>Una regla visual: evento + condición + acción.</summary>
public sealed class ScriptRule
{
    public ScriptEvent Event { get; set; } = ScriptEvent.OnStart;

    public ScriptCondition Condition { get; set; } = ScriptCondition.None;

    public string ConditionItem { get; set; } = "llave";

    public float Threshold { get; set; } = 50f;

    public ActionKind Action { get; set; } = ActionKind.Message;

    public string TargetName { get; set; } = string.Empty;

    public string Message { get; set; } = "¡Hola!";

    public float Radius { get; set; } = 2.5f;

    public float Interval { get; set; } = 3f;

    public float Amount { get; set; } = 20f;

    public bool Once { get; set; } = true;

    [JsonIgnore]
    internal bool Done { get; set; }

    [JsonIgnore]
    internal bool Inside { get; set; }

    [JsonIgnore]
    internal float Timer { get; set; }

    public ScriptRule Clone() => new()
    {
        Event = Event,
        Condition = Condition,
        ConditionItem = ConditionItem,
        Threshold = Threshold,
        Action = Action,
        TargetName = TargetName,
        Message = Message,
        Radius = Radius,
        Interval = Interval,
        Amount = Amount,
        Once = Once,
    };
}

/// <summary>
/// Scripting visual sin nodos: cuatro reglas configurables desde el inspector.
/// Cada regla reacciona a un evento, comprueba una condición y ejecuta una acción.
/// </summary>
public sealed class VisualScript : Component, ITickable
{
    public List<ScriptRule> Rules { get; set; } = CreateDefaultRules();

    public static List<ScriptRule> CreateDefaultRules() => new()
    {
        new ScriptRule(),
        new ScriptRule { Event = ScriptEvent.OnPlayerEnter, Action = ActionKind.Message, Message = "Entraste en la zona" },
        new ScriptRule { Event = ScriptEvent.OnInteract, Action = ActionKind.OpenDoor, TargetName = "Puerta", Message = "Puerta abierta" },
        new ScriptRule { Event = ScriptEvent.OnTimer, Action = ActionKind.Message, Message = "Tic", Interval = 5f },
    };

    public ScriptRule GetRule(int index)
    {
        while (Rules.Count <= index)
        {
            Rules.Add(new ScriptRule());
        }

        index = System.Math.Clamp(index, 0, Rules.Count - 1);
        return Rules[index];
    }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        var player = ActionExecutor.FindPlayer(scene);

        for (var index = 0; index < Rules.Count; index++)
        {
            var rule = Rules[index];

            if (rule.Done && (rule.Once || rule.Event == ScriptEvent.OnStart))
            {
                continue;
            }

            if (!ConditionMet(rule, player))
            {
                continue;
            }

            var fire = false;

            switch (rule.Event)
            {
                case ScriptEvent.OnStart:
                    fire = true;
                    rule.Done = true;
                    break;

                case ScriptEvent.OnTimer:
                    rule.Timer -= deltaTime;

                    if (rule.Timer <= 0f)
                    {
                        rule.Timer = MathF.Max(0.25f, rule.Interval);
                        fire = true;
                    }
                    break;

                case ScriptEvent.OnPlayerEnter:
                {
                    var inside = player is not null &&
                                 System.Numerics.Vector2.Distance(Owner.Transform.Position, player.Transform.Position) <= rule.Radius;

                    if (inside && !rule.Inside)
                    {
                        fire = true;
                    }

                    rule.Inside = inside;
                    break;
                }

                case ScriptEvent.OnInteract:
                    if (player is not null &&
                        System.Numerics.Vector2.Distance(Owner.Transform.Position, player.Transform.Position) <= rule.Radius &&
                        InputState.WasKeyPressed(KeyCode.E))
                    {
                        fire = true;
                    }
                    break;

                case ScriptEvent.OnPlayerDeath:
                    fire = player?.GetComponent<Health>() is { IsDead: true };
                    break;
            }

            if (!fire)
            {
                continue;
            }

            if (rule.Once)
            {
                rule.Done = true;
            }

            ActionExecutor.Execute(scene, rule.Action, rule.TargetName, rule.Message, "player", rule.Amount);
        }
    }

    private static bool ConditionMet(ScriptRule rule, Entity? player)
    {
        if (player is null)
        {
            return rule.Condition == ScriptCondition.None;
        }

        return rule.Condition switch
        {
            ScriptCondition.HasItem => player.GetComponent<Inventory>()?.Has(rule.ConditionItem, 1) == true,
            ScriptCondition.HealthBelow => player.GetComponent<Health>() is { } health && health.Current <= rule.Threshold,
            _ => true,
        };
    }

    public override Component CloneFor(Entity newOwner) => new VisualScript
    {
        Enabled = Enabled,
        Rules = Rules.Select(rule => rule.Clone()).ToList(),
        Owner = newOwner,
    };
}
