using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Pathfinding;
using Ollin.Core.Scenes;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Agente sencillo: patrulla alrededor de su posición inicial y persigue al
/// objetivo cuando entra en su radio de visión. Usa A* si hay un tilemap sólido.
/// </summary>
public sealed class AiAgent : Component, ITickable
{
    public string TargetTag { get; set; } = "player";

    public float Speed { get; set; } = 2.5f;

    public float VisionRadius { get; set; } = 6f;

    public float PatrolRadius { get; set; } = 3f;

    public float PathRefreshSeconds { get; set; } = 0.45f;

    /// <summary>Segundos que sigue buscando al objetivo tras perderlo de vista.</summary>
    public float MemorySeconds { get; set; } = 3f;

    [JsonIgnore]
    public bool IsChasing { get; private set; }

    [JsonIgnore]
    public Vector2 Spawn { get; private set; }

    [JsonIgnore]
    public bool HasSpawn { get; private set; }

    private readonly List<GridPoint> _path = new();
    private int _pathIndex;
    private float _refresh;
    private float _patrolTimer;
    private Vector2 _patrolTarget;
    private float _memory;
    private Vector2 _lastKnown;

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
        var distance = target is null ? float.MaxValue : Vector2.Distance(position, target.Transform.Position);
        var visible = target is not null && distance <= VisionRadius;

        if (visible)
        {
            _lastKnown = target!.Transform.Position;
            _memory = MemorySeconds;
        }

        Vector2 desired;

        if (visible)
        {
            IsChasing = true;
            desired = _lastKnown;
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

            if (_patrolTimer <= 0f || Vector2.Distance(position, _patrolTarget) < 0.4f)
            {
                _patrolTarget = Spawn + new Vector2(
                    (Random.Shared.NextSingle() * 2f - 1f) * PatrolRadius,
                    (Random.Shared.NextSingle() * 2f - 1f) * PatrolRadius);
                _patrolTimer = 2.5f;
                _path.Clear();
            }

            desired = _patrolTarget;
        }

        _refresh -= deltaTime;

        if (_refresh <= 0f)
        {
            _refresh = System.Math.Max(0.1f, PathRefreshSeconds);
            RebuildPath(scene, position, desired);
        }

        var next = NextWaypoint(position, desired);
        var toNext = next - position;

        if (toNext.LengthSquared() < 0.0016f)
        {
            if (_pathIndex < _path.Count - 1)
            {
                _pathIndex++;
            }

            Stop();
            return;
        }

        var direction = Vector2.Normalize(toNext);
        var body = Owner.GetComponent<Rigidbody2D>();

        if (body is { IsStatic: false })
        {
            body.Velocity = direction * Speed;
        }
        else
        {
            Owner.Transform.Position += direction * Speed * deltaTime;
        }
    }

    private Entity? FindTarget(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (entity.Active &&
                entity.Tags.Any(tag => tag.Equals(TargetTag, StringComparison.OrdinalIgnoreCase)))
            {
                return entity;
            }
        }

        return null;
    }

    private void RebuildPath(Scene scene, Vector2 from, Vector2 to)
    {
        _path.Clear();
        _pathIndex = 0;

        var (mapEntity, map) = FindSolidTilemap(scene);

        if (mapEntity is null || map is null)
        {
            return;
        }

        var world = scene.GetWorldMatrix(mapEntity);

        if (!Matrix4x4.Invert(world, out var inverse))
        {
            return;
        }

        var (fromX, fromY) = map.WorldToCell(Vector2.Transform(from, inverse));
        var (toX, toY) = map.WorldToCell(Vector2.Transform(to, inverse));
        var fromCell = new GridPoint(fromX, fromY);
        var toCell = new GridPoint(toX, toY);

        bool Walkable(int x, int y) => map.IsWalkableAt(x, y);

        var path = GridPathfinder.FindPath(map.Width, map.Height, fromCell, toCell, Walkable);

        if (path is not null)
        {
            _path.AddRange(path);
        }
    }

    private (Entity? Entity, TileMapLayer? Map) FindSolidTilemap(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            foreach (var map in entity.GetComponents<TileMapLayer>())
            {
                if (map.Enabled && (map.Collides || map.TileSet is { Tiles.Count: > 0 }))
                    return (entity, map);
            }
        }
        return (null, null);
    }

    private Vector2 NextWaypoint(Vector2 position, Vector2 desired)
    {
        if (_path.Count == 0 || _pathIndex >= _path.Count)
        {
            return desired;
        }

        var scene = Owner?.Scene;

        if (scene is null)
        {
            return desired;
        }

        var (mapEntity, map) = FindSolidTilemap(scene);

        if (mapEntity is null || map is null)
        {
            return desired;
        }

        var cell = _path[_pathIndex];
        var world = scene.GetWorldMatrix(mapEntity);
        var size = map.TileSize;
        var center = Vector2.Transform(
            new Vector2((cell.X + 0.5f) * size, -(cell.Y + 0.5f) * size),
            world);

        if (Vector2.Distance(position, center) < 0.25f && _pathIndex < _path.Count - 1)
        {
            _pathIndex++;
            cell = _path[_pathIndex];
            center = Vector2.Transform(
                new Vector2((cell.X + 0.5f) * size, -(cell.Y + 0.5f) * size),
                world);
        }

        return center;
    }

    private void Stop()
    {
        if (Owner?.GetComponent<Rigidbody2D>() is { IsStatic: false } body)
        {
            body.Velocity = Vector2.Zero;
        }
    }

    public override Component CloneFor(Entity newOwner) => new AiAgent
    {
        Enabled = Enabled,
        TargetTag = TargetTag,
        Speed = Speed,
        VisionRadius = VisionRadius,
        PatrolRadius = PatrolRadius,
        PathRefreshSeconds = PathRefreshSeconds,
        MemorySeconds = MemorySeconds,
        Owner = newOwner,
    };
}
