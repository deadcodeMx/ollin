using System.Numerics;
using CoreAudio = Ollin.Core.Audio.Audio;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

public enum AnimationEasing
{
    Linear,
    Smooth,
    EaseIn,
    EaseOut,
    Step,
}

public sealed class AnimationKeyframe
{
    public float Time { get; set; }

    public Vector2 Value { get; set; }

    public float Number { get; set; }

    public string? Text { get; set; }

    public AnimationEasing Easing { get; set; } = AnimationEasing.Linear;

    /// <summary>Tangente de salida para interpolación Bézier/Hermite.</summary>
    public Vector2 OutTangent { get; set; }

    /// <summary>Tangente de entrada para interpolación Bézier/Hermite.</summary>
    public Vector2 InTangent { get; set; }
}

public sealed class AnimationTrack
{
    /// <summary>position, rotation, scale, frame, audio, method.</summary>
    public string Property { get; set; } = "position";

    public List<AnimationKeyframe> Keys { get; set; } = new();
}

public sealed class AnimationTimeline
{
    public string Name { get; set; } = "clip";

    public float Duration { get; set; } = 1f;

    public bool Loop { get; set; } = true;

    public List<AnimationTrack> Tracks { get; set; } = new();
}

/// <summary>Reproductor de timelines con tracks de transformación, sprite y audio.</summary>
public sealed class AnimationPlayer2D : Component, ITickable
{
    public List<AnimationTimeline> Clips { get; set; } = new();
    public string? PlayOnStart { get; set; }
    public float Speed { get; set; } = 1f;

    [JsonIgnore] public string? Current { get; private set; }
    [JsonIgnore] public bool Playing { get; private set; }
    [JsonIgnore] public float Time { get; private set; }
    [JsonIgnore] public bool Looping { get; private set; }

    private bool _started;
    private float _fadeRemaining;
    private float _fadeDuration;
    private Vector2 _fadePosition;
    private float _fadeRotation;
    private Vector2 _fadeScale;

    /// <summary>Evento disparado por un track con Property = "event".</summary>
    public event Action<string>? Event;
    private float _lastTime;

    public void OnUpdate(float deltaTime)
    {
        if (!_started)
        {
            _started = true;
            if (!string.IsNullOrWhiteSpace(PlayOnStart)) Play(PlayOnStart!);
        }
        if (!Playing || Owner is null) return;

        _lastTime = Time;
        Time += deltaTime * MathF.Max(0f, Speed);
        var clip = Clips.FirstOrDefault(item => item.Name == Current);
        if (clip is null) { Playing = false; return; }

        Evaluate(clip);
        if (_fadeRemaining > 0f && Owner is not null)
        {
            _fadeRemaining = MathF.Max(0f, _fadeRemaining - deltaTime);
            var t = 1f - System.Math.Clamp(_fadeRemaining / MathF.Max(0.001f, _fadeDuration), 0f, 1f);
            Owner.Transform.Position = Vector2.Lerp(_fadePosition, Owner.Transform.Position, t);
            Owner.Transform.Rotation = _fadeRotation + (Owner.Transform.Rotation - _fadeRotation) * t;
            Owner.Transform.Scale = Vector2.Lerp(_fadeScale, Owner.Transform.Scale, t);
        }
        if (Time >= clip.Duration)
        {
            if (clip.Loop) { Time %= MathF.Max(0.001f, clip.Duration); _lastTime = 0f; }
            else { Time = clip.Duration; Playing = false; }
        }
    }

    public void Play(string name, float fadeSeconds = 0f)
    {
        if (Clips.All(clip => clip.Name != name)) return;
        if (fadeSeconds > 0f && Owner is not null)
        {
            _fadePosition = Owner.Transform.Position;
            _fadeRotation = Owner.Transform.Rotation;
            _fadeScale = Owner.Transform.Scale;
            _fadeDuration = fadeSeconds;
            _fadeRemaining = fadeSeconds;
        }
        Current = name;
        Time = 0f;
        _lastTime = 0f;
        Playing = true;
        Looping = Clips.First(clip => clip.Name == name).Loop;
    }

