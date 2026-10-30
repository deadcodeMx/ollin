using System.IO.Compression;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Ollin.Core.Projects;

/// <summary>
/// Paquete del juego: un ZIP con escenas, assets y scripts compilados, cifrado en
/// un solo archivo `game.pak`. Los scripts viajan como DLL, nunca como código fuente.
/// </summary>
public static class GamePackage
{
    private static readonly byte[] Magic = "OLLINPAK"u8.ToArray();
    private static readonly byte[] Key = SHA256.HashData(Encoding.UTF8.GetBytes("Ollin.Game.Package.2026.v1"));

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = true,
    };

    private static readonly string[] ContentFolders =
    {
        "scenes", "assets", "dialogs", "items", "prefabs",
    };

    /// <summary>Crea el paquete cifrado (escenas, assets, manifiesto y script DLL).</summary>
    public static byte[] Create(ProjectInfo project, GameManifest manifest, byte[]? scriptsImage)
    {
        using var buffer = new MemoryStream();

        using (var archive = new ZipArchive(buffer, ZipArchiveMode.Create, leaveOpen: true))
        {
            AddBytes(archive, "game.json", JsonSerializer.SerializeToUtf8Bytes(manifest, JsonOptions));
            AddFile(archive, ProjectService.FileName, project.ProjectFile);

            if (scriptsImage is { Length: > 0 })
            {
                AddBytes(archive, "game.dll", scriptsImage);
            }

            foreach (var folder in ContentFolders)
            {
                AddDirectory(archive, project.RootPath, folder);
            }
        }

        return Encrypt(buffer.ToArray());
    }

    /// <summary>Descifra y extrae el paquete; devuelve el manifiesto y los scripts.</summary>
    public static (GameManifest Manifest, byte[]? ScriptsImage) Extract(byte[] package, string targetDirectory)
    {
        var zipBytes = Decrypt(package);
        Directory.CreateDirectory(targetDirectory);

        using var buffer = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);

        var manifestEntry = archive.GetEntry("game.json");
        var manifest = manifestEntry is null
            ? new GameManifest()
            : JsonSerializer.Deserialize<GameManifest>(Read(manifestEntry), new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
            }) ?? new GameManifest();

        byte[]? scripts = null;

        foreach (var entry in archive.Entries)
        {
            if (string.IsNullOrWhiteSpace(entry.Name))
            {
                continue;
            }

            var relative = entry.FullName.Replace('\\', '/');

            if (relative.Contains("..", StringComparison.Ordinal) || Path.IsPathRooted(relative))
            {
                continue;
            }

            var target = Path.Combine(targetDirectory, relative.Replace('/', Path.DirectorySeparatorChar));
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);

            using (var output = File.Create(target))
            using (var input = entry.Open())
            {
                input.CopyTo(output);
            }

            if (relative.Equals("game.dll", StringComparison.OrdinalIgnoreCase))
            {
                scripts = File.ReadAllBytes(target);
            }
        }

        return (manifest, scripts);
    }

    /// <summary>Comprueba que el paquete contiene lo necesario para jugar.</summary>
    public static IReadOnlyList<string> Verify(byte[] package, GameManifest manifest, bool expectsScripts)
    {
        var missing = new List<string>();
        var zipBytes = Decrypt(package);

        using var buffer = new MemoryStream(zipBytes);
        using var archive = new ZipArchive(buffer, ZipArchiveMode.Read);
        var names = archive.Entries.Select(entry => entry.FullName.Replace('\\', '/')).ToHashSet(StringComparer.OrdinalIgnoreCase);

        void Require(string name)
        {
            if (!names.Contains(name))
            {
                missing.Add(name);
            }
        }

        Require("game.json");
        Require(ProjectService.FileName);
        Require(manifest.StartScene.Replace('\\', '/'));

        if (expectsScripts)
        {
            Require("game.dll");
        }

        return missing;
    }

    public static bool IsPackage(byte[] bytes) =>
        bytes.Length > Magic.Length + 12 + 16 &&
        bytes.AsSpan(0, Magic.Length).SequenceEqual(Magic);

    private static byte[] Encrypt(byte[] plain)
    {
        var nonce = RandomNumberGenerator.GetBytes(12);
        var tag = new byte[16];
        var cipher = new byte[plain.Length];

        using var aes = new AesGcm(Key, 16);
        aes.Encrypt(nonce, plain, cipher, tag);

        var result = new byte[Magic.Length + nonce.Length + tag.Length + cipher.Length];
        var offset = 0;
        Magic.CopyTo(result, offset);
        offset += Magic.Length;
        nonce.CopyTo(result, offset);
        offset += nonce.Length;
        tag.CopyTo(result, offset);
        offset += tag.Length;
        cipher.CopyTo(result, offset);
        return result;
    }

    private static byte[] Decrypt(byte[] package)
    {
        if (!IsPackage(package))
        {
            throw new InvalidDataException("El archivo no es un paquete de Ollin válido.");
        }

        var offset = Magic.Length;
        var nonce = package.AsSpan(offset, 12).ToArray();
        offset += 12;
        var tag = package.AsSpan(offset, 16).ToArray();
        offset += 16;
        var cipher = package.AsSpan(offset).ToArray();
        var plain = new byte[cipher.Length];

        using var aes = new AesGcm(Key, 16);
        aes.Decrypt(nonce, cipher, tag, plain);
        return plain;
    }

    private static void AddFile(ZipArchive archive, string entryName, string path)
    {
        if (File.Exists(path))
        {
            AddBytes(archive, entryName, File.ReadAllBytes(path));
        }
    }

    private static void AddDirectory(ZipArchive archive, string root, string folder)
    {
        var directory = Path.Combine(root, folder);

        if (!Directory.Exists(directory))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            AddBytes(archive, relative, File.ReadAllBytes(file));
        }
    }

    private static void AddBytes(ZipArchive archive, string entryName, byte[] bytes)
    {
        var entry = archive.CreateEntry(entryName, CompressionLevel.Optimal);
        using var stream = entry.Open();
        stream.Write(bytes);
    }

    private static byte[] Read(ZipArchiveEntry entry)
    {
        using var stream = entry.Open();
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }
}
