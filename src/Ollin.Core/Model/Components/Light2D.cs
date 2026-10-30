using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

public enum LightKind
{
    Point,
    Cone,
}

/// <summary>
/// Luz 2D. Se dibuja como un degradado aditivo sobre la escena: las puntuales
/// iluminan en todas direcciones y los conos en la dirección de la entidad.
/// </summary>
public sealed class Light2D : Component, ITickable
{
    public LightKind Kind { get; set; } = LightKind.Point;

    public Vector4 Color { get; set; } = new(1f, 0.94f, 0.82f, 1f);

    public float Radius { get; set; } = 4.5f;

    public float Intensity { get; set; } = 1f;

    /// <summary>Ángulo total del cono en grados (solo para <see cref="LightKind.Cone"/>).</summary>
    public float ConeAngle { get; set; } = 60f;

    /// <summary>Variación de intensidad (0 = luz fija).</summary>
    public float FlickerAmplitude { get; set; }

    public float FlickerSpeed { get; set; } = 7f;

    /// <summary>Intensidad del frame; el renderizador usa <see cref="Intensity"/> si es 0.</summary>
    [JsonIgnore]
    public float CurrentIntensity { get; set; }

    [JsonIgnore]
    public float Time { get; private set; }

    public override Component CloneFor(Entity newOwner) => new Light2D
    {
        Enabled = Enabled,
        Kind = Kind,
        Color = Color,
        Radius = Radius,
        Intensity = Intensity,
        ConeAngle = ConeAngle,
        FlickerAmplitude = FlickerAmplitude,
        FlickerSpeed = FlickerSpeed,
        Owner = newOwner,
    };

    public void OnUpdate(float deltaTime)
    {
        Time += deltaTime;

        if (FlickerAmplitude <= 0f)
        {
            CurrentIntensity = Intensity;
            return;
        }

        var wave = MathF.Sin(Time * FlickerSpeed) * 0.6f +
                   MathF.Sin(Time * FlickerSpeed * 2.37f) * 0.4f;

        CurrentIntensity = System.Math.Clamp(Intensity * (1f + wave * FlickerAmplitude), 0f, 2.5f);
    }
}
