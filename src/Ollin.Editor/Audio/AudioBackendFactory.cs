using Ollin.Core.Audio;

namespace Ollin.Editor.Audio;

/// <summary>Selecciona el backend de audio disponible en la plataforma actual.</summary>
public static class AudioBackendFactory
{
    public static IAudioBackend Create() =>
        OperatingSystem.IsWindows() ? new NAudioBackend() : new SilentAudioBackend();
}
