using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Agente del modo 2.5D: patrulla alrededor de su origen, ve al objetivo por
/// distancia y línea de vista, lo persigue y recuerda su última posición.
/// Se mueve en el plano X/Z y colisiona con cajas <see cref="Wall3D"/>.
/// </summary>
public sealed class AiAgent3D : Component, ITickable
{
    public string TargetTag { get; set; } = "player";

    public float Speed { get; set; } = 1.6f;

    public float VisionRadius { get; set; } = 8f;

    public float PatrolRadius { get; set; } = 3f;

    public float MemorySeconds { get; set; } = 3f;

    public float CollisionRadius { get; set; } = 0.4f;

    public float AttackDamage { get; set; }

    public float AttackRange { get; set; } = 1.5f;

    public float AttackCooldown { get; set; } = 1.2f;

    [JsonIgnore]
    public bool IsChasing { get; private set; }

    [JsonIgnore]
    public Vector2 Spawn { get; private set; }

    [JsonIgnore]
    public bool HasSpawn { get; private set; }

    private float _memory;
    private float _patrolTimer;
    private Vector2 _lastKnown;
    private Vector2 _patrolTarget;
    private float _attackTimer;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene || deltaTime <= 0f)
        {
            return;
        }

        if (!HasSpawn)
        {
            Spawn = Owner.Transform.Position;
            _patrolTarget = Spawn;
            HasSpawn = true;
        }

        var position = Owner.Transform.Position;
        var target = FindTarget(scene);
        var targetPosition = target?.Transform.Position ?? position;
        var distance = target is null ? float.MaxValue : Vector2.Distance(position, targetPosition);
        var visible = target is not null &&
                      distance <= VisionRadius &&
                      HasLineOfSight(scene, position, targetPosition);

        if (visible)
        {
            _lastKnown = targetPosition;
            _memory = MemorySeconds;
        }

        Vector2 desired;

        if (visible)
        {
            IsChasing = true;
            desired = targetPosition;
        }
        else if (_memory > 0f)
        {
            IsChasing = true;
            _memory -= deltaTime;
            desired = _lastKnown;
        }
        else
        {
            IsChasing = false;
            _patrolTimer -= deltaTime;

            if (_patrolTimer <= 0f || Vector2.Distance(position, _patrolTarget) < 0.3f)
            {
                _patrolTarget = Spawn + new Vector2(
                    (Random.Shared.NextSingle() * 2f - 1f) * PatrolRadius,
                    (Random.Shared.NextSingle() * 2f - 1f) * PatrolRadius);
                _patrolTimer = 3f;
            }

            desired = _patrolTarget;
        }

        var toTarget = desired - position;

        if (toTarget.LengthSquared() < 0.0025f)
        {
            return;
        }

        var direction = Vector2.Normalize(toTarget);
        var step = direction * Speed * deltaTime;
        var walls = CollectWalls(scene);

        var next = position;
        next.X = MoveAxis(next, step.X, horizontal: true, walls);
        next.Y = MoveAxis(next, step.Y, horizontal: false, walls);
        Owner.Transform.Position = next;

        _attackTimer -= deltaTime;

        if (target is not null && AttackDamage > 0f && _attackTimer <= 0f &&
            distance <= AttackRange && HasLineOfSight(scene, next, targetPosition))
        {
            target.GetComponent<Health>()?.TakeDamage(AttackDamage);
            _attackTimer = MathF.Max(0.1f, AttackCooldown);
        }
    }

    private Entity? FindTarget(Scenes.Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.Tags.Any(tag => tag.Equals(TargetTag, StringComparison.OrdinalIgnoreCase)))
            {
                return entity;
            }
        }

        return null;
    }

    private static List<Wall3D> CollectWalls(Scenes.Scene scene)
    {
        var walls = new List<Wall3D>();

        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.GetComponent<Wall3D>() is { Enabled: true, Collides: true, Visible: true } wall)
            {
                walls.Add(wall);
            }
        }

        return walls;
    }

    private float MoveAxis(Vector2 position, float delta, bool horizontal, List<Wall3D> walls)
    {
        if (MathF.Abs(delta) < 0.00001f)
        {
            return horizontal ? position.X : position.Y;
        }

        var value = horizontal ? position.X : position.Y;
        var other = horizontal ? position.Y : position.X;
        var radius = MathF.Max(0.05f, CollisionRadius);
        var next = value + delta;

        foreach (var wall in walls)
        {
            var (min, max) = wall.Bounds;
            var minAxis = horizontal ? min.X : min.Z;
            var maxAxis = horizontal ? max.X : max.Z;
            var minOther = horizontal ? min.Z : min.X;
            var maxOther = horizontal ? max.Z : max.X;

            if (next + radius <= minAxis || next - radius >= maxAxis)
            {
                continue;
            }

            if (other + radius <= minOther || other - radius >= maxOther)
            {
                continue;
            }

            next = delta > 0f ? minAxis - radius : maxAxis + radius;
        }

        return next;
    }

    private static bool HasLineOfSight(Scenes.Scene scene, Vector2 from, Vector2 to)
    {
        const int samples = 12;

        for (var i = 1; i < samples; i++)
        {
            var t = i / (float)samples;
            var point = Vector2.Lerp(from, to, t);

            foreach (var entity in scene.Entities)
            {
                if (!entity.Active || entity.GetComponent<Wall3D>() is not { Enabled: true, Visible: true, Collides: true } wall)
                {
                    continue;
                }

                var (min, max) = wall.Bounds;

                if (point.X > min.X && point.X < max.X && point.Y > min.Z && point.Y < max.Z)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public override Component CloneFor(Entity newOwner) => new AiAgent3D
    {
        Enabled = Enabled,
        TargetTag = TargetTag,
        Speed = Speed,
        VisionRadius = VisionRadius,
        PatrolRadius = PatrolRadius,
        MemorySeconds = MemorySeconds,
        CollisionRadius = CollisionRadius,
        AttackDamage = AttackDamage,
        AttackRange = AttackRange,
        AttackCooldown = AttackCooldown,
        Owner = newOwner,
    };
}
