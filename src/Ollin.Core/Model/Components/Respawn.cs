using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Hud;

namespace Ollin.Core.Model.Components;

/// <summary>Reaparición del jugador con vidas y punto de control.</summary>
public sealed class Respawn : Component, ITickable
{
    public int Lives { get; set; } = 3;

    public float RespawnSeconds { get; set; } = 1.5f;

    public bool ResetHealth { get; set; } = true;

    [JsonIgnore]
    public int LivesRemaining { get; private set; }

    [JsonIgnore]
    public bool CanRespawn => LivesRemaining > 0;

    [JsonIgnore]
    public bool IsWaiting { get; private set; }

    [JsonIgnore]
    public Vector2 Checkpoint { get; private set; }

    private bool _initialized;
    private bool _deathHandled;
    private float _timer;

    public void SetCheckpoint(Vector2 position, bool heal = true)
    {
        Checkpoint = position;

        if (heal && Owner?.GetComponent<Health>() is { } health)
        {
            health.Current = health.Max;
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (Owner is null)
        {
            return;
        }

        if (!_initialized)
        {
            _initialized = true;
            LivesRemaining = System.Math.Max(0, Lives);
            Checkpoint = Owner.Transform.Position;
            HudHost.SetLabel("Vidas", LivesRemaining.ToString());
        }

        var health = Owner.GetComponent<Health>();

        if (health is null || !health.IsDead)
        {
            return;
        }

        if (!_deathHandled)
        {
            _deathHandled = true;
            LivesRemaining = System.Math.Max(0, LivesRemaining - 1);
            HudHost.SetLabel("Vidas", LivesRemaining.ToString());

            if (LivesRemaining <= 0)
            {
                IsWaiting = false;
                return;
            }

            IsWaiting = true;
            _timer = MathF.Max(0.1f, RespawnSeconds);
        }

        if (!IsWaiting)
        {
            return;
        }

        _timer -= deltaTime;

        if (_timer > 0f)
        {
            return;
        }

        Owner.Transform.Position = Checkpoint;
        Owner.Active = true;

        if (ResetHealth && health is not null)
        {
            health.Current = health.Max;
        }

        IsWaiting = false;
        _deathHandled = false;
        HudHost.ShowMessage("Reapareces", 1.5f);
    }

    public override Component CloneFor(Entity newOwner) => new Respawn
    {
        Enabled = Enabled,
        Lives = Lives,
        RespawnSeconds = RespawnSeconds,
        ResetHealth = ResetHealth,
        Owner = newOwner,
    };
}
