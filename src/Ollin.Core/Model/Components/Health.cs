using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Hud;

namespace Ollin.Core.Model.Components;

/// <summary>Salud genérica para jugador o enemigo, configurable desde el inspector.</summary>
public sealed class Health : Component, ITickable
{
    public float Max { get; set; } = 100f;

    public float Current { get; set; } = 100f;

    public bool DeactivateOnDeath { get; set; } = true;

    public bool ShowOnHud { get; set; } = true;

    public string HudLabel { get; set; } = "Vida";

    public Vector4 HudColor { get; set; } = new(0.28f, 0.75f, 0.6f, 1f);

    public float InvulnerabilitySeconds { get; set; } = 0.15f;

    [JsonIgnore]
    public bool IsDead => Current <= 0f;

    private float _cooldown;

    public void TakeDamage(float amount)
    {
        if (IsDead || amount <= 0f || _cooldown > 0f)
        {
            return;
        }

        Current = MathF.Max(0f, Current - amount);
        _cooldown = MathF.Max(0f, InvulnerabilitySeconds);

        if (Current <= 0f && DeactivateOnDeath && Owner is not null)
        {
            Owner.Active = false;
        }
    }

    public void Heal(float amount)
    {
        if (amount <= 0f)
        {
            return;
        }

        Current = MathF.Min(Max, Current + amount);
    }

    public void OnUpdate(float deltaTime)
    {
        if (_cooldown > 0f)
        {
            _cooldown -= deltaTime;
        }

        if (ShowOnHud && Owner is { Active: true })
        {
            HudHost.SetBar(HudLabel, Current, Max, HudColor);
        }
    }

    public override Component CloneFor(Entity newOwner) => new Health
    {
        Enabled = Enabled,
        Max = Max,
        Current = Current,
        DeactivateOnDeath = DeactivateOnDeath,
        ShowOnHud = ShowOnHud,
        HudLabel = HudLabel,
        HudColor = HudColor,
        InvulnerabilitySeconds = InvulnerabilitySeconds,
        Owner = newOwner,
    };
}
