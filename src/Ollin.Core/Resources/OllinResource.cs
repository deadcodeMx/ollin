using System.Text.Json.Serialization;

namespace Ollin.Core.Resources;

/// <summary>Recurso cargable con identidad, ruta y dependencias.</summary>
public abstract class OllinResource
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public List<string> Dependencies { get; set; } = new();

    [JsonIgnore] public string Path { get; set; } = string.Empty;
}

/// <summary>TileSet empaquetado como recurso reutilizable.</summary>
public sealed class TileSetResource : OllinResource
{
    public Model.Components.TileSet TileSet { get; set; } = new();
}
