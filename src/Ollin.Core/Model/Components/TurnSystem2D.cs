using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Unidad que participa en el orden de turnos.</summary>
public sealed class TurnActor : Component
{
    public string Team { get; set; } = "player";
    public int Initiative { get; set; }
    public override Component CloneFor(Entity newOwner) => new TurnActor { Enabled = Enabled, Team = Team, Initiative = Initiative, Owner = newOwner };
}

/// <summary>Gestor de turnos por iniciativa para combates tácticos.</summary>
public sealed class TurnManager : Component, ITickable
{
    public bool AutoStart { get; set; } = true;
    public float TurnSeconds { get; set; }
    public int Round { get; private set; }
    public int CurrentIndex { get; private set; }
    public bool IsActive { get; private set; }

    [JsonIgnore] public List<Guid> Order { get; } = new();
    [JsonIgnore] public Guid CurrentActor => CurrentIndex >= 0 && CurrentIndex < Order.Count ? Order[CurrentIndex] : Guid.Empty;

    private float _timeLeft;
    private bool _started;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        if (!_started && AutoStart) { _started = true; Start(); }
        if (!IsActive) return;

        if (TurnSeconds > 0f)
        {
            _timeLeft -= deltaTime;
            if (_timeLeft <= 0f) EndTurn();
        }

        var actor = scene.Entities.FirstOrDefault(entity => entity.Id == CurrentActor);
        Hud.HudHost.SetLabel("Turno", actor is null ? $"Ronda {Round}" : $"Ronda {Round}: {actor.Name}");
    }

    public void Start()
    {
        if (Owner?.Scene is not { } scene) return;
        Order.Clear();
        Order.AddRange(scene.Entities
            .Where(entity => entity.Active && entity.GetComponent<TurnActor>() is not null)
            .OrderByDescending(entity => entity.GetComponent<TurnActor>()!.Initiative)
            .ThenBy(entity => entity.Name)
            .Select(entity => entity.Id));
        if (Order.Count == 0) { IsActive = false; return; }
        Round = 1;
        CurrentIndex = 0;
        IsActive = true;
        _timeLeft = TurnSeconds;
    }

    public void EndTurn()
    {
        if (!IsActive || Order.Count == 0) return;
        CurrentIndex++;
        if (CurrentIndex >= Order.Count) { CurrentIndex = 0; Round++; }
        _timeLeft = TurnSeconds;
    }

    public override Component CloneFor(Entity newOwner) => new TurnManager
    {
        Enabled = Enabled, AutoStart = AutoStart, TurnSeconds = TurnSeconds, Owner = newOwner,
    };
}
