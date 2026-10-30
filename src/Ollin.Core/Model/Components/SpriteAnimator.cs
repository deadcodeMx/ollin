using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Animación por hoja de sprites: recorta la textura en cuadros de tamaño fijo y
/// avanza a <see cref="Fps"/> cuadros por segundo. Escribe el recorte en el
/// <see cref="SpriteRenderer"/> de la misma entidad.
/// </summary>
public sealed class SpriteAnimator : Component, ITickable
{
    /// <summary>Textura de la hoja; si está vacía se usa la que ya tenga el sprite.</summary>
    public string? TexturePath { get; set; }

    public int FrameWidth { get; set; } = 32;

    public int FrameHeight { get; set; } = 32;

    /// <summary>Cuadros por fila de la hoja.</summary>
    public int Columns { get; set; } = 4;

    /// <summary>Total de cuadros de la animación (puede ser menor que la hoja).</summary>
    public int FrameCount { get; set; } = 4;

    public int StartFrame { get; set; }

    public float Fps { get; set; } = 8f;

    public bool Loop { get; set; } = true;

    public bool Playing { get; set; } = true;

    /// <summary>Multiplicador de velocidad (1 = normal).</summary>
    public float Speed { get; set; } = 1f;

    /// <summary>Cuadro que se está mostrando (solo lectura).</summary>
    public int CurrentFrame { get; private set; }

    public float TimeInClip { get; private set; }

    public override Component CloneFor(Entity newOwner) => new SpriteAnimator
    {
        Enabled = Enabled,
        TexturePath = TexturePath,
        FrameWidth = FrameWidth,
        FrameHeight = FrameHeight,
        Columns = Columns,
        FrameCount = FrameCount,
        StartFrame = StartFrame,
        Fps = Fps,
        Loop = Loop,
        Playing = Playing,
        Speed = Speed,
        Owner = newOwner,
    };

    public void OnUpdate(float deltaTime)
    {
        var sprite = Owner?.GetComponent<SpriteRenderer>();
        if (Owner is null || sprite is null)
        {
            return;
        }

        if (!string.IsNullOrWhiteSpace(TexturePath))
        {
            sprite.TexturePath = TexturePath;
        }

        if (!string.IsNullOrWhiteSpace(sprite.TexturePath))
        {
            sprite.Visible = true;
        }

        var frames = System.Math.Max(1, FrameCount);
        var columns = System.Math.Max(1, Columns);
        var fps = System.Math.Max(0.01f, Fps);

        if (Playing)
        {
            TimeInClip += deltaTime * System.Math.Max(0f, Speed);

            var advance = (int)(TimeInClip * fps);
            if (Loop)
            {
                CurrentFrame = (StartFrame + advance % frames) % frames;
            }
            else
            {
                CurrentFrame = System.Math.Min(StartFrame + advance, frames - 1);
                if (CurrentFrame >= frames - 1)
                {
                    Playing = false;
                }
            }
        }

        var frame = System.Math.Clamp(CurrentFrame, 0, frames - 1);
        var column = frame % columns;
        var row = frame / columns;
        sprite.FrameRect = new Vector4(column * FrameWidth, row * FrameHeight, FrameWidth, FrameHeight);
    }

    /// <summary>Reinicia la animación desde el principio.</summary>
    public void Restart()
    {
        TimeInClip = 0f;
        CurrentFrame = System.Math.Clamp(StartFrame, 0, System.Math.Max(1, FrameCount) - 1);
        Playing = true;
    }
}
