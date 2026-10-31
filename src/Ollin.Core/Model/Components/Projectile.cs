using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Model;

namespace Ollin.Core.Model.Components;

/// <summary>Proyectil simple que avanza, choca con paredes y daña salud.</summary>
public sealed class Projectile : Component, ITickable
{
    public float Speed { get; set; } = 18f;

    public float Damage { get; set; } = 30f;

    public float Lifetime { get; set; } = 3f;

    public float Radius { get; set; } = 0.35f;

    public float Size { get; set; } = 0.25f;

    public Vector4 Color { get; set; } = new(1f, 0.85f, 0.35f, 1f);

    public string? TexturePath { get; set; }

    public Guid ShooterId { get; set; }

    [JsonIgnore]
    public Vector2 Direction { get; set; } = Vector2.UnitY;

    private float _age;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        _age += deltaTime;

        if (_age >= Lifetime)
        {
            Owner.Active = false;
            return;
        }

        var next = Owner.Transform.Position + Direction * Speed * deltaTime;

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Wall3D>() is not { Enabled: true, Visible: true, Collides: true } wall)
            {
                continue;
            }

            var (min, max) = wall.Bounds;

            if (next.X > min.X - Radius && next.X < max.X + Radius &&
                next.Y > min.Z - Radius && next.Y < max.Z + Radius)
            {
                Owner.Active = false;
                return;
            }
        }

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.Id == ShooterId ||
                entity.GetComponent<Health>() is not { IsDead: false } health)
            {
                continue;
            }

            var center = new Vector2(entity.Transform.Position.X, entity.Transform.Position.Y);

            if (Vector2.Distance(next, center) <= Radius + 0.5f)
            {
                health.TakeDamage(Damage);
                Owner.Active = false;
                return;
            }
        }

        Owner.Transform.Position = next;
    }

    public static Entity Spawn(
        Scenes.Scene scene,
        Vector2 position,
        Vector2 direction,
        float damage,
        Guid shooterId,
        float speed = 18f,
        float size = 0.25f,
        string? texturePath = null,
        Vector4? color = null)
    {
        var entity = scene.Create("Proyectil", position);
        entity.Tags.Add("projectile");
        entity.AddComponent(new SpriteRenderer
        {
            TexturePath = texturePath,
            Size = new Vector2(size, size),
            Tint = color ?? new Vector4(1f, 0.85f, 0.35f, 1f),
            SortingOrder = 20,
        });
        entity.AddComponent(new Projectile
        {
            Speed = speed,
            Damage = damage,
            ShooterId = shooterId,
            Direction = direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) : Vector2.UnitY,
            Size = size,
            TexturePath = texturePath,
            Color = color ?? new Vector4(1f, 0.85f, 0.35f, 1f),
        });
        return entity;
    }

    public override Component CloneFor(Entity newOwner) => new Projectile
    {
        Enabled = Enabled,
        Speed = Speed,
        Damage = Damage,
        Lifetime = Lifetime,
        Radius = Radius,
        Size = Size,
        Color = Color,
        TexturePath = TexturePath,
        ShooterId = ShooterId,
        Direction = Direction,
        Owner = newOwner,
    };
}
