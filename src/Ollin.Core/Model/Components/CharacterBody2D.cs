using System.Numerics;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Controlador de personaje 2D para plataformas. Usa el Rigidbody2D y el
/// BoxCollider de la entidad para moverse y resolver colisiones.
/// Teclas por defecto: A/D o flechas, Espacio/W/Arriba para saltar.
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

    /// <summary>Si está activo, el personaje no se mueve pero la gravedad sigue.</summary>
    public bool Frozen { get; set; }

    public bool IsGrounded { get; private set; }

    private float _coyote;
    private float _buffer;
    private bool _jumpHeld;

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
        Frozen = Frozen,
        Owner = newOwner,
    };

    public void OnUpdate(float deltaTime)
    {
        if (Owner is null || Frozen) return;
        var body = Owner.GetComponent<Rigidbody2D>();
        if (body is null || body.IsStatic) return;

        body.AffectedByGravity = false;

        var velocity = body.Velocity;
        IsGrounded = MathF.Abs(velocity.Y) < 0.05f;
        _coyote = IsGrounded ? MathF.Max(0.001f, CoyoteTime) : MathF.Max(0f, _coyote - deltaTime);

        var left = InputState.IsKeyDown(KeyCode.A) || InputState.IsKeyDown(KeyCode.Left);
        var right = InputState.IsKeyDown(KeyCode.D) || InputState.IsKeyDown(KeyCode.Right);
        var direction = (right ? 1f : 0f) - (left ? 1f : 0f);
        var target = direction * MathF.Max(0f, MoveSpeed);
        var rate = direction == 0f ? MathF.Max(0.01f, Deceleration) : MathF.Max(0.01f, Acceleration);
        velocity.X = MoveTowards(velocity.X, target, rate * deltaTime);

        var jumpPressed = InputState.WasKeyPressed(KeyCode.Space) ||
                          InputState.WasKeyPressed(KeyCode.W) ||
                          InputState.WasKeyPressed(KeyCode.Up);
        var jumpDown = InputState.IsKeyDown(KeyCode.Space) ||
                       InputState.IsKeyDown(KeyCode.W) ||
                       InputState.IsKeyDown(KeyCode.Up);

        if (jumpPressed) _buffer = MathF.Max(0.001f, JumpBuffer);
        else _buffer = MathF.Max(0f, _buffer - deltaTime);

        if (_buffer > 0f && _coyote > 0f)
        {
            velocity.Y = MathF.Max(0f, JumpSpeed);
            _buffer = 0f;
            _coyote = 0f;
            IsGrounded = false;
        }
        else
        {
            velocity.Y -= MathF.Max(0f, Gravity) * deltaTime;
            if (velocity.Y < -MathF.Abs(MaxFallSpeed)) velocity.Y = -MathF.Abs(MaxFallSpeed);
        }

        // Salto variable: al soltar, se corta la subida.
        if (_jumpHeld && !jumpDown && velocity.Y > 0f) velocity.Y *= 0.5f;
        _jumpHeld = jumpDown;

        body.Velocity = velocity;
    }

    private static float MoveTowards(float current, float target, float maxDelta) =>
        MathF.Abs(target - current) <= maxDelta ? target :
        current + MathF.Sign(target - current) * maxDelta;
}
