using NAudio.Wave;
using Ollin.Core.Audio;
using CoreAudio = Ollin.Core.Audio.Audio;
using Ollin.Core.Diagnostics;

namespace Ollin.Editor.Audio;

/// <summary>Reproducción de audio con NAudio (Windows). Cada sonido usa su propia salida.</summary>
internal sealed class NAudioBackend : IAudioBackend, IDisposable
{
    private sealed class Playback : IDisposable
    {
        public required AudioFileReader Reader { get; init; }

        public required WaveOutEvent Output { get; init; }

        public required int Handle { get; init; }

        public required bool Loop { get; init; }

        public void Dispose()
        {
            try
            {
                Output.Stop();
            }
            catch
            {
                // La salida pudo fallar al cerrarse.
            }

            Output.Dispose();
            Reader.Dispose();
        }
    }

    private readonly Dictionary<int, Playback> _playbacks = new();
    private readonly object _gate = new();
    private int _nextHandle = 1;
    private int _musicHandle = -1;
    private bool _disposed;

    public int Play(string absolutePath, float volume, bool loop, AudioChannel channel)
    {
        lock (_gate)
        {
            if (_disposed)
            {
                return -1;
            }

            if (channel == AudioChannel.Music && _musicHandle >= 0)
            {
                StopLocked(_musicHandle);
                _musicHandle = -1;
            }

            var reader = new AudioFileReader(absolutePath);
            reader.Volume = volume * CoreAudio.MasterVolume;

            var output = new WaveOutEvent { DesiredLatency = 120 };
            var handle = _nextHandle++;
            var playback = new Playback { Reader = reader, Output = output, Handle = handle, Loop = loop };

            output.Init(reader);
            output.PlaybackStopped += (_, _) =>
            {
                if (loop)
                {
                    try
                    {
                        reader.Position = 0;
                        output.Play();
                    }
                    catch
                    {
                        Remove(handle);
                    }
                }
                else
                {
                    Remove(handle);
                }
            };

            _playbacks[handle] = playback;
            output.Play();

            if (channel == AudioChannel.Music)
            {
                _musicHandle = handle;
            }

            return handle;
        }
    }

    public void Stop(int handle)
    {
        lock (_gate)
        {
            StopLocked(handle);
        }
    }

    private void StopLocked(int handle)
    {
        if (!_playbacks.Remove(handle, out var playback))
        {
            return;
        }

        if (handle == _musicHandle)
        {
            _musicHandle = -1;
        }

        playback.Dispose();
    }

    public void StopAll()
    {
        lock (_gate)
        {
            foreach (var playback in _playbacks.Values)
            {
                playback.Dispose();
            }

            _playbacks.Clear();
            _musicHandle = -1;
        }
    }

    public void SetVolume(int handle, float volume)
    {
        lock (_gate)
        {
            if (_playbacks.TryGetValue(handle, out var playback))
            {
                playback.Reader.Volume = volume * CoreAudio.MasterVolume;
            }
        }
    }

    public void Fade(int handle, float targetVolume, float seconds)
    {
        _ = Task.Run(async () =>
        {
            float start;
            lock (_gate)
            {
                if (!_playbacks.TryGetValue(handle, out var playback))
                {
                    return;
                }

                start = playback.Reader.Volume / System.Math.Max(0.0001f, CoreAudio.MasterVolume);
            }

            var steps = System.Math.Max(1, (int)(seconds * 20));
            var delay = (int)(seconds * 1000 / steps);

            for (var step = 1; step <= steps; step++)
            {
                var value = start + (targetVolume - start) * step / steps;
                SetVolume(handle, value);

                try
                {
                    await Task.Delay(delay);
                }
                catch
                {
                    return;
                }
            }
        });
    }

    private void Remove(int handle)
    {
        lock (_gate)
        {
            if (!_playbacks.Remove(handle, out var playback))
            {
                return;
            }

            if (handle == _musicHandle)
            {
                _musicHandle = -1;
            }

            playback.Dispose();
        }
    }

    public void Dispose()
    {
        lock (_gate)
        {
            _disposed = true;
        }

        StopAll();
    }
}
