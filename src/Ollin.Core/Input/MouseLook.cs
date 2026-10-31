using System.Numerics;

namespace Ollin.Core.Input;

/// <summary>
/// Estado del ratón bloqueado. La plataforma (Editor/Player) rellena el delta
/// mientras el puntero está capturado; el controlador FPS lo consume.
/// </summary>
public static class MouseLook
{
    private static Vector2 _delta;

    public static bool IsLocked { get; set; }

    public static void AddDelta(Vector2 delta)
    {
        if (IsLocked)
        {
            _delta += delta;
        }
    }

    public static Vector2 ConsumeDelta()
    {
        var value = _delta;
        _delta = Vector2.Zero;
        return value;
    }

    public static void Reset()
    {
        _delta = Vector2.Zero;
    }

    public static void Disable()
    {
        IsLocked = false;
        _delta = Vector2.Zero;
    }
}
