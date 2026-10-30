using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ollin.Core.Projects;

/// <summary>Datos que acompañan a un juego exportado.</summary>
public sealed class GameManifest
{
    public string ProjectName { get; set; } = "Juego";

    public string StartScene { get; set; } = "scenes/inicio.ollin.json";

    public string OllinVersion { get; set; } = "0.0.0";

    /// <summary>Muestra la pantalla de inicio con el logo de Ollin.</summary>
    public bool Splash { get; set; } = true;

    /// <summary>Muestra la marca de agua «Hecho con Ollin» durante el juego.</summary>
    public bool Watermark { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// Empaqueta un proyecto y el reproductor en una carpeta lista para ejecutar
/// (`Ollin.Player.exe` + escenas, assets, scripts y `game.json`).
/// </summary>
public static class GameExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    private static readonly string[] ProjectFolders =
    {
        "scenes", "assets", "scripts", "dialogs", "items", "prefabs",
    };

    public static string Export(
        ProjectInfo project,
        string destination,
        string playerTemplate,
        bool splash = true,
        bool watermark = true)
    {
        if (!Directory.Exists(playerTemplate))
        {
            throw new DirectoryNotFoundException($"No se encontró la plantilla del reproductor: {playerTemplate}");
        }

        var target = Path.Combine(destination, SafeName(project.Name));
        Directory.CreateDirectory(target);

        CopyDirectory(playerTemplate, target);

        foreach (var folder in ProjectFolders)
        {
            var source = Path.Combine(project.RootPath, folder);

            if (Directory.Exists(source))
            {
                CopyDirectory(source, Path.Combine(target, folder));
            }
        }

        File.Copy(project.ProjectFile, Path.Combine(target, ProjectService.FileName), overwrite: true);

        var manifest = new GameManifest
        {
            ProjectName = project.Name,
            StartScene = project.StartScene,
            OllinVersion = typeof(GameExporter).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Splash = splash,
            Watermark = watermark,
        };

        File.WriteAllText(Path.Combine(target, "game.json"), JsonSerializer.Serialize(manifest, JsonOptions));
        File.WriteAllText(Path.Combine(target, "LEEME.txt"), ReadmeText(project.Name, manifest));

        return target;
    }

    /// <summary>Busca la plantilla del reproductor instalada o la de desarrollo.</summary>
    public static string? FindPlayerTemplate()
    {
        var candidates = new List<string>
        {
            Path.Combine(AppContext.BaseDirectory, "player"),
            Path.Combine(AppContext.BaseDirectory, "..", "player"),
        };

        var root = FindRepositoryRoot();

        if (root is not null)
        {
            candidates.Add(Path.Combine(root, "dist", "app", "player"));
            candidates.Add(Path.Combine(root, "tools", "player-template"));
        }

        return candidates
            .Select(Path.GetFullPath)
            .FirstOrDefault(path => Directory.Exists(path) && Directory.EnumerateFiles(path, "Ollin.Player*").Any());
    }

    private static string? FindRepositoryRoot()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);

        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, "Ollin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }

    private static string ReadmeText(string name, GameManifest manifest) => $"""
        {name}
        Hecho con Ollin {manifest.OllinVersion}

        Ejecuta Ollin.Player.exe.

        Controles:
          F11  pantalla completa
          Esc  salir

        Este juego incluye el reproductor de Ollin y los datos del proyecto.
        La pantalla de inicio y la marca de agua se pueden quitar modificando game.json.
        """;

    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "Juego" : clean;
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);

        foreach (var file in Directory.EnumerateFiles(source))
        {
            File.Copy(file, Path.Combine(target, Path.GetFileName(file)), overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(source))
        {
            CopyDirectory(directory, Path.Combine(target, Path.GetFileName(directory)));
        }
    }
}
