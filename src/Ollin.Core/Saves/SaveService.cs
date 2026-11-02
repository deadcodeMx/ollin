using System.Text.Json;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;

namespace Ollin.Core.Saves;

/// <summary>Partida guardada: escena completa y metadatos.</summary>
public sealed class GameSave
{
    public int Version { get; set; } = 1;

    public string Name { get; set; } = "Partida";

    public DateTime SavedAt { get; set; } = DateTime.Now;

    /// <summary>Ruta de la escena original, si el guardado vino de un archivo.</summary>
    public string? ScenePath { get; set; }

    public Scene? Scene { get; set; }

    /// <summary>Datos globales de los scripts (puertas, misiones...).</summary>
    public GameState State { get; set; } = new();
}

/// <summary>Guarda y carga partidas con la escena y el estado de sus componentes.</summary>
public static class SaveService
{
    public static void Save(Scene scene, string path, string? scenePath = null, string name = "Partida", GameState? state = null)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var save = new GameSave
        {
            Name = name,
            SavedAt = DateTime.Now,
            ScenePath = scenePath,
            Scene = scene,
            State = state ?? new GameState(),
        };

        var json = JsonSerializer.Serialize(save, SceneSerializer.Options);
        var fullPath = Path.GetFullPath(path);
        var temp = fullPath + ".tmp";

        if (File.Exists(fullPath))
        {
            try { File.Copy(fullPath, fullPath + ".bak", overwrite: true); } catch { }
        }

        File.WriteAllText(temp, json);
        File.Move(temp, fullPath, overwrite: true);
    }

    public static GameSave Load(string path)
    {
        GameSave? save = null;
        foreach (var candidate in new[] { path, path + ".bak" })
        {
            if (!File.Exists(candidate)) continue;
            try
            {
                save = JsonSerializer.Deserialize<GameSave>(File.ReadAllText(candidate), SceneSerializer.Options);
                if (save?.Scene is not null) break;
            }
            catch { }
        }

        if (save?.Scene is null)
        {
            throw new InvalidOperationException("La partida guardada no es válida.");
        }

        save.Scene.Rebind();
        return save;
    }

    public static string DefaultPath(string projectRoot) => Path.Combine(projectRoot, "saves", "partida.json");
}
