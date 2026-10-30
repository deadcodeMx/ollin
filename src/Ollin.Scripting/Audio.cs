using CoreAudio = Ollin.Core.Audio.Audio;
using Ollin.Core.Audio;

namespace Ollin.Scripting;

/// <summary>Audio para scripts: efectos y música del proyecto.</summary>
public static class Audio
{
    public static float MasterVolume
    {
        get => CoreAudio.MasterVolume;
        set => CoreAudio.MasterVolume = value;
    }

    /// <summary>Reproduce un efecto una vez (o en bucle).</summary>
    public static void Play(string clip, float volume = 1f, bool loop = false) =>
        CoreAudio.Play(clip, volume, loop, AudioChannel.Effect);

    /// <summary>Música de fondo: corta la anterior.</summary>
    public static void Music(string clip, float volume = 0.6f, bool loop = true) =>
        CoreAudio.Play(clip, volume, loop, AudioChannel.Music);

    public static void StopAll() => CoreAudio.StopAll();
}
