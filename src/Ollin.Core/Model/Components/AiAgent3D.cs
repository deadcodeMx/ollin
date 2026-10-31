using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Navigation;

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

    public bool UseNavigation { get; set; } = true;

    public float NavCellSize { get; set; } = 0.5f;

    public float NavRefreshSeconds { get; set; } = 0.6f;

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
    private NavGrid3D? _nav;
    private List<Vector2>? _path;
    private int _pathIndex;
    private Vector2 _pathTarget;
    private float _navTimer;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene || deltaTime <= 0f)
        {
            return;
        }

        if (!HasSpawn)
        {
            Spawn = scene.GetWorldPosition(Owner);
            _patrolTarget = Spawn;
            HasSpawn = true;
        }

        var position = scene.GetWorldPosition(Owner);
        var target = FindTarget(scene);
        var targetPosition = target is null ? position : scene.GetWorldPosition(target);
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

        if (visible)
        {
            _path = null;
        }
        else if (UseNavigation)
        {
            _navTimer -= deltaTime;

            if (_nav is null || _navTimer <= 0f)
            {
                _nav = NavGrid3D.Build(scene, NavCellSize, CollisionRadius);
                _navTimer = MathF.Max(0.1f, NavRefreshSeconds);
                _path = null;
            }

            if (_path is null || _pathIndex >= _path.Count || Vector2.Distance(_pathTarget, desired) > 0.6f)
            {
                _path = _nav.TryFindPath(position, desired, out var found) ? found : null;
                _pathIndex = 0;
                _pathTarget = desired;
            }

            while (_path is not null && _pathIndex < _path.Count &&
                   Vector2.Distance(position, _path[_pathIndex]) < 0.35f)
            {
                _pathIndex++;
            }

            if (_path is not null && _pathIndex < _path.Count)
            {
                desired = _path[_pathIndex];
            }
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
        next = CylinderCollision.Resolve(scene, next, CollisionRadius, 1.1f);
        scene.SetWorldPosition(Owner, next);

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
        UseNavigation = UseNavigation,
        NavCellSize = NavCellSize,
        NavRefreshSeconds = NavRefreshSeconds,
        Owner = newOwner,
    };
}
