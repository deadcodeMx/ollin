namespace Ollin.Core.Saves;

/// <summary>
/// Datos globales de la partida que los scripts pueden guardar y recuperar
/// (puertas abiertas, misiones, contadores...). Se incluyen en el guardado.
/// </summary>
public sealed class GameState
{
    public Dictionary<string, string> Values { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    public void Set(string key, string value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        Values[key] = value;
    }

    public string? Get(string key) =>
        !string.IsNullOrWhiteSpace(key) && Values.TryGetValue(key, out var value) ? value : null;

    public bool Has(string key) => !string.IsNullOrWhiteSpace(key) && Values.ContainsKey(key);

    public void Remove(string key)
    {
        if (!string.IsNullOrWhiteSpace(key))
        {
            Values.Remove(key);
        }
    }

    public void Clear() => Values.Clear();

    public void SetNumber(string key, float value) => Set(key, value.ToString(System.Globalization.CultureInfo.InvariantCulture));

    public float GetNumber(string key, float fallback = 0f) =>
        float.TryParse(Get(key), System.Globalization.NumberStyles.Float,
            System.Globalization.CultureInfo.InvariantCulture, out var value) ? value : fallback;

    public void SetFlag(string key, bool value) => Set(key, value ? "1" : "0");

    public bool GetFlag(string key, bool fallback = false)
    {
        var value = Get(key);

        if (value is null)
        {
            return fallback;
        }

        return value is "1" or "true" or "True";
    }

    public void CopyFrom(GameState other)
    {
        Values.Clear();

        foreach (var (key, value) in other.Values)
        {
            Values[key] = value;
        }
    }
}

/// <summary>Acceso de los scripts al estado global de la partida.</summary>
public static class GameStateHost
{
    public static GameState? Current { get; set; }
}
