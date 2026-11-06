using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Transición automática por parámetro numérico.</summary>
public sealed class AnimationTransition2D
{
    public string From { get; set; } = string.Empty;
    public string To { get; set; } = string.Empty;
    public string Parameter { get; set; } = "speed";
    public float Threshold { get; set; }
}

/// <summary>Clip de una máquina de estados de animación.</summary>
public sealed class AnimationClip2D
{
    public string Name { get; set; } = "idle";
    public int StartFrame { get; set; }
    public int FrameCount { get; set; } = 4;
    public float Fps { get; set; } = 8f;
    public bool Loop { get; set; } = true;
    public float Speed { get; set; } = 1f;
}

/// <summary>
/// Máquina de estados simple para SpriteAnimator. Los estados se guardan como
/// datos y los scripts llaman a Play("run"), Play("jump"), etc.
/// </summary>
public sealed class AnimationStateMachine : Component, ITickable
{
    public List<AnimationClip2D> States { get; set; } = new()
    {
        new AnimationClip2D { Name = "idle", StartFrame = 0, FrameCount = 4, Fps = 6f },
        new AnimationClip2D { Name = "run", StartFrame = 4, FrameCount = 6, Fps = 12f },
        new AnimationClip2D { Name = "jump", StartFrame = 10, FrameCount = 2, Fps = 8f, Loop = false },
    };

    public string DefaultState { get; set; } = "idle";

    public List<AnimationTransition2D> Transitions { get; set; } = new();
    public Dictionary<string, float> Parameters { get; set; } = new();

    [JsonIgnore] public string CurrentState { get; private set; } = string.Empty;

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.GetComponent<SpriteAnimator>() is not { } animator) return;
        if (string.IsNullOrEmpty(CurrentState)) Play(DefaultState);
        var clip = States.FirstOrDefault(state => state.Name == CurrentState);
        if (clip is null) return;
        animator.StartFrame = clip.StartFrame;
        animator.FrameCount = clip.FrameCount;
        animator.Fps = clip.Fps;
        animator.Loop = clip.Loop;
        animator.Speed = clip.Speed;
    }

    public void Play(string state)
    {
        if (States.All(item => item.Name != state)) return;
        if (CurrentState == state) return;
        CurrentState = state;
        Owner?.GetComponent<SpriteAnimator>()?.Restart();
    }

    public override Component CloneFor(Entity newOwner) => new AnimationStateMachine
    {
        Enabled = Enabled,
        States = States.Select(state => new AnimationClip2D
        {
            Name = state.Name, StartFrame = state.StartFrame, FrameCount = state.FrameCount,
            Fps = state.Fps, Loop = state.Loop, Speed = state.Speed,
        }).ToList(),
        DefaultState = DefaultState,
        Transitions = Transitions.Select(item => new AnimationTransition2D
        {
            From = item.From, To = item.To, Parameter = item.Parameter, Threshold = item.Threshold,
        }).ToList(),
        Parameters = new Dictionary<string, float>(Parameters),
        Owner = newOwner,
    };
}
