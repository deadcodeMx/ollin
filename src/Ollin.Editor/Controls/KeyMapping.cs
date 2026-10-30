using Avalonia.Input;
using Ollin.Core.Input;

namespace Ollin.Editor.Controls;

/// <summary>Traduce las teclas de Avalonia al enum del motor.</summary>
public static class KeyMapping
{
    public static KeyCode? ToKeyCode(Key key)
    {
        if (key is >= Key.A and <= Key.Z)
        {
            return KeyCode.A + (key - Key.A);
        }

        if (key is >= Key.D0 and <= Key.D9)
        {
            return KeyCode.D0 + (key - Key.D0);
        }

        if (key is >= Key.F1 and <= Key.F12)
        {
            return KeyCode.F1 + (key - Key.F1);
        }

        return key switch
        {
            Key.Space => KeyCode.Space,
            Key.Enter => KeyCode.Enter,
            Key.Escape => KeyCode.Escape,
            Key.Tab => KeyCode.Tab,
            Key.Back => KeyCode.Backspace,
            Key.LeftShift => KeyCode.LeftShift,
            Key.RightShift => KeyCode.RightShift,
            Key.LeftCtrl => KeyCode.LeftControl,
            Key.RightCtrl => KeyCode.RightControl,
            Key.LeftAlt => KeyCode.LeftAlt,
            Key.RightAlt => KeyCode.RightAlt,
            Key.Left => KeyCode.Left,
            Key.Right => KeyCode.Right,
            Key.Up => KeyCode.Up,
            Key.Down => KeyCode.Down,
            _ => null,
        };
    }
}
