using Ollin.Core.Diagnostics;

namespace Ollin.Core.Audio;

public enum AudioChannel
{
    Effect,
    Music,
}

/// <summary>Salida de audio concreta (NAudio en Windows, silencio en pruebas).</summary>
public interface IAudioBackend
{
    int Play(string absolutePath, float volume, bool loop, AudioChannel channel);

    void Stop(int handle);

    void StopAll();

    void SetVolume(int handle, float volume);

    void Fade(int handle, float targetVolume, float seconds);
}

/// <summary>
/// Fachada de audio para el editor y los scripts. Los archivos relativos se
/// resuelven contra <see cref="Root"/> (la carpeta del proyecto).
/// </summary>
public static class Audio
{
    private static readonly object Gate = new();
    private static IAudioBackend? _backend;
    private static float _masterVolume = 0.8f;

    /// <summary>Carpeta base para rutas relativas.</summary>
    public static string? Root { get; set; }

    public static bool Enabled => _backend is not null;

    public static float MasterVolume
    {
        get => _masterVolume;
        set => _masterVolume = System.Math.Clamp(value, 0f, 1f);
    }

    public static void Initialize(IAudioBackend backend)
    {
        lock (Gate)
        {
            _backend = backend;
        }
    }

    public static void Shutdown()
    {
        lock (Gate)
        {
            _backend?.StopAll();
            _backend = null;
        }
    }

    public static string Resolve(string path)
    {
        if (Path.IsPathRooted(path))
        {
            return path;
        }

        var root = Root ?? Environment.CurrentDirectory;
        return Path.GetFullPath(Path.Combine(root, path));
    }

    /// <summary>Reproduce un archivo (wav/mp3). Devuelve un identificador o -1.</summary>
    public static int Play(string clip, float volume = 1f, bool loop = false, AudioChannel channel = AudioChannel.Effect)
    {
        if (_backend is null || string.IsNullOrWhiteSpace(clip))
        {
            return -1;
        }

        var fullPath = Resolve(clip);
        if (!File.Exists(fullPath))
        {
            Log.Warn($"No existe el audio: {clip}", "Audio");
            return -1;
        }

        try
        {
            return _backend.Play(fullPath, System.Math.Clamp(volume, 0f, 1f), loop, channel);
        }
        catch (Exception exception)
        {
            Log.Warn($"No se pudo reproducir {clip}: {exception.Message}", "Audio");
            return -1;
        }
    }

    public static void Stop(int handle)
    {
        if (handle >= 0)
        {
            _backend?.Stop(handle);
        }
    }

    public static void StopAll() => _backend?.StopAll();

    public static void SetVolume(int handle, float volume) => _backend?.SetVolume(handle, System.Math.Clamp(volume, 0f, 1f));

    public static void Fade(int handle, float targetVolume, float seconds) =>
        _backend?.Fade(handle, System.Math.Clamp(targetVolume, 0f, 1f), System.Math.Max(0.01f, seconds));
}
