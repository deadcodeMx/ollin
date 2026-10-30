using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Hace oscilar la entidad alrededor de su posición inicial (flotar, mecerse,
/// temblar). La posición base se captura en el primer cuadro de reproducción.
/// </summary>
public sealed class OscillatorComponent : Component, ITickable
{
    /// <summary>Dirección del movimiento; se normaliza al usarla.</summary>
    public Vector2 Axis { get; set; } = new(0f, 1f);

    /// <summary>Amplitud en unidades de mundo.</summary>
    public float Amplitude { get; set; } = 0.25f;

    /// <summary>Ciclos por segundo.</summary>
    public float Frequency { get; set; } = 1f;

    /// <summary>Desfase inicial en radianes.</summary>
    public float Phase { get; set; }

    private Vector2 _origin;
    private float _time;
    private bool _initialized;

    public void OnUpdate(float deltaTime)
    {
        if (Owner is null)
        {
            return;
        }

        if (!_initialized)
        {
            _origin = Owner.Transform.Position;
            _initialized = true;
        }

        _time += deltaTime;
        var axis = Axis.LengthSquared() > 0.0001f ? Vector2.Normalize(Axis) : Vector2.UnitY;
        var offset = MathF.Sin(_time * Frequency * MathF.Tau + Phase) * Amplitude;
        Owner.Transform.Position = _origin + axis * offset;
    }

    /// <summary>Vuelve a capturar la posición base (por ejemplo, al salir del modo juego).</summary>
    public void Reset()
    {
        _initialized = false;
        _time = 0f;
    }

    public override Component CloneFor(Entity newOwner) => new OscillatorComponent
    {
        Enabled = Enabled,
        Axis = Axis,
        Amplitude = Amplitude,
        Frequency = Frequency,
        Phase = Phase,
        Owner = newOwner,
    };
}
