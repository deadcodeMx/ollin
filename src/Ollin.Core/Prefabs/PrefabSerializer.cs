using System.Numerics;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ollin.Core.Model;
using Ollin.Core.Serialization;

namespace Ollin.Core.Prefabs;

/// <summary>
/// Entidades plantilla en archivos `*.prefab.json`: se guardan desde el editor y
/// se instancian en cualquier escena. Una instancia es una copia independiente.
/// </summary>
public static class PrefabSerializer
{
    public const int CurrentFormatVersion = 1;

    public const string Extension = ".prefab.json";

    public static string Serialize(Entity entity)
    {
        var document = new PrefabDocument
        {
            FormatVersion = CurrentFormatVersion,
            Prefab = entity,
        };

        return JsonSerializer.Serialize(document, SceneSerializer.Options);
    }

    public static Entity Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<PrefabDocument>(json, SceneSerializer.Options)
            ?? throw new InvalidOperationException("El prefab está vacío o no es válido.");

        var entity = document.Prefab ?? throw new InvalidOperationException("El archivo no contiene una entidad.");
        entity.ParentId = null;
        return entity;
    }

    public static void Save(Entity entity, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(entity));
    }

    public static Entity Load(string path) => Deserialize(File.ReadAllText(path));

    /// <summary>Copia lista para añadir a una escena, en la posición indicada.</summary>
    public static Entity Instantiate(Entity prefab, Vector2 position, string? name = null)
    {
        var instance = prefab.Clone();
        instance.Id = Guid.NewGuid();
        instance.ParentId = null;
        instance.Transform.Position = position;

        if (!string.IsNullOrWhiteSpace(name))
        {
            instance.Name = name;
        }

        return instance;
    }
}

/// <summary>Envoltura con metadatos del archivo de prefab.</summary>
public sealed class PrefabDocument
{
    public int FormatVersion { get; set; } = PrefabSerializer.CurrentFormatVersion;

    public string? Engine { get; set; } = "Ollin";

    public Entity? Prefab { get; set; }
}
