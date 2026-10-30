using Ollin.Core.Model;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Fuente de sonido de una entidad. En reproducción arranca sola si
/// <see cref="PlayOnStart"/> está activo; los scripts también pueden llamarla.
/// </summary>
public sealed class AudioSource : Component
{
    public string? Clip { get; set; }

    public float Volume { get; set; } = 1f;

    public bool Loop { get; set; }

    public bool PlayOnStart { get; set; } = true;

    /// <summary>Si es música, corta la anterior al empezar.</summary>
    public bool Music { get; set; }

    public float FadeIn { get; set; }

    public override Component CloneFor(Entity newOwner) => new AudioSource
    {
        Enabled = Enabled,
        Clip = Clip,
        Volume = Volume,
        Loop = Loop,
        PlayOnStart = PlayOnStart,
        Music = Music,
        FadeIn = FadeIn,
        Owner = newOwner,
    };
}
