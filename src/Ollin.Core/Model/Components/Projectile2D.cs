using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Scenes;

namespace Ollin.Core.Model.Components;

/// <summary>Proyectil 2D que daña entidades con Hurtbox/Health del equipo contrario.</summary>
public sealed class Projectile2D : Component, ITickable
{
    public float Speed { get; set; } = 14f;
    public float Damage { get; set; } = 10f;
    public float Lifetime { get; set; } = 3f;
    public float Radius { get; set; } = 0.25f;
    public string Team { get; set; } = "player";
    public Guid ShooterId { get; set; }
    [JsonIgnore] public Vector2 Direction { get; set; } = Vector2.UnitX;

    private float _age;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene) return;
        _age += deltaTime;
        if (_age >= Lifetime) { Owner.Active = false; return; }

        var next = scene.GetWorldPosition(Owner) + Direction * Speed * deltaTime;

        foreach (var wall in scene.Entities)
        {
            if (!wall.Active || wall.Id == ShooterId || wall.GetComponent<Collider2D>() is not { Enabled: true, IsTrigger: false } collider) continue;
            var corners = collider.GetWorldCorners(scene.GetWorldMatrix(wall));
            if (corners.Length == 0) continue;
            var min = corners[0]; var max = corners[0];
            foreach (var corner in corners) { min = Vector2.Min(min, corner); max = Vector2.Max(max, corner); }
            if (next.X > min.X - Radius && next.X < max.X + Radius && next.Y > min.Y - Radius && next.Y < max.Y + Radius)
            {
                Owner.Active = false;
                return;
            }
        }

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.Id == ShooterId) continue;
            var hurt = entity.GetComponent<Hurtbox>();
            var health = entity.GetComponent<Health>();
            if (hurt is null || health is null || health.IsDead) continue;
            if (string.Equals(hurt.Team, Team, StringComparison.OrdinalIgnoreCase)) continue;
            var target = scene.GetWorldPosition(entity);
            if (Vector2.Distance(next, target) > Radius + 0.45f) continue;
            health.TakeDamage(Damage);
            Owner.Active = false;
            return;
        }

        scene.SetWorldPosition(Owner, next);
    }

    public static Entity Spawn(Scene scene, Vector2 position, Vector2 direction, float damage,
        Guid shooterId, string team, float speed = 14f, float radius = 0.25f, float lifetime = 3f,
        string? texturePath = null, Vector4? color = null)
    {
        var entity = scene.Create("Proyectil 2D", position);
        entity.Tags.Add("projectile");
        entity.AddComponent(new SpriteRenderer
        {
            TexturePath = texturePath,
            Size = new Vector2(radius * 2f),
            Tint = color ?? new Vector4(1f, 0.85f, 0.35f, 1f),
            SortingOrder = 20,
        });
        entity.AddComponent(new CircleCollider2D { Radius = radius, IsTrigger = true });
        entity.AddComponent(new Projectile2D
        {
            Speed = speed, Damage = damage, Radius = radius, Lifetime = lifetime,
            Team = team, ShooterId = shooterId,
            Direction = direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : Vector2.UnitX,
        });
        return entity;
    }

    public override Component CloneFor(Entity newOwner) => new Projectile2D
    {
        Enabled = Enabled, Speed = Speed, Damage = Damage, Lifetime = Lifetime, Radius = Radius,
        Team = Team, ShooterId = ShooterId, Direction = Direction, Owner = newOwner,
    };
}

/// <summary>Arma 2D que dispara Projectile2D al pulsar la acción attack.</summary>
public sealed class Weapon2D : Component, ITickable
{
    public float Damage { get; set; } = 10f;
    public float Cooldown { get; set; } = 0.25f;
    public float ProjectileSpeed { get; set; } = 14f;
    public float ProjectileLifetime { get; set; } = 3f;
    public float ProjectileRadius { get; set; } = 0.25f;
    public string Team { get; set; } = "player";
    public int Ammo { get; set; } = -1;
    public bool AimAtMouse { get; set; } = true;
    public float FireOffset { get; set; } = 0.5f;

    private float _cooldown;

    public void OnUpdate(float deltaTime)
    {
        _cooldown = MathF.Max(0f, _cooldown - deltaTime);
        if (Owner?.Scene is not { } scene || _cooldown > 0f) return;
        if (!Ollin.Core.Input.InputMap.IsDown("attack")) return;
        if (Ammo == 0) return;

        var position = scene.GetWorldPosition(Owner);
        var direction = AimAtMouse
            ? Ollin.Core.Input.InputState.MouseWorldPosition - position
            : new Vector2(Owner.Transform.Scale.X < 0f ? -1f : 1f, 0f);
        if (direction.LengthSquared() < 0.0001f) direction = Vector2.UnitX;
        direction = Vector2.Normalize(direction);

        Projectile2D.Spawn(scene, position + direction * FireOffset, direction, Damage,
            Owner.Id, Team, ProjectileSpeed, ProjectileRadius, ProjectileLifetime);
        if (Ammo > 0) Ammo--;
        _cooldown = MathF.Max(0.01f, Cooldown);
    }

    public override Component CloneFor(Entity newOwner) => new Weapon2D
    {
        Enabled = Enabled, Damage = Damage, Cooldown = Cooldown, ProjectileSpeed = ProjectileSpeed,
        ProjectileLifetime = ProjectileLifetime, ProjectileRadius = ProjectileRadius, Team = Team,
        Ammo = Ammo, AimAtMouse = AimAtMouse, FireOffset = FireOffset, Owner = newOwner,
    };
}
