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
/// Empaqueta un proyecto en un juego listo para distribuir: un ejecutable con el
/// nombre del juego y un `game.pak` cifrado con escenas, assets y scripts compilados.
/// </summary>
public static class GameExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Export(
        ProjectInfo project,
        string destination,
        string playerTemplate,
        byte[]? scriptsImage = null,
        bool splash = true,
        bool watermark = true)
    {
        if (!Directory.Exists(playerTemplate))
        {
            throw new DirectoryNotFoundException($"No se encontró la plantilla del reproductor: {playerTemplate}");
        }

        var target = Path.Combine(destination, SafeName(project.Name));
        Directory.CreateDirectory(target);

        var manifest = new GameManifest
        {
            ProjectName = project.Name,
            StartScene = project.StartScene,
            OllinVersion = typeof(GameExporter).Assembly.GetName().Version?.ToString() ?? "1.0.0",
            Splash = splash,
            Watermark = watermark,
        };

        var package = GamePackage.Create(project, manifest, scriptsImage);
        File.WriteAllBytes(Path.Combine(target, "game.pak"), package);

        CopyPlayer(playerTemplate, target, SafeName(project.Name) + ".exe");

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

    private static void CopyPlayer(string template, string target, string executableName)
    {
        foreach (var file in Directory.EnumerateFiles(template))
        {
            var name = Path.GetFileName(file);
            var destination = name.Equals("Ollin.Player.exe", StringComparison.OrdinalIgnoreCase)
                ? executableName
                : name;

            File.Copy(file, Path.Combine(target, destination), overwrite: true);
        }

        foreach (var directory in Directory.EnumerateDirectories(template))
        {
            var destination = Path.Combine(target, Path.GetFileName(directory));
            Directory.CreateDirectory(destination);

            foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
            {
                var relative = Path.GetRelativePath(directory, file);
                var path = Path.Combine(destination, relative);
                Directory.CreateDirectory(Path.GetDirectoryName(path)!);
                File.Copy(file, path, overwrite: true);
            }
        }
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

        Ejecuta {name}.exe.

        Controles:
          F11  pantalla completa
          Esc  salir

        game.pak contiene las escenas, assets y scripts compilados del juego.
        El código fuente no se incluye. La pantalla de inicio y la marca de agua
        se pueden quitar editando el manifiesto dentro del paquete.
        """;

    private static string SafeName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var clean = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
        return string.IsNullOrWhiteSpace(clean) ? "Juego" : clean;
    }
}
