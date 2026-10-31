using System.Numerics;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Controlador en primera persona para el modo 2.5D: ratón para mirar, WASD para
/// moverse y colisión contra cajas <see cref="Wall3D"/>.
/// </summary>
public sealed class FirstPersonController : Component, ITickable
{
    public float Speed { get; set; } = 4f;

    public float RunMultiplier { get; set; } = 1.75f;

    public float MouseSensitivityX { get; set; } = 0.0022f;

    public float MouseSensitivityY { get; set; } = 0.0022f;

    /// <summary>Compatibilidad con scripts antiguos: ajusta ambos ejes.</summary>
    public float MouseSensitivity
    {
        get => MouseSensitivityX;
        set
        {
            MouseSensitivityX = value;
            MouseSensitivityY = value;
        }
    }

    public bool InvertY { get; set; }

    /// <summary>0 = sin suavizado; valores altos suavizan el giro.</summary>
    public float LookSmoothing { get; set; }

    public float CollisionRadius { get; set; } = 0.35f;

    public float MinPitch { get; set; } = -1.2f;

    public float MaxPitch { get; set; } = 1.2f;

    public bool LookWithMouse { get; set; } = true;

    /// <summary>Ignora el ratón al arrancar para evitar giros por el salto del puntero.</summary>
    public float LookWarmupSeconds { get; set; } = 0.8f;

    private bool _lookInitialized;
    private float _lookWarmup;
    private float _targetYaw;
    private float _targetPitch;
    private bool _targetInitialized;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene || deltaTime <= 0f)
        {
            return;
        }

        var camera = Owner.GetComponent<Camera3D>() ?? scene.GetMainCamera3D();

        if (camera is null)
        {
            return;
        }

        if (!_lookInitialized)
        {
            _lookInitialized = true;
            _lookWarmup = MathF.Max(0f, LookWarmupSeconds);
        }

        if (!_targetInitialized)
        {
            _targetInitialized = true;
            _targetYaw = camera.Yaw;
            _targetPitch = camera.Pitch;
        }

        if (_lookWarmup > 0f)
        {
            _lookWarmup -= deltaTime;

            if (MouseLook.IsLocked)
            {
                MouseLook.ConsumeDelta();
            }
        }
        else if (LookWithMouse)
        {
            var delta = MouseLook.IsLocked ? MouseLook.ConsumeDelta() : InputState.MouseDelta;

            if (MouseLook.IsLocked || delta.LengthSquared() <= 40000f)
            {
                _targetYaw += delta.X * MouseSensitivityX;
                _targetPitch = System.Math.Clamp(
                    _targetPitch - delta.Y * MouseSensitivityY * (InvertY ? -1f : 1f),
                    MinPitch,
                    MaxPitch);
            }
        }

        if (LookSmoothing <= 0f)
        {
            camera.Yaw = _targetYaw;
            camera.Pitch = _targetPitch;
        }
        else
        {
            var alpha = 1f - MathF.Exp(-deltaTime * 30f * (1f - System.Math.Clamp(LookSmoothing, 0f, 0.95f)));
            camera.Yaw += (_targetYaw - camera.Yaw) * alpha;
            camera.Pitch += (_targetPitch - camera.Pitch) * alpha;
        }

        var forwardInput = 0f;
        var rightInput = 0f;

        if (InputState.IsKeyDown(KeyCode.W) || InputState.IsKeyDown(KeyCode.Up)) forwardInput += 1f;
        if (InputState.IsKeyDown(KeyCode.S) || InputState.IsKeyDown(KeyCode.Down)) forwardInput -= 1f;
        if (InputState.IsKeyDown(KeyCode.D) || InputState.IsKeyDown(KeyCode.Right)) rightInput += 1f;
        if (InputState.IsKeyDown(KeyCode.A) || InputState.IsKeyDown(KeyCode.Left)) rightInput -= 1f;

        if (forwardInput == 0f && rightInput == 0f)
        {
            return;
        }

        var forward = new Vector2(MathF.Sin(camera.Yaw), MathF.Cos(camera.Yaw));
        var right = new Vector2(MathF.Cos(camera.Yaw), -MathF.Sin(camera.Yaw));
        var direction = forward * forwardInput + right * rightInput;

        if (direction.LengthSquared() > 1f)
        {
            direction = Vector2.Normalize(direction);
        }

        var running = InputState.IsKeyDown(KeyCode.LeftShift) || InputState.IsKeyDown(KeyCode.RightShift);
        var speed = Speed * (running ? RunMultiplier : 1f);
        var move = direction * speed * deltaTime;

        var position = Owner.Transform.Position;
        var walls = CollectWalls(scene);

        position.X = MoveAxis(position, move.X, horizontal: true, camera, walls);
        position.Y = MoveAxis(position, move.Y, horizontal: false, camera, walls);
        position = CylinderCollision.Resolve(scene, position, CollisionRadius, camera.Height);
        Owner.Transform.Position = position;
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

    private float MoveAxis(Vector2 position, float delta, bool horizontal, Camera3D camera, List<Wall3D> walls)
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

            if (camera.Height < min.Y || camera.Height > max.Y)
            {
                continue;
            }

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

    public override Component CloneFor(Entity newOwner) => new FirstPersonController
    {
        Enabled = Enabled,
        Speed = Speed,
        RunMultiplier = RunMultiplier,
        MouseSensitivity = MouseSensitivity,
        CollisionRadius = CollisionRadius,
        MinPitch = MinPitch,
        MaxPitch = MaxPitch,
        LookWithMouse = LookWithMouse,
        Owner = newOwner,
    };
}
