using System.Security.Cryptography;
using System.Text.Json;
using Ollin.Core.Projects;

namespace Ollin.Player;

/// <summary>Carpeta extraída del juego y manifiesto.</summary>
public sealed record LoadedGame(string Root, GameManifest Manifest, byte[]? ScriptsImage);

/// <summary>Localiza y descomprime el paquete `game.pak` del juego.</summary>
public static class GameLoader
{
    public static LoadedGame? Load()
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

        var origin = ResolveOrigin(requested);

        if (origin is null)
        {
            return null;
        }

        var packagePath = Path.Combine(origin, "game.pak");

        if (File.Exists(packagePath))
        {
            try
            {
                var bytes = File.ReadAllBytes(packagePath);
                var extractRoot = Path.Combine(
                    Path.GetTempPath(),
                    "OllinGames",
                    Convert.ToHexString(SHA256.HashData(bytes))[..16]);

                Directory.CreateDirectory(extractRoot);
                var (manifest, scripts) = GamePackage.Extract(bytes, extractRoot);
                return new LoadedGame(extractRoot, manifest, scripts);
            }
            catch
            {
                return null;
            }
        }

        // Carpeta de proyecto sin empaquetar (útil en desarrollo).
        if (File.Exists(Path.Combine(origin, ProjectService.FileName)))
        {
            var manifestPath = Path.Combine(origin, "game.json");
            var manifest = File.Exists(manifestPath)
                ? JsonSerializer.Deserialize<GameManifest>(File.ReadAllText(manifestPath), new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                }) ?? new GameManifest { ProjectName = Path.GetFileName(origin) }
                : new GameManifest { ProjectName = Path.GetFileName(origin) };

            return new LoadedGame(origin, manifest, null);
        }

        return null;
    }

    private static string? ResolveOrigin(string? requested)
    {
        if (!string.IsNullOrWhiteSpace(requested))
        {
            var full = Path.GetFullPath(requested);
            return File.Exists(full) ? Path.GetDirectoryName(full) : Directory.Exists(full) ? full : null;
        }

        var baseDirectory = AppContext.BaseDirectory;

        if (File.Exists(Path.Combine(baseDirectory, "game.pak")) ||
            File.Exists(Path.Combine(baseDirectory, ProjectService.FileName)))
        {
            return baseDirectory;
        }

        var processDirectory = Path.GetDirectoryName(Environment.ProcessPath);
        return processDirectory is not null && File.Exists(Path.Combine(processDirectory, "game.pak"))
            ? processDirectory
            : null;
    }
}
