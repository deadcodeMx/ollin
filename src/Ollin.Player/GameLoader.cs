using System.Text.Json;
using Ollin.Core.Projects;

namespace Ollin.Player;

/// <summary>Localiza la carpeta del juego y su manifiesto.</summary>
public static class GameLoader
{
    public static (string? Root, GameManifest? Manifest) Load()
    {
        var arguments = Environment.GetCommandLineArgs();
        string? requested = null;

        for (var i = 0; i < arguments.Length - 1; i++)
        {
            if (arguments[i].Equals("--project", StringComparison.OrdinalIgnoreCase))
            {
                requested = arguments[i + 1];
            }
        }

        var root = ResolveRoot(requested);

        if (root is null)
        {
            return (null, null);
        }

        var manifestPath = Path.Combine(root, "game.json");

        if (!File.Exists(manifestPath))
        {
            if (!File.Exists(Path.Combine(root, ProjectService.FileName)))
            {
                return (null, null);
            }

            return (root, new GameManifest { ProjectName = Path.GetFileName(root) });
        }

        try
        {
            var manifest = JsonSerializer.Deserialize<GameManifest>(
                File.ReadAllText(manifestPath),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            return manifest is null ? (root, new GameManifest()) : (root, manifest);
        }
        catch
        {
            return (root, new GameManifest());
        }
    }

    private static string? ResolveRoot(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var full = Path.GetFullPath(requested);
            return File.Exists(full) ? Path.GetDirectoryName(full) : Directory.Exists(full) ? full : null;
        }

        var baseDirectory = AppContext.BaseDirectory;

        if (File.Exists(Path.Combine(baseDirectory, ProjectService.FileName)) ||
            File.Exists(Path.Combine(baseDirectory, "game.json")))
        {
            return baseDirectory;
        }

        return null;
    }
}
