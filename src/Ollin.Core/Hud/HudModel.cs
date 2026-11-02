using System.Numerics;

namespace Ollin.Core.Hud;

/// <summary>Barra del HUD (vida, energía, progreso...).</summary>
public sealed class HudBar
{
    public string Name { get; set; } = string.Empty;

    public float Value { get; set; }

    public float Max { get; set; } = 100f;

    public Vector4 Color { get; set; } = new(0.28f, 0.72f, 0.66f, 1f);

    public float Fraction => Max <= 0f ? 0f : System.Math.Clamp(Value / Max, 0f, 1f);
}

/// <summary>Texto fijo del HUD (puntos, munición, etc.).</summary>
public sealed class HudLabel
{
    public string Name { get; set; } = string.Empty;

    public string Text { get; set; } = string.Empty;
}

/// <summary>Mensaje temporal en pantalla.</summary>
public sealed class HudMessage
{
    public string Text { get; set; } = string.Empty;

    public float Remaining { get; set; }

    public float Duration { get; set; }
}

/// <summary>
/// Estado del HUD de la partida. Los scripts lo modifican a través de
/// <see cref="HudHost"/> y el editor lo dibuja sobre el viewport.
/// </summary>
public sealed class HudModel
{
    private readonly List<HudBar> _bars = new();
    private readonly List<HudLabel> _labels = new();
    private readonly List<HudMessage> _messages = new();

    public IReadOnlyList<HudBar> Bars => _bars;

    public IReadOnlyList<HudLabel> Labels => _labels;

    public IReadOnlyList<HudMessage> Messages => _messages;

    public float TransitionAlpha { get; private set; }

    public Vector4 TransitionColor { get; private set; } = Vector4.Zero;

    private float _transitionRemaining;
    private float _transitionDuration;

    public bool IsGameOver { get; private set; }

    public string? GameOverText { get; private set; }

    public void SetBar(string name, float value, float max, Vector4 color)
    {
        var bar = _bars.Find(item => item.Name == name);

        if (bar is null)
        {
            bar = new HudBar { Name = name };
            _bars.Add(bar);
        }

        bar.Value = value;
        bar.Max = max <= 0f ? 100f : max;
        bar.Color = color;
    }

    public void SetLabel(string name, string text)
    {
        var label = _labels.Find(item => item.Name == name);

        if (label is null)
        {
            label = new HudLabel { Name = name };
            _labels.Add(label);
        }

        label.Text = text;
    }

    public void ShowMessage(string text, float seconds)
    {
        var duration = System.Math.Max(0.2f, seconds);
        _messages.RemoveAll(item => item.Text == text);
        _messages.Add(new HudMessage { Text = text, Remaining = duration, Duration = duration });
    }

    public void ShowTransition(float seconds, Vector4 color)
    {
        _transitionDuration = System.Math.Max(0.05f, seconds);
        _transitionRemaining = _transitionDuration;
        TransitionColor = color;
        TransitionAlpha = 1f;
    }

    public void GameOver(string text)
    {
        IsGameOver = true;
        GameOverText = text;
    }

    public void Clear()
    {
        _bars.Clear();
        _labels.Clear();
        _messages.Clear();
        IsGameOver = false;
        GameOverText = null;
        TransitionAlpha = 0f;
        _transitionRemaining = 0f;
        _transitionDuration = 0f;
    }

    public void Update(float deltaTime)
    {
        if (_transitionRemaining > 0f)
        {
            _transitionRemaining = System.Math.Max(0f, _transitionRemaining - deltaTime);
            TransitionAlpha = _transitionDuration <= 0f ? 0f : _transitionRemaining / _transitionDuration;
        }
        else TransitionAlpha = 0f;

        if (_messages.Count == 0)
        {
            return;
        }

        for (var i = _messages.Count - 1; i >= 0; i--)
        {
            _messages[i].Remaining -= deltaTime;

            if (_messages[i].Remaining <= 0f)
            {
                _messages.RemoveAt(i);
            }
        }
    }
}
