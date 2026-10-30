using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Ciclo de día y noche. Avanza <see cref="TimeOfDay"/> de 0 (medianoche) a 1 y
/// calcula la luz ambiente que el runtime aplica a las cámaras.
/// </summary>
public sealed class DayNightCycle : Component, ITickable
{
    /// <summary>Duración de un día completo en segundos.</summary>
    public float DayLengthSeconds { get; set; } = 120f;

    /// <summary>Hora normalizada: 0 = medianoche, 0.5 = mediodía.</summary>
    public float TimeOfDay { get; set; } = 0.3f;

    public bool Paused { get; set; }

    public Vector4 DayAmbient { get; set; } = new(1f, 0.97f, 0.9f, 1f);

    public Vector4 NightAmbient { get; set; } = new(0.16f, 0.18f, 0.28f, 1f);

    /// <summary>Hora en formato 0–24 (para HUD o scripts).</summary>
    public float Hour => TimeOfDay * 24f;

    /// <summary>0 = noche cerrada, 1 = pleno día.</summary>
    public float DayFactor => 0.5f - 0.5f * MathF.Cos(TimeOfDay * MathF.Tau);

    public Vector4 AmbientNow => Vector4.Lerp(NightAmbient, DayAmbient, DayFactor);

    public void OnUpdate(float deltaTime)
    {
        if (Paused || DayLengthSeconds <= 0f)
        {
            return;
        }

        TimeOfDay = (TimeOfDay + deltaTime / DayLengthSeconds) % 1f;

        if (TimeOfDay < 0f)
        {
            TimeOfDay += 1f;
        }
    }

    public override Component CloneFor(Entity newOwner) => new DayNightCycle
    {
        Enabled = Enabled,
        DayLengthSeconds = DayLengthSeconds,
        TimeOfDay = TimeOfDay,
        Paused = Paused,
        DayAmbient = DayAmbient,
        NightAmbient = NightAmbient,
        Owner = newOwner,
    };
}
