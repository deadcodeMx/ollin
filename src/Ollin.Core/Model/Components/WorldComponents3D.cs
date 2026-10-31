using System.Numerics;
using Ollin.Core.Hud;

namespace Ollin.Core.Model.Components;

/// <summary>Zona de daño (lava, pinchos, veneno) que daña al jugador por segundo.</summary>
public sealed class Hazard3D : Component, ITickable
{
    public float Radius { get; set; } = 1.5f;

    public float DamagePerSecond { get; set; } = 20f;

    public string PlayerTag { get; set; } = "player";

    private float _timer;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        _timer -= deltaTime;

        if (_timer > 0f)
        {
            return;
        }

        _timer = 0.5f;
        var player = ActionExecutor.FindPlayer(scene, PlayerTag);

        if (player is null ||
            Vector2.Distance(Owner.Transform.Position, player.Transform.Position) > Radius)
        {
            return;
        }

        player.GetComponent<Health>()?.TakeDamage(DamagePerSecond * 0.5f);
    }

    public override Component CloneFor(Entity newOwner) => new Hazard3D
    {
        Enabled = Enabled,
        Radius = Radius,
        DamagePerSecond = DamagePerSecond,
        PlayerTag = PlayerTag,
        Owner = newOwner,
    };
}

/// <summary>Teletransporta al jugador a otra entidad al entrar en su radio.</summary>
public sealed class Teleporter3D : Component, ITickable
{
    public float Radius { get; set; } = 1.5f;

    public string TargetName { get; set; } = string.Empty;

    public string Message { get; set; } = "Teletransportado";

    public float CooldownSeconds { get; set; } = 1.5f;

    public string PlayerTag { get; set; } = "player";

    private float _cooldown;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        if (_cooldown > 0f)
        {
            _cooldown -= deltaTime;
            return;
        }

        var player = ActionExecutor.FindPlayer(scene, PlayerTag);

        if (player is null || Vector2.Distance(Owner.Transform.Position, player.Transform.Position) > Radius)
        {
            return;
        }

        var target = string.IsNullOrWhiteSpace(TargetName) ? null : scene.Find(TargetName);

        if (target is null)
        {
            return;
        }

        player.Transform.Position = target.Transform.Position;
        _cooldown = MathF.Max(0.2f, CooldownSeconds);

        if (!string.IsNullOrWhiteSpace(Message))
        {
            HudHost.ShowMessage(Message, 1.5f);
        }
    }

    public override Component CloneFor(Entity newOwner) => new Teleporter3D
    {
        Enabled = Enabled,
        Radius = Radius,
        TargetName = TargetName,
        Message = Message,
        CooldownSeconds = CooldownSeconds,
        PlayerTag = PlayerTag,
        Owner = newOwner,
    };
}

/// <summary>Compañero que sigue al jugador manteniendo una distancia.</summary>
public sealed class Follower3D : Component, ITickable
{
    public string TargetTag { get; set; } = "player";

    public float Speed { get; set; } = 2.5f;

    public float FollowDistance { get; set; } = 2f;

    public float CollisionRadius { get; set; } = 0.35f;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        var target = ActionExecutor.FindPlayer(scene, TargetTag);

        if (target is null)
        {
            return;
        }

        var toTarget = target.Transform.Position - Owner.Transform.Position;

        if (toTarget.Length() <= FollowDistance)
        {
            return;
        }

        var direction = Vector2.Normalize(toTarget);
        var step = direction * Speed * deltaTime;
        var next = Owner.Transform.Position;
        var walls = new List<Wall3D>();

        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.GetComponent<Wall3D>() is { Enabled: true, Collides: true, Visible: true } wall)
            {
                walls.Add(wall);
            }
        }

        next.X = MoveAxis(next, step.X, horizontal: true, walls);
        next.Y = MoveAxis(next, step.Y, horizontal: false, walls);
        next = CylinderCollision.Resolve(scene, next, CollisionRadius, 1.1f);
        Owner.Transform.Position = next;
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

    public override Component CloneFor(Entity newOwner) => new Follower3D
    {
        Enabled = Enabled,
        TargetTag = TargetTag,
        Speed = Speed,
        FollowDistance = FollowDistance,
        CollisionRadius = CollisionRadius,
        Owner = newOwner,
    };
}
