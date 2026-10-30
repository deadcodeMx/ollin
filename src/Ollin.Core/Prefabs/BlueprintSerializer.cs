using System.Numerics;
using System.Text.Json;
using Ollin.Core.Model;
using Ollin.Core.Serialization;

namespace Ollin.Core.Prefabs;

/// <summary>
/// Blueprint: una entidad y sus hijos guardados como plantilla compuesta. Al
/// instanciarlo se recrea toda la jerarquía con identificadores nuevos.
/// </summary>
public sealed class BlueprintDocument
{
    public int FormatVersion { get; set; } = 1;

    public string? Name { get; set; }

    public Guid RootId { get; set; }

    public List<Entity> Entities { get; set; } = new();
}

public static class BlueprintSerializer
{
    public const string Extension = ".blueprint.json";

    public static string NormalizePath(string path)
    {
        var trimmed = path.Trim();

        if (trimmed.EndsWith(Extension, StringComparison.OrdinalIgnoreCase))
        {
            return trimmed;
        }

        if (trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
        {
            return trimmed[..^5] + Extension;
        }

        return trimmed + Extension;
    }

    public static BlueprintDocument Capture(Ollin.Core.Scenes.Scene scene, Entity root, string? name = null)
    {
        var document = new BlueprintDocument
        {
            Name = name ?? root.Name,
            RootId = root.Id,
        };

        Collect(scene, root, document.Entities);
        return document;
    }

    private static void Collect(Ollin.Core.Scenes.Scene scene, Entity entity, List<Entity> target)
    {
        target.Add(entity.Clone(newId: false));

        foreach (var child in scene.ChildrenOf(entity.Id))
        {
            Collect(scene, child, target);
        }
    }

    public static void Save(Ollin.Core.Scenes.Scene scene, Entity root, string path, string? name = null)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));

        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, JsonSerializer.Serialize(Capture(scene, root, name), SceneSerializer.Options));
    }

    public static BlueprintDocument Load(string path) =>
        JsonSerializer.Deserialize<BlueprintDocument>(File.ReadAllText(path), SceneSerializer.Options)
        ?? throw new InvalidOperationException("El blueprint no es válido.");

    /// <summary>Instancia el blueprint en la escena y devuelve la raíz creada.</summary>
    public static Entity Instantiate(Ollin.Core.Scenes.Scene scene, BlueprintDocument document, Vector2 position)
    {
        var map = new Dictionary<Guid, Guid>();
        var clones = new List<(Entity Source, Entity Clone)>();

        foreach (var source in document.Entities)
        {
            var clone = source.Clone();
            clone.Id = Guid.NewGuid();
            map[source.Id] = clone.Id;
            clones.Add((source, clone));
        }

        Entity? root = null;

        foreach (var (source, clone) in clones)
        {
            clone.ParentId = source.ParentId is { } parent && map.TryGetValue(parent, out var mapped) ? mapped : null;

            if (source.Id == document.RootId)
            {
                clone.Transform.Position = position;
                root = clone;
            }

            scene.Add(clone);
        }

        if (root is null && clones.Count > 0)
        {
            root = clones[0].Clone;
        }

        return root!;
    }
}
