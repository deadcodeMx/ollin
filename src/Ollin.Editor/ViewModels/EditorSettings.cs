using System.Text.Json;

namespace Ollin.Editor.ViewModels;

/// <summary>Preferencias del editor que sobreviven entre sesiones.</summary>
public sealed class EditorSettings
{
    public string? LastProject { get; set; }

    public bool ShowGrid { get; set; } = true;

    private static string SettingsFile => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Ollin",
        "editor.json");

    public static EditorSettings Load()
    {
        try
        {
            if (File.Exists(SettingsFile))
            {
                return JsonSerializer.Deserialize<EditorSettings>(File.ReadAllText(SettingsFile)) ?? new EditorSettings();
            }
        }
        catch
        {
            // Preferencias corruptas: se ignoran y se empieza de cero.
        }

        return new EditorSettings();
    }

    public void Save()
    {
        try
        {
            var folder = Path.GetDirectoryName(SettingsFile)!;
            Directory.CreateDirectory(folder);
            File.WriteAllText(SettingsFile, JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true }));
        }
        catch
        {
            // No es crítico si no se pueden guardar las preferencias.
        }
    }
}
