using System.Numerics;
using Ollin.Core.Input;

namespace Ollin.Scripting;

/// <summary>
/// Entrada de teclado y ratón disponible para los scripts. Los valores se leen del
/// estado que el editor alimenta mientras la escena se reproduce.
/// </summary>
public static class Input
{
    /// <summary>¿Está la tecla pulsada ahora mismo?</summary>
    public static bool GetAction(string action) => InputMap.IsDown(action);

    public static bool GetActionDown(string action) => InputMap.WasPressed(action);

    public static bool GetActionUp(string action) => InputMap.WasReleased(action);

    public static bool GetKey(KeyCode key) => InputState.IsKeyDown(key);

    /// <summary>¿Se pulsó la tecla en este cuadro?</summary>
    public static bool GetKeyDown(KeyCode key) => InputState.WasKeyPressed(key);

    /// <summary>¿Se soltó la tecla en este cuadro?</summary>
    public static bool GetKeyUp(KeyCode key) => InputState.WasKeyReleased(key);

    /// <summary>Botones del ratón: 0 izquierdo, 1 derecho, 2 central.</summary>
    public static bool GetMouseButton(int button) => InputState.IsMouseButtonDown(button);

    public static bool GetMouseButtonDown(int button) => InputState.WasMouseButtonPressed(button);

    public static bool GetMouseButtonUp(int button) => InputState.WasMouseButtonReleased(button);

    /// <summary>Posición del ratón en unidades de mundo.</summary>
    public static Vector2 MousePosition => InputState.MouseWorldPosition;

    /// <summary>Posición del ratón en píxeles del viewport.</summary>
    public static Vector2 MouseScreenPosition => InputState.MouseScreenPosition;

    /// <summary>Desplazamiento del ratón en el último cuadro (píxeles).</summary>
    public static Vector2 MouseDelta => InputState.MouseDelta;
}
