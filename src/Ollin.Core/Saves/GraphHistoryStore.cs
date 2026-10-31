using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Ollin.Core.Saves;

/// <summary>Persistencia del historial de deshacer del editor de grafo.</summary>
public sealed class GraphHistoryStore
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = false,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public GraphHistoryStore(string? rootPath = null)
    {
        RootPath = string.IsNullOrWhiteSpace(rootPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Ollin", "graph-history")
            : rootPath;
    }

    public string RootPath { get; }

    public string GetPath(string scenePath, Guid entityId)
    {
        var hash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes($"{scenePath}|{entityId}")));
        return Path.Combine(RootPath, hash + ".json");
    }

    public void Save(string scenePath, Guid entityId, NodeGraphHistory history)
    {
        var path = GetPath(scenePath, entityId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var payload = new Payload(history.UndoSnapshots.ToList(), history.RedoSnapshots.ToList());
        File.WriteAllText(path, JsonSerializer.Serialize(payload, Options));
    }

    public bool Load(string scenePath, Guid entityId, NodeGraphHistory history)
    {
        var path = GetPath(scenePath, entityId);

        if (!File.Exists(path))
        {
            return false;
        }

        try
        {
            var payload = JsonSerializer.Deserialize<Payload>(File.ReadAllText(path), Options);

            if (payload is null)
            {
                return false;
            }

            history.Replace(payload.Undo ?? new List<GraphSnapshot>(), payload.Redo ?? new List<GraphSnapshot>());
            return true;
        }
        catch
        {
            return false;
        }
    }

    private sealed record Payload(List<GraphSnapshot>? Undo, List<GraphSnapshot>? Redo);
}
