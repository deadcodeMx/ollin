using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Controlador de personaje 2D para plataformas. Trabaja con Rigidbody2D y un
/// Collider2D: gravedad, salto, coyote time, buffer, pendientes, techo/pared y
/// arrastre por plataformas móviles.
/// </summary>
public sealed class CharacterBody2D : Component, ITickable
{
    public float MoveSpeed { get; set; } = 6f;

    public float Acceleration { get; set; } = 60f;

    public float Deceleration { get; set; } = 80f;

    public float JumpSpeed { get; set; } = 12f;

    public float Gravity { get; set; } = 30f;

    public float MaxFallSpeed { get; set; } = 25f;

    public float CoyoteTime { get; set; } = 0.1f;

    public float JumpBuffer { get; set; } = 0.12f;

    /// <summary>Ángulo máximo de pendiente que se puede caminar (grados).</summary>
    public float MaxSlopeAngle { get; set; } = 50f;

    /// <summary>Distancia de imán al suelo al bajar escalones.</summary>
    public float FloorSnapLength { get; set; } = 0.18f;

    /// <summary>Si está activo, el personaje no se mueve pero la gravedad sigue.</summary>
    public bool Frozen { get; set; }

    [JsonIgnore] public bool IsGrounded { get; private set; }

    [JsonIgnore] public bool IsOnWall { get; private set; }

    [JsonIgnore] public bool IsOnCeiling { get; private set; }

    [JsonIgnore] public Vector2 FloorNormal { get; private set; } = Vector2.UnitY;

    [JsonIgnore] public Vector2 PlatformVelocity { get; private set; }

    [JsonIgnore] public Guid? GroundPlatformId { get; private set; }

    [JsonIgnore] public bool WasGrounded { get; private set; }

    private float _coyote;
    private float _buffer;
    private bool _jumpHeld;
    private Vector2 _pendingCarry;

    public override Component CloneFor(Entity newOwner) => new CharacterBody2D
    {
        Enabled = Enabled,
        MoveSpeed = MoveSpeed,
        Acceleration = Acceleration,
        Deceleration = Deceleration,
        JumpSpeed = JumpSpeed,
        Gravity = Gravity,
        MaxFallSpeed = MaxFallSpeed,
        CoyoteTime = CoyoteTime,
        JumpBuffer = JumpBuffer,
        MaxSlopeAngle = MaxSlopeAngle,
        FloorSnapLength = FloorSnapLength,
        Frozen = Frozen,
        Owner = newOwner,
    };

    /// <summary>La física limpia los contactos al empezar cada subpaso.</summary>
    public void BeginPhysicsStep()
    {
        WasGrounded = IsGrounded;
        IsGrounded = false;
        IsOnWall = false;
        IsOnCeiling = false;
        FloorNormal = Vector2.UnitY;
        PlatformVelocity = Vector2.Zero;
    }

    /// <summary>La física avisa de un contacto sólido; normal apunta fuera del sólido.</summary>
    public void RegisterContact(Vector2 normal, Entity? other)
    {
        if (normal.Y > 0.3f && MathF.Acos(System.Math.Clamp(normal.Y, -1f, 1f)) * 180f / MathF.PI <= MaxSlopeAngle)
        {
            IsGrounded = true;
            FloorNormal = normal;
            if (other?.GetComponent<MovingPlatform2D>() is { Enabled: true })
            {
                GroundPlatformId = other.Id;
                PlatformVelocity = other.GetComponent<MovingPlatform2D>()!.Velocity;
            }
        }

        if (normal.Y < -0.3f) IsOnCeiling = true;
        if (MathF.Abs(normal.X) > 0.3f) IsOnWall = true;
    }

