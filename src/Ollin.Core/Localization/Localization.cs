using System.Text.Json;

namespace Ollin.Core.Localization;

/// <summary>Textos por idioma cargados desde localization/*.json.</summary>
public sealed class LocalizationDatabase
{
    private readonly Dictionary<string, Dictionary<string, string>> _languages = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public LocalizationDatabase(string root) { Root = root; Reload(); }

    public string Root { get; }
    public string Language { get; private set; } = "es";
    public IReadOnlyCollection<string> Languages => _languages.Keys;

    public void Reload()
    {
        _languages.Clear();
        var folder = Path.Combine(Root, "localization");
        if (!Directory.Exists(folder)) return;
        foreach (var file in Directory.EnumerateFiles(folder, "*.json"))
        {
            try
            {
                var values = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(file), Options);
                if (values is not null) _languages[Path.GetFileNameWithoutExtension(file)] = values;
            }
            catch { }
        }
        if (!_languages.ContainsKey(Language) && _languages.Count > 0)
            Language = _languages.Keys.First();
    }

    public void SetLanguage(string language)
    {
        if (_languages.ContainsKey(language)) Language = language;
    }

    public bool IsRightToLeft => Language is "ar" or "he" or "fa" or "ur";

    public bool Pseudo { get; set; }

    public string GetPlural(string key, int count, string? fallback = null)
    {
        var suffix = count == 0 ? ".zero" : count == 1 ? ".one" : ".other";
        var plural = Get(key + suffix, string.Empty);
        return string.IsNullOrEmpty(plural) ? Get(key, fallback) : plural;
    }

    public string GetContext(string context, string key, string? fallback = null)
    {
        var contextual = Get($"{context}.{key}", string.Empty);
        return string.IsNullOrEmpty(contextual) ? Get(key, fallback) : contextual;
    }

    public string Get(string key, string? fallback = null)
    {
        if (_languages.TryGetValue(Language, out var table) && table.TryGetValue(key, out var text)) return Pseudo ? $"[{text}]" : text;
        if (_languages.TryGetValue("es", out var spanish) && spanish.TryGetValue(key, out var spanishText)) return Pseudo ? $"[{spanishText}]" : spanishText;
        return fallback ?? key;
    }
}

/// <summary>Acceso global a la localización para scripts y runtime.</summary>
public static class Localization
{
    private static LocalizationDatabase? _database;
    public static string Language => _database?.Language ?? "es";
    public static IReadOnlyCollection<string> Languages => _database?.Languages ?? Array.Empty<string>();
    public static bool IsRightToLeft => _database?.IsRightToLeft ?? false;

    public static void Load(string root, string? language = null)
    {
        _database = new LocalizationDatabase(root);
        if (!string.IsNullOrWhiteSpace(language)) _database.SetLanguage(language);
    }

    public static void SetLanguage(string language) => _database?.SetLanguage(language);
    public static bool Pseudo { get => _database?.Pseudo ?? false; set { if (_database is not null) _database.Pseudo = value; } }

    public static string Get(string key, string? fallback = null) => _database?.Get(key, fallback) ?? fallback ?? key;

    public static string GetContext(string context, string key, string? fallback = null) => _database?.GetContext(context, key, fallback) ?? fallback ?? key;

    public static string GetPlural(string key, int count, string? fallback = null) => _database?.GetPlural(key, count, fallback) ?? fallback ?? key;

    public static string Format(string key, params object[] values) =>
        string.Format(Get(key), values);
}
