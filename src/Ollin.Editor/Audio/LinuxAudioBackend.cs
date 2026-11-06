using System.Diagnostics;
using Ollin.Core.Audio;

namespace Ollin.Editor.Audio;

/// <summary>
/// Backend Linux que delega en paplay/aplay/ffplay. No hay control de volumen
/// por handle; se guarda para el siguiente arranque y se usa silencio si no hay player.
/// </summary>
public sealed class LinuxAudioBackend : IAudioBackend
{
    private sealed class Playback
    {
        public required Process Process { get; init; }
        public required string Path { get; init; }
        public required float Volume { get; init; }
        public required bool Loop { get; init; }
        public required AudioChannel Channel { get; init; }
    }

    private readonly Dictionary<int, Playback> _playbacks = new();
    private readonly object _gate = new();
    private readonly string? _player;
    private readonly SilentAudioBackend _fallback = new();
    private int _musicHandle = -1;
    private int _nextHandle = 1;

    public LinuxAudioBackend()
    {
        _player = FindPlayer();
    }

    private static string? FindPlayer()
    {
        foreach (var candidate in new[] { "paplay", "aplay", "ffplay" })
        {
            try
            {
                using var probe = Process.Start(new ProcessStartInfo
                {
                    FileName = "which",
                    ArgumentList = { candidate },
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                });
                if (probe is null) continue;
                probe.WaitForExit(500);
                if (probe.ExitCode == 0) return candidate;
            }
            catch { }
        }
        return null;
    }

    public int Play(string absolutePath, float volume, bool loop, AudioChannel channel)
    {
        if (_player is null) return _fallback.Play(absolutePath, volume, loop, channel);
        lock (_gate)
        {
            if (channel == AudioChannel.Music && _musicHandle >= 0)
            {
                StopLocked(_musicHandle);
                _musicHandle = -1;
            }
            var handle = _nextHandle++;
            var playback = Start(absolutePath, volume, loop, channel, handle);
            if (playback is null) return _fallback.Play(absolutePath, volume, loop, channel);
            _playbacks[handle] = playback;
            if (channel == AudioChannel.Music) _musicHandle = handle;
            return handle;
        }
    }

    private Playback? Start(string path, float volume, bool loop, AudioChannel channel, int handle)
    {
        try
        {
            var info = new ProcessStartInfo
            {
                FileName = _player!,
                UseShellExecute = false,
                CreateNoWindow = true,
            };
            if (_player == "ffplay")
            {
                info.ArgumentList.Add("-nodisp");
                info.ArgumentList.Add("-autoexit");
                info.ArgumentList.Add("-loglevel");
                info.ArgumentList.Add("quiet");
                info.ArgumentList.Add("-volume");
                info.ArgumentList.Add(((int)(volume * 100)).ToString());
            }
            info.ArgumentList.Add(path);

            var process = new Process { StartInfo = info, EnableRaisingEvents = true };
            process.Exited += (_, _) =>
            {
                lock (_gate)
                {
                    if (!_playbacks.TryGetValue(handle, out var current)) return;
                    if (!current.Loop) { _playbacks.Remove(handle); return; }
                }
                var restarted = Start(path, volume, loop, channel, handle);
                if (restarted is null) return;
                lock (_gate) _playbacks[handle] = restarted;
            };
            process.Start();
            return new Playback { Process = process, Path = path, Volume = volume, Loop = loop, Channel = channel };
        }
        catch
        {
            return null;
        }
    }

    private void StopLocked(int handle)
    {
        if (!_playbacks.Remove(handle, out var playback)) return;
        if (handle == _musicHandle) _musicHandle = -1;
        Kill(playback.Process);
    }

    public void Stop(int handle)
    {
        lock (_gate)
        {
            StopLocked(handle);
        }
    }

    public void StopAll()
    {
        lock (_gate)
        {
            foreach (var playback in _playbacks.Values) Kill(playback.Process);
            _playbacks.Clear();
            _musicHandle = -1;
        }
    }

    private static void Kill(Process process)
    {
        try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
        process.Dispose();
    }

    public void SetVolume(int handle, float volume) { /* paplay/aplay no permiten volumen por proceso */ }

    public void SetPan(int handle, float pan) { }

    public void Fade(int handle, float targetVolume, float seconds) { }
}
