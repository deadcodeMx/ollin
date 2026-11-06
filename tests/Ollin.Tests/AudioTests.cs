using System.Numerics;
using Ollin.Core.Audio;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class AudioMixTests
{
    private sealed class FakeBackend : IAudioBackend
    {
        public readonly Dictionary<int, float> Volumes = new();
        public readonly Dictionary<int, float> Pans = new();
        public int LastHandle;
        public int StopCalls;

        public int Play(string absolutePath, float volume, bool loop, AudioChannel channel)
        {
            LastHandle++;
            Volumes[LastHandle] = volume;
            return LastHandle;
        }

        public void Stop(int handle) => StopCalls++;
        public void StopAll() { }
        public void SetVolume(int handle, float volume) => Volumes[handle] = volume;
        public void SetPan(int handle, float pan) => Pans[handle] = pan;
        public void Fade(int handle, float targetVolume, float seconds) => Volumes[handle] = targetVolume;
    }

    [Fact]
    public void PlayAplicaMasterYBusSinDuplicar()
    {
        var backend = new FakeBackend();
        var root = Path.Combine(Path.GetTempPath(), $"ollin-audio-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        File.WriteAllBytes(Path.Combine(root, "beep.wav"), new byte[] { 1 });
        Audio.Initialize(backend);
        Audio.Root = root;
        Audio.MasterVolume = 0.5f;
        Audio.EffectsVolume = 0.4f;
        Audio.MusicVolume = 1f;

        try
        {
            Audio.Play("beep.wav", 0.5f, channel: AudioChannel.Effect);
            Assert.Equal(0.1f, backend.Volumes[backend.LastHandle], 3);
        }
        finally
        {
            Audio.Shutdown();
            Audio.Root = null;
            Directory.Delete(root, true);
        }
    }

    [Fact]
    public void AudioSource2DAtenuaYDesplazaElPan()
    {
        var backend = new FakeBackend();
        var scene = new Scene();
        var listener = scene.Create("Jugador", new Vector2(5f, 0f));
        listener.Tags.Add("player");
        var sourceEntity = scene.Create("Fuente", Vector2.Zero);
        var path = Path.Combine(Path.GetTempPath(), $"ollin-audio-{Guid.NewGuid():N}.wav");
        File.WriteAllBytes(path, new byte[] { 1 });
        var source = sourceEntity.AddComponent(new AudioSource2D
        {
            Clip = path, Volume = 1f, MinDistance = 1f, MaxDistance = 10f,
        });
        Audio.Initialize(backend);
        Audio.Root = null;
        Audio.MasterVolume = 1f;
        Audio.EffectsVolume = 1f;

        try
        {
            source.OnUpdate(0.016f);
            source.OnUpdate(0.016f);
            Assert.True(backend.Volumes[source.Handle] > 0.4f);
            Assert.True(backend.Pans[source.Handle] < 0f);
        }
        finally
        {
            Audio.Shutdown();
            File.Delete(path);
        }
    }
}
