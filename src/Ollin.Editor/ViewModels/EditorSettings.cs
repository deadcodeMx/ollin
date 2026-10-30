using System.Text.Json;

namespace Ollin.Editor.ViewModels;

/// <summary>Preferencias del editor que sobreviven entre sesiones.</summary>
public sealed class EditorSettings
{
    public string? LastProject { get; set; }

    public bool ShowGrid { get; set; } = true;

    /// <summary>Tema de la interfaz: Dark, Light u Ollin.</summary>
    public string Theme { get; set; } = "Dark";

    /// <summary>Últimos proyectos abiertos, del más reciente al más antiguo.</summary>
    public List<string> RecentProjects { get; set; } = new();

    public void RememberProject(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return;
        }

        RecentProjects.RemoveAll(item => string.Equals(item, path, StringComparison.OrdinalIgnoreCase));
        RecentProjects.Insert(0, path);

        if (RecentProjects.Count > 10)
        {
            RecentProjects.RemoveRange(10, RecentProjects.Count - 10);
        }

        LastProject = path;
    }

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
                var settings = JsonSerializer.Deserialize<EditorSettings>(File.ReadAllText(SettingsFile)) ?? new EditorSettings();
                settings.RecentProjects ??= new List<string>();

                if (settings.RecentProjects.Count == 0 && !string.IsNullOrWhiteSpace(settings.LastProject))
                {
                    settings.RecentProjects.Add(settings.LastProject);
                }

                return settings;
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
