using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Zona que aplica daño al entrar en contacto con un Hurtbox de otro equipo.</summary>
public sealed class Hitbox : Component, ITickable
{
    public float Damage { get; set; } = 10f;

    public string Team { get; set; } = "player";

    public float Cooldown { get; set; } = 0.4f;

    [JsonIgnore] public bool CanHit => _cooldown <= 0f;

    private float _cooldown;

    public void OnUpdate(float deltaTime) => _cooldown = MathF.Max(0f, _cooldown - deltaTime);

    public bool TryConsume()
    {
        if (_cooldown > 0f) return false;
        _cooldown = MathF.Max(0.01f, Cooldown);
        return true;
    }

    public override Component CloneFor(Entity newOwner) => new Hitbox
    {
        Enabled = Enabled, Damage = Damage, Team = Team, Cooldown = Cooldown, Owner = newOwner,
    };
}

/// <summary>Empuje aplicado al objetivo cuando una Hitbox lo alcanza.</summary>
public sealed class Knockback2D : Component
{
    public float Force { get; set; } = 6f;

    public float Upward { get; set; } = 2f;

    public override Component CloneFor(Entity newOwner) => new Knockback2D
    {
        Enabled = Enabled, Force = Force, Upward = Upward, Owner = newOwner,
    };
}

/// <summary>Zona que recibe daño. Requiere Health en la misma entidad.</summary>
public sealed class Hurtbox : Component
{
    public string Team { get; set; } = "enemy";

    public float DamageMultiplier { get; set; } = 1f;

    public override Component CloneFor(Entity newOwner) => new Hurtbox
    {
        Enabled = Enabled, Team = Team, DamageMultiplier = DamageMultiplier, Owner = newOwner,
    };
}

/// <summary>Punto de control 2D: al tocar al jugador guarda la posición de reaparición.</summary>
public sealed class Checkpoint2D : Component, ITickable
{
    public float Radius { get; set; } = 1.2f;

    public bool HealOnActivate { get; set; } = true;

    [JsonIgnore] public bool Activated { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Activated || Owner?.Scene is not { } scene) return;
        var position = scene.GetWorldPosition(Owner);

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || !entity.Tags.Any(tag => tag.Equals("player", StringComparison.OrdinalIgnoreCase))) continue;
            if (Vector2.Distance(scene.GetWorldPosition(entity), position) > Radius) continue;
            entity.GetComponent<Respawn>()?.SetCheckpoint(position, HealOnActivate);
            Activated = true;
            break;
        }
    }

    public override Component CloneFor(Entity newOwner) => new Checkpoint2D
    {
        Enabled = Enabled, Radius = Radius, HealOnActivate = HealOnActivate, Owner = newOwner,
    };
}
