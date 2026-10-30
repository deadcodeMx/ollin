using System.Numerics;

namespace Ollin.Core.Input;

/// <summary>
/// Estado de entrada del juego. El editor lo alimenta con los eventos de teclado y
/// ratón; el runtime lo "cierra" al final de cada cuadro para calcular los flancos
/// (<c>GetKeyDown</c>, <c>GetKeyUp</c>) y el desplazamiento del ratón.
/// </summary>
public static class InputState
{
    private static readonly HashSet<KeyCode> Down = new();
    private static readonly HashSet<KeyCode> Previous = new();
    private static readonly bool[] MouseDown = new bool[3];
    private static readonly bool[] MousePrevious = new bool[3];

    private static Vector2 _mouseScreen;
    private static Vector2 _mouseWorld;
    private static Vector2 _mouseDelta;
    private static Vector2 _lastMouseScreen;

    /// <summary>Posición del ratón en píxeles del viewport.</summary>
    public static Vector2 MouseScreenPosition => _mouseScreen;

    /// <summary>Posición del ratón en unidades de mundo.</summary>
    public static Vector2 MouseWorldPosition => _mouseWorld;

    /// <summary>Desplazamiento del ratón durante el último cuadro (píxeles).</summary>
    public static Vector2 MouseDelta => _mouseDelta;

    public static bool IsKeyDown(KeyCode key) => Down.Contains(key);

    public static bool WasKeyPressed(KeyCode key) => Down.Contains(key) && !Previous.Contains(key);

    public static bool WasKeyReleased(KeyCode key) => !Down.Contains(key) && Previous.Contains(key);

    public static bool IsMouseButtonDown(int button) => InRange(button) && MouseDown[button];

    public static bool WasMouseButtonPressed(int button) => InRange(button) && MouseDown[button] && !MousePrevious[button];

    public static bool WasMouseButtonReleased(int button) => InRange(button) && !MouseDown[button] && MousePrevious[button];

    public static void SetKey(KeyCode key, bool down)
    {
        if (key == KeyCode.None)
        {
            return;
        }

        if (down)
        {
            Down.Add(key);
        }
        else
        {
            Down.Remove(key);
        }
    }

    public static void SetMouseButton(int button, bool down)
    {
        if (InRange(button))
        {
            MouseDown[button] = down;
        }
    }

    public static void SetMousePosition(Vector2 screen, Vector2 world)
    {
        _mouseScreen = screen;
        _mouseWorld = world;
    }

    /// <summary>Cierra el cuadro: calcula flancos y delta del ratón. Lo llama el runtime.</summary>
    public static void Update()
    {
        _mouseDelta = _mouseScreen - _lastMouseScreen;
        _lastMouseScreen = _mouseScreen;

        Previous.Clear();
        foreach (var key in Down)
        {
            Previous.Add(key);
        }

        for (var i = 0; i < MouseDown.Length; i++)
        {
            MousePrevious[i] = MouseDown[i];
        }
    }

    /// <summary>Limpia todo el estado (al empezar o detener la reproducción).</summary>
    public static void Reset()
    {
        Down.Clear();
        Previous.Clear();
        Array.Clear(MouseDown);
        Array.Clear(MousePrevious);
        _mouseScreen = Vector2.Zero;
        _mouseWorld = Vector2.Zero;
        _mouseDelta = Vector2.Zero;
        _lastMouseScreen = Vector2.Zero;
    }

    private static bool InRange(int button) => button is >= 0 and < 3;
}