    public void CrossFade(string name, float seconds) => Play(name, seconds);

    public void Stop() { Playing = false; }
    public void Resume() { Playing = true; }

    private void Evaluate(AnimationTimeline clip)
    {
        foreach (var track in clip.Tracks)
        {
            if (track.Keys.Count == 0) continue;
            var keys = track.Keys.OrderBy(key => key.Time).ToList();
            var previous = keys[0];
            var next = keys[^1];
            for (var i = 0; i < keys.Count - 1; i++)
            {
                if (Time >= keys[i].Time && Time <= keys[i + 1].Time)
                {
                    previous = keys[i];
                    next = keys[i + 1];
                    break;
                }
            }

            var span = MathF.Max(0.0001f, next.Time - previous.Time);
            var t = System.Math.Clamp((Time - previous.Time) / span, 0f, 1f);
            t = Ease(t, next.Easing);
            var value = (previous.OutTangent.LengthSquared() > 0.0001f || next.InTangent.LengthSquared() > 0.0001f)
                ? Hermite(previous.Value, previous.OutTangent, next.Value, next.InTangent, t)
                : Vector2.Lerp(previous.Value, next.Value, t);
            var number = previous.Number + (next.Number - previous.Number) * t;

            if (track.Property == "event")
            {
                foreach (var key in keys)
                    if (_lastTime < key.Time && Time >= key.Time && !string.IsNullOrWhiteSpace(key.Text))
                        Event?.Invoke(key.Text!);
                continue;
            }

            switch (track.Property)
            {
                case "position": Owner!.Transform.Position = value; break;
                case "rotation": Owner!.Transform.Rotation = number; break;
                case "scale": Owner!.Transform.Scale = value; break;
                case "frame":
                    if (Owner!.GetComponent<SpriteRenderer>() is { } sprite)
                        sprite.FrameRect = new Vector4(value.X, value.Y, MathF.Max(1f, number), MathF.Max(1f, number));
                    break;
                case "audio":
                    foreach (var key in keys)
                        if (_lastTime < key.Time && Time >= key.Time && !string.IsNullOrWhiteSpace(key.Text))
                            CoreAudio.Play(key.Text!, 1f);
                    break;
            }
        }
    }

    private static Vector2 Hermite(Vector2 a, Vector2 outTangent, Vector2 b, Vector2 inTangent, float t)
    {
        var t2 = t * t;
        var t3 = t2 * t;
        var h00 = 2f * t3 - 3f * t2 + 1f;
        var h10 = t3 - 2f * t2 + t;
        var h01 = -2f * t3 + 3f * t2;
        var h11 = t3 - t2;
        return h00 * a + h10 * outTangent + h01 * b + h11 * inTangent;
    }

    private static float Ease(float t, AnimationEasing easing) => easing switch
    {
        AnimationEasing.Smooth => t * t * (3f - 2f * t),
        AnimationEasing.EaseIn => t * t,
        AnimationEasing.EaseOut => 1f - (1f - t) * (1f - t),
        AnimationEasing.Step => t < 1f ? 0f : 1f,
        _ => t,
    };

    public override Component CloneFor(Entity newOwner) => new AnimationPlayer2D
    {
        Enabled = Enabled,
        Clips = Clips.Select(clip => new AnimationTimeline
        {
            Name = clip.Name, Duration = clip.Duration, Loop = clip.Loop,
            Tracks = clip.Tracks.Select(track => new AnimationTrack
            {
                Property = track.Property,
                Keys = track.Keys.Select(key => new AnimationKeyframe { Time = key.Time, Value = key.Value, Number = key.Number, Text = key.Text, Easing = key.Easing, OutTangent = key.OutTangent, InTangent = key.InTangent }).ToList(),
            }).ToList(),
        }).ToList(),
        PlayOnStart = PlayOnStart, Speed = Speed, Owner = newOwner,
    };
}
