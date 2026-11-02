using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Fundido de entrada o transición de pantalla.</summary>
public sealed class ScreenTransition2D : Component, ITickable
{
    public float Duration { get; set; } = 0.6f;
    public Vector4 Color { get; set; } = new(0f, 0f, 0f, 1f);
    public bool PlayOnStart { get; set; } = true;
    public float Delay { get; set; }

    private bool _played;
    private float _timer;

    public void Play() => Hud.HudHost.Transition(Duration, Color);

    public void OnUpdate(float deltaTime)
    {
        if (!PlayOnStart || _played) return;
        _timer += deltaTime;
        if (_timer < Delay) return;
        _played = true;
        Play();
    }

    public override Component CloneFor(Entity newOwner) => new ScreenTransition2D
    {
        Enabled = Enabled, Duration = Duration, Color = Color, PlayOnStart = PlayOnStart, Delay = Delay, Owner = newOwner,
    };
}
