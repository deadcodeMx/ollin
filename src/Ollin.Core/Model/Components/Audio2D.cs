using System.Numerics;
using System.Text.Json.Serialization;
using Ollin.Core.Audio;
using Ollin.Core.Scenes;

namespace Ollin.Core.Model.Components;

/// <summary>Marca la posición que escucha el audio 2D. Si no hay, se usa el jugador.</summary>
public sealed class AudioListener2D : Component
{
    public override Component CloneFor(Entity newOwner) => new AudioListener2D { Enabled = Enabled, Owner = newOwner };
}

/// <summary>Fuente de audio con atenuación por distancia al listener.</summary>
public sealed class AudioSource2D : Component, ITickable
{
    public string Clip { get; set; } = string.Empty;
    public float Volume { get; set; } = 1f;
    public bool Loop { get; set; }
    public bool PlayOnStart { get; set; } = true;
    public float MinDistance { get; set; } = 1f;
    public float MaxDistance { get; set; } = 12f;
    public AudioChannel Channel { get; set; } = AudioChannel.Effect;

    [JsonIgnore] public int Handle { get; private set; } = -1;

    private bool _started;

    public void OnUpdate(float deltaTime)
    {
        if (!_started)
        {
            _started = true;
            if (PlayOnStart) Play();
        }
        if (Handle < 0 || Owner?.Scene is not { } scene) return;
        var listener = FindListener(scene);
        if (listener is null) return;
        var sourcePosition = scene.GetWorldPosition(Owner);
        var listenerPosition = scene.GetWorldPosition(listener);
        var distance = Vector2.Distance(sourcePosition, listenerPosition);
        var range = MathF.Max(0.01f, MaxDistance - MinDistance);
        var attenuation = 1f - System.Math.Clamp((distance - MinDistance) / range, 0f, 1f);
        Audio.Audio.SetVolume(Handle, Volume * attenuation * Audio.Audio.MasterVolume * Audio.Audio.BusVolume(Channel));
        var pan = MaxDistance <= 0.01f ? 0f : System.Math.Clamp((sourcePosition.X - listenerPosition.X) / MaxDistance, -1f, 1f);
        Audio.Audio.SetPan(Handle, pan);
    }

    public int Play()
    {
        Stop();
        var handle = Audio.Audio.Play(Clip, Volume, Loop, Channel);
        Handle = handle;
        return handle;
    }

    public void Stop()
    {
        if (Handle >= 0) Audio.Audio.Stop(Handle);
        Handle = -1;
    }

    private static Entity? FindListener(Scene scene)
    {
        var explicitListener = scene.Entities.FirstOrDefault(entity => entity.Active && entity.GetComponent<AudioListener2D>() is not null);
        if (explicitListener is not null) return explicitListener;
        return scene.Entities.FirstOrDefault(entity => entity.Active && entity.Tags.Any(tag => tag.Equals("player", StringComparison.OrdinalIgnoreCase)));
    }

    public override Component CloneFor(Entity newOwner) => new AudioSource2D
    {
        Enabled = Enabled, Clip = Clip, Volume = Volume, Loop = Loop, PlayOnStart = PlayOnStart,
        MinDistance = MinDistance, MaxDistance = MaxDistance, Channel = Channel, Owner = newOwner,
    };
}
