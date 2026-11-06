using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Pista de ángulo para un hueso de Skeleton2D.</summary>
public sealed class SkeletonBoneTrack2D
{
    public int Bone { get; set; }
    public List<AnimationKeyframe> Keys { get; set; } = new();
}

/// <summary>Clip de animación esquelética: ángulos de huesos en el tiempo.</summary>
public sealed class SkeletonClip2D
{
    public string Name { get; set; } = "idle";
    public float Duration { get; set; } = 1f;
    public bool Loop { get; set; } = true;
    public List<SkeletonBoneTrack2D> Tracks { get; set; } = new();
}

/// <summary>Reproduce clips esqueléticos sobre Skeleton2D con eventos y mezcla simple.</summary>
public sealed class SkeletonAnimator2D : Component, ITickable
{
    public List<SkeletonClip2D> Clips { get; set; } = new();
    public string? PlayOnStart { get; set; }
    public float Speed { get; set; } = 1f;
    public float Blend { get; set; } = 1f;

    [JsonIgnore] public string? Current { get; private set; }
    [JsonIgnore] public bool Playing { get; private set; }
    [JsonIgnore] public float Time { get; private set; }

    public event Action<string>? Event;

    private bool _started;
    private float _lastTime;

    public void OnUpdate(float deltaTime)
    {
        if (!_started)
        {
            _started = true;
            if (!string.IsNullOrWhiteSpace(PlayOnStart)) Play(PlayOnStart!);
        }
        if (!Playing || Owner?.GetComponent<Skeleton2D>() is not { } skeleton) return;
        var clip = Clips.FirstOrDefault(item => item.Name == Current);
        if (clip is null) { Playing = false; return; }

        _lastTime = Time;
        Time += deltaTime * MathF.Max(0f, Speed);
        var previousAngles = skeleton.Bones.Select(bone => bone.Angle).ToArray();
        foreach (var track in clip.Tracks)
        {
            if (track.Bone < 0 || track.Bone >= skeleton.Bones.Count || track.Keys.Count == 0) continue;
            var keys = track.Keys.OrderBy(key => key.Time).ToList();
            var previous = keys[0];
            var next = keys[^1];
            for (var i = 0; i < keys.Count - 1; i++)
                if (Time >= keys[i].Time && Time <= keys[i + 1].Time) { previous = keys[i]; next = keys[i + 1]; break; }
            var span = MathF.Max(0.0001f, next.Time - previous.Time);
            var t = System.Math.Clamp((Time - previous.Time) / span, 0f, 1f);
            t = next.Easing switch
            {
                AnimationEasing.Smooth => t * t * (3f - 2f * t),
                AnimationEasing.EaseIn => t * t,
                AnimationEasing.EaseOut => 1f - (1f - t) * (1f - t),
                AnimationEasing.Step => t < 1f ? 0f : 1f,
                _ => t,
            };
            var angle = previous.Number + (next.Number - previous.Number) * t;
            skeleton.Bones[track.Bone].Angle = previousAngles[track.Bone] + (angle - previousAngles[track.Bone]) * System.Math.Clamp(Blend, 0f, 1f);
        }

        foreach (var track in clip.Tracks)
            if (track.TracksEvents())
                foreach (var key in track.Keys)
                    if (_lastTime < key.Time && Time >= key.Time && !string.IsNullOrWhiteSpace(key.Text))
                        Event?.Invoke(key.Text!);

        if (Time >= clip.Duration)
        {
            if (clip.Loop) { Time %= MathF.Max(0.001f, clip.Duration); _lastTime = 0f; }
            else { Time = clip.Duration; Playing = false; }
        }
    }

    public void Play(string name)
    {
        if (Clips.All(clip => clip.Name != name)) return;
        Current = name;
        Time = 0f;
        _lastTime = 0f;
        Playing = true;
    }

    public void Stop() => Playing = false;
    public void Resume() => Playing = true;

    public override Component CloneFor(Entity newOwner) => new SkeletonAnimator2D
    {
        Enabled = Enabled,
        Speed = Speed, Blend = Blend, PlayOnStart = PlayOnStart,
        Clips = Clips.Select(clip => new SkeletonClip2D
        {
            Name = clip.Name, Duration = clip.Duration, Loop = clip.Loop,
            Tracks = clip.Tracks.Select(track => new SkeletonBoneTrack2D
            {
                Bone = track.Bone,
                Keys = track.Keys.Select(key => new AnimationKeyframe
                {
                    Time = key.Time, Value = key.Value, Number = key.Number, Text = key.Text,
                    Easing = key.Easing, OutTangent = key.OutTangent, InTangent = key.InTangent,
                }).ToList(),
            }).ToList(),
        }).ToList(),
        Owner = newOwner,
    };
}

internal static class SkeletonTrackExtensions
{
    public static bool TracksEvents(this SkeletonBoneTrack2D track) =>
        track.Keys.Any(key => !string.IsNullOrWhiteSpace(key.Text));
}
