using System.Numerics;
using Ollin.Core.Hud;

namespace Ollin.Scripting;

/// <summary>HUD para scripts: barras, etiquetas, mensajes y fin de partida.</summary>
public static class Hud
{
    private static readonly Vector4 DefaultColor = new(0.28f, 0.72f, 0.66f, 1f);

    /// <summary>Define o actualiza una barra (vida, energía, progreso...).</summary>
    public static void SetBar(string name, float value, float max = 100f, Vector4? color = null) =>
        HudHost.SetBar(name, value, max, color ?? DefaultColor);

    /// <summary>Define o actualiza una etiqueta de texto (puntos, munición...).</summary>
    public static void SetLabel(string name, string text) => HudHost.SetLabel(name, text);

    /// <summary>Muestra un mensaje temporal en pantalla.</summary>
    public static void Message(string text, float seconds = 2f) => HudHost.ShowMessage(text, seconds);

    /// <summary>Muestra la pantalla de fin de partida.</summary>
    public static void GameOver(string text = "Fin del juego") => HudHost.GameOver(text);

    /// <summary>Vacía el HUD.</summary>
    public static void Clear() => HudHost.Clear();
}
