using System.Text.Json;
using Ollin.Core.Model.Components;

namespace Ollin.Core.Serialization;

/// <summary>Guarda y carga TileSet como asset JSON reutilizable.</summary>
public static class TileSetSerializer
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    public static void Save(TileSet tileSet, string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(tileSet, Options));
    }

    public static TileSet Load(string path) =>
        JsonSerializer.Deserialize<TileSet>(File.ReadAllText(path), Options)
            ?? throw new InvalidOperationException("TileSet inválido");
}
