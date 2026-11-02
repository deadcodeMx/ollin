using System.Numerics;

namespace Ollin.Core.Hud;

/// <summary>
/// Puente entre el runtime y la API de scripting para el HUD. El runtime instala
/// los manejadores al reproducir y los limpia al detener.
/// </summary>
public static class HudHost
{
    public static Action<string, float, float, Vector4>? BarHandler { get; set; }

    public static Action<string, string>? LabelHandler { get; set; }

    public static Action<string, float>? MessageHandler { get; set; }

    public static Action<string>? GameOverHandler { get; set; }

    public static Action? ClearHandler { get; set; }

    public static Action<float, Vector4>? TransitionHandler { get; set; }

    public static void SetBar(string name, float value, float max, Vector4 color) =>
        BarHandler?.Invoke(name, value, max, color);

    public static void SetLabel(string name, string text) => LabelHandler?.Invoke(name, text);

    public static void ShowMessage(string text, float seconds) => MessageHandler?.Invoke(text, seconds);

    public static void GameOver(string text) => GameOverHandler?.Invoke(text);

    public static void Clear() => ClearHandler?.Invoke();

    public static void Transition(float seconds, Vector4 color) => TransitionHandler?.Invoke(seconds, color);

    public static void Reset()
    {
        BarHandler = null;
        LabelHandler = null;
        MessageHandler = null;
        GameOverHandler = null;
        ClearHandler = null;
        TransitionHandler = null;
    }
}