    public void OnUpdate(float deltaTime)
    {
        if (Owner is null || Frozen) return;
        var body = Owner.GetComponent<Rigidbody2D>();
        if (body is null || body.IsStatic) return;

        body.AffectedByGravity = false;

        // La gravedad puede separar un píxel del suelo; conserva el contacto un cuadro.
        if (!IsGrounded && WasGrounded && body.Velocity.Y <= 0.2f) IsGrounded = true;
        if (!IsGrounded && !WasGrounded) GroundPlatformId = null;

        // La plataforma móvil arrastra al pasajero antes de aplicar la velocidad.
        if (IsGrounded && GroundPlatformId is Guid platformId && Owner.Scene?.Find(platformId) is { } platformEntity)
        {
            if (platformEntity.GetComponent<MovingPlatform2D>() is { Enabled: true } platform)
                _pendingCarry += platform.DeltaThisFrame;
        }

        if (IsGrounded && _pendingCarry != Vector2.Zero)
        {
            Owner.Transform.Position += _pendingCarry;
            _pendingCarry = Vector2.Zero;
        }

        var velocity = body.Velocity;
        if (IsGrounded && velocity.Y <= 0.1f) SnapToFloor();
        _coyote = IsGrounded ? MathF.Max(0.001f, CoyoteTime) : MathF.Max(0f, _coyote - deltaTime);

        var left = InputMap.IsDown("move_left") || InputState.IsKeyDown(KeyCode.A) || InputState.IsKeyDown(KeyCode.Left);
        var right = InputMap.IsDown("move_right") || InputState.IsKeyDown(KeyCode.D) || InputState.IsKeyDown(KeyCode.Right);
        var direction = (right ? 1f : 0f) - (left ? 1f : 0f);
        var target = direction * MathF.Max(0f, MoveSpeed);
        var rate = direction == 0f ? MathF.Max(0.01f, Deceleration) : MathF.Max(0.01f, Acceleration);
        velocity.X = MoveTowards(velocity.X, target, rate * deltaTime);

        var jumpPressed = InputMap.WasPressed("jump") ||
                          InputState.WasKeyPressed(KeyCode.Space) ||
                          InputState.WasKeyPressed(KeyCode.W) ||
                          InputState.WasKeyPressed(KeyCode.Up);
        var jumpDown = InputMap.IsDown("jump") ||
                       InputState.IsKeyDown(KeyCode.Space) ||
                       InputState.IsKeyDown(KeyCode.W) ||
                       InputState.IsKeyDown(KeyCode.Up);

        if (jumpPressed) _buffer = MathF.Max(0.001f, JumpBuffer);
        else _buffer = MathF.Max(0f, _buffer - deltaTime);

        var jumped = false;
        if (_buffer > 0f && _coyote > 0f)
        {
            velocity.Y = MathF.Max(0f, JumpSpeed);
            _buffer = 0f;
            _coyote = 0f;
            IsGrounded = false;
            GroundPlatformId = null;
            jumped = true;
        }
        else
        {
            velocity.Y -= MathF.Max(0f, Gravity) * deltaTime;
            if (velocity.Y < -MathF.Abs(MaxFallSpeed)) velocity.Y = -MathF.Abs(MaxFallSpeed);
        }

        // Caminar por pendientes: proyecta la velocidad horizontal sobre el suelo.
        if (!jumped && IsGrounded && FloorNormal.Y > 0.25f && FloorNormal.Y < 0.98f && MathF.Abs(FloorNormal.X) > 0.01f)
            velocity.Y = -velocity.X * FloorNormal.X / MathF.Max(0.2f, FloorNormal.Y);

        // Salto variable: al soltar, se corta la subida.
        if (_jumpHeld && !jumpDown && velocity.Y > 0f) velocity.Y *= 0.5f;
        _jumpHeld = jumpDown;

        body.Velocity = velocity;
    }

    private void SnapToFloor()
    {
        if (Owner?.Scene is not { } scene || FloorSnapLength <= 0f || Owner.GetComponent<Collider2D>() is not { } self) return;
        var selfCorners = self.GetWorldCorners(scene.GetWorldMatrix(Owner));
        if (selfCorners.Length == 0) return;
        var selfMin = selfCorners[0];
        var selfMax = selfCorners[0];
        foreach (var corner in selfCorners) { selfMin = Vector2.Min(selfMin, corner); selfMax = Vector2.Max(selfMax, corner); }
        var bottom = selfMin.Y;
        var bestTop = float.MinValue;

        void Consider(IEnumerable<Vector2> corners)
        {
            var points = corners as Vector2[] ?? corners.ToArray();
            if (points.Length == 0) return;
            var min = points[0]; var max = points[0];
            foreach (var point in points) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
            if (max.X <= selfMin.X || min.X >= selfMax.X) return;
            if (max.Y > bottom + 0.05f || min.Y < bottom - FloorSnapLength) return;
            bestTop = MathF.Max(bestTop, max.Y);
        }

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.Id == Owner.Id) continue;
            if (entity.GetComponent<Collider2D>() is { Enabled: true, IsTrigger: false } collider)
                Consider(collider.GetWorldCorners(scene.GetWorldMatrix(entity)));
            if (entity.GetComponent<Tilemap>() is { Enabled: true, Collides: true } map)
            {
                for (var y = 0; y < map.Height; y++)
                    for (var x = 0; x < map.Width; x++)
                    {
                        if (map.TileAt(x, y) < 0) continue;
                        var origin = map.CellOrigin(x, y);
                        var size = new Vector2(map.TileSize);
                        var corners = new[]
                        {
                            Vector2.Transform(origin, scene.GetWorldMatrix(entity)),
                            Vector2.Transform(origin + new Vector2(size.X, 0f), scene.GetWorldMatrix(entity)),
                            Vector2.Transform(origin + size, scene.GetWorldMatrix(entity)),
                            Vector2.Transform(origin + new Vector2(0f, size.Y), scene.GetWorldMatrix(entity)),
                        };
                        Consider(corners);
                    }
            }
        }

        if (bestTop > float.MinValue)
            Owner.Transform.Position += new Vector2(0f, bestTop - bottom);
    }
    private static float MoveTowards(float current, float target, float maxDelta) =>
        MathF.Abs(target - current) <= maxDelta ? target :
        current + MathF.Sign(target - current) * maxDelta;
}
