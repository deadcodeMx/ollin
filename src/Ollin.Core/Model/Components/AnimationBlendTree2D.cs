using System.Numerics;

namespace Ollin.Core.Model.Components;

public sealed class AnimationBlendEntry
{
    public string Clip { get; set; } = string.Empty;
    public float Weight { get; set; } = 1f;
}

/// <summary>Mezcla lineal de varios clips de un AnimationPlayer2D.</summary>
public sealed class AnimationBlendTree2D : Component, ITickable
{
    public List<AnimationBlendEntry> Entries { get; set; } = new();
    public float Speed { get; set; } = 1f;
    public bool Loop { get; set; } = true;
    public float Duration { get; set; } = 1f;
    [System.Text.Json.Serialization.JsonIgnore] public float Time { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.GetComponent<AnimationPlayer2D>() is not { } player) return;
        Time += deltaTime * Speed;
        if (Loop && Duration > 0.001f) Time %= Duration;
        if (Entries.Count == 0) return;

        var position = Vector2.Zero;
        var scale = Vector2.Zero;
        var rotation = 0f;
        var total = 0f;
        foreach (var entry in Entries)
        {
            var weight = MathF.Max(0f, entry.Weight);
            if (weight <= 0f) continue;
            player.SampleTransform(entry.Clip, Time, out var p, out var r, out var s);
            position += p * weight;
            rotation += r * weight;
            scale += s * weight;
            total += weight;
        }
        if (total <= 0.0001f) return;
        Owner.Transform.Position = position / total;
        Owner.Transform.Rotation = rotation / total;
        Owner.Transform.Scale = scale / total;
    }

    public override Component CloneFor(Entity newOwner) => new AnimationBlendTree2D
    {
        Enabled = Enabled, Speed = Speed, Loop = Loop, Duration = Duration,
        Entries = Entries.Select(entry => new AnimationBlendEntry { Clip = entry.Clip, Weight = entry.Weight }).ToList(),
        Owner = newOwner,
    };
}
