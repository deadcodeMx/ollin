using System.Text.Json;
using Ollin.Core.Resources;

namespace Ollin.Core.Resources;

/// <summary>Carga recursos por ruta con cache, res:// y dependencias.</summary>
public static class ResourceLoader
{
    private static readonly Dictionary<string, OllinResource> Cache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly HashSet<string> Loading = new(StringComparer.OrdinalIgnoreCase);
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
        IncludeFields = true,
    };

    public static string Root { get; set; } = Environment.CurrentDirectory;

    public static string Resolve(string path)
    {
        if (path.StartsWith("res://", StringComparison.OrdinalIgnoreCase))
            return System.IO.Path.GetFullPath(System.IO.Path.Combine(Root, path[6..]));
        return System.IO.Path.IsPathRooted(path) ? System.IO.Path.GetFullPath(path) : System.IO.Path.GetFullPath(System.IO.Path.Combine(Root, path));
    }

    public static T? Load<T>(string path) where T : OllinResource
    {
        var full = Resolve(path);
        if (Cache.TryGetValue(full, out var cached)) return cached as T;
        if (!File.Exists(full)) return null;
        if (!Loading.Add(full)) throw new InvalidOperationException($"Ciclo de recursos: {path}");
        try
        {
            using var document = JsonDocument.Parse(File.ReadAllText(full));
            var type = document.RootElement.TryGetProperty("type", out var typeElement) ? typeElement.GetString() : null;
            OllinResource? resource = type switch
            {
                "tileset" => JsonSerializer.Deserialize<TileSetResource>(document.RootElement.GetRawText(), Options),
                _ => null,
            };
            if (resource is null) return null;
            resource.Path = full;
            Cache[full] = resource;
            foreach (var dependency in resource.Dependencies)
            {
                var dependencyFull = Resolve(dependency);
                if (!Cache.ContainsKey(dependencyFull)) Load<OllinResource>(dependency);
            }
            return resource as T;
        }
        finally
        {
            Loading.Remove(full);
        }
    }

    public static TileSetResource? LoadTileSet(string path) => Load<TileSetResource>(path);

    public static void Save<T>(T resource, string path) where T : OllinResource
    {
        var full = Resolve(path);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(full) ?? ".");
        var envelope = new Dictionary<string, object?>
        {
            ["type"] = resource is TileSetResource ? "tileset" : "resource",
            ["id"] = resource.Id,
            ["dependencies"] = resource.Dependencies,
        };
        if (resource is TileSetResource tileSet) envelope["tileset"] = tileSet.TileSet;
        File.WriteAllText(full, JsonSerializer.Serialize(envelope, Options));
        resource.Path = full;
        Cache[full] = resource;
    }

    public static void Clear() => Cache.Clear();
}
