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

    public void Save(string scenePath, Guid entityId, NodeGraphHistory history, IEnumerable<Guid>? breakpoints = null)
    {
        var path = GetPath(scenePath, entityId);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var payload = new Payload(2, history.UndoSnapshots.ToList(), history.RedoSnapshots.ToList(), breakpoints?.ToList() ?? new List<Guid>());
        File.WriteAllText(path, JsonSerializer.Serialize(payload, Options));
        Cleanup(TimeSpan.FromDays(30));
    }

    public bool Load(string scenePath, Guid entityId, NodeGraphHistory history, ICollection<Guid>? breakpoints = null)
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

            if (breakpoints is not null)
            {
                breakpoints.Clear();
                foreach (var id in payload.Breakpoints ?? new List<Guid>()) breakpoints.Add(id);
            }

            return true;
        }
        catch
        {
            return false;
        }
    }

    public int Cleanup(TimeSpan maxAge)
    {
        if (!Directory.Exists(RootPath)) return 0;

        var cutoff = DateTime.UtcNow - maxAge;
        var removed = 0;

        foreach (var file in Directory.EnumerateFiles(RootPath, "*.json"))
        {
            if (File.GetLastWriteTimeUtc(file) >= cutoff) continue;
            try { File.Delete(file); removed++; } catch { }
        }

        return removed;
    }

    private sealed record Payload(int Version, List<GraphSnapshot>? Undo, List<GraphSnapshot>? Redo, List<Guid>? Breakpoints);
}
