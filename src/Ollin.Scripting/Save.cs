using Ollin.Core.Saves;

namespace Ollin.Scripting;

/// <summary>
/// Datos persistentes de la partida para scripts. Se guardan con la partida
/// (puertas abiertas, misiones, contadores...).
/// </summary>
public static class Save
{
    private static GameState? State => GameStateHost.Current;

    public static void Set(string key, string value) => State?.Set(key, value);

    public static string? Get(string key) => State?.Get(key);

    public static bool Has(string key) => State?.Has(key) ?? false;

    public static void Remove(string key) => State?.Remove(key);

    public static void SetNumber(string key, float value) => State?.SetNumber(key, value);

    public static float GetNumber(string key, float fallback = 0f) => State?.GetNumber(key, fallback) ?? fallback;

    public static void SetFlag(string key, bool value) => State?.SetFlag(key, value);

    public static bool GetFlag(string key, bool fallback = false) => State?.GetFlag(key, fallback) ?? fallback;
}
