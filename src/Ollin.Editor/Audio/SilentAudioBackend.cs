using Ollin.Core.Audio;

namespace Ollin.Editor.Audio;

/// <summary>
/// Backend de audio sin salida. Permite que el editor y los juegos arranquen en
/// plataformas sin NAudio; el resto del motor sigue funcionando.
/// </summary>
public sealed class SilentAudioBackend : IAudioBackend
{
    private int _nextHandle = 1;

    public int Play(string absolutePath, float volume, bool loop, AudioChannel channel) => _nextHandle++;

    public void Stop(int handle) { }

    public void StopAll() { }

    public void SetVolume(int handle, float volume) { }

    public void Fade(int handle, float targetVolume, float seconds) { }
}
