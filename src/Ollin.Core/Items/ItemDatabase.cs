using System.Text.Json;

namespace Ollin.Core.Items;

/// <summary>Definición de un objeto del juego (`items/<id>.json`).</summary>
public sealed class ItemDefinition
{
    public string Id { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public int MaxStack { get; set; } = 99;
}

/// <summary>Carga las definiciones de objetos del proyecto.</summary>
public sealed class ItemDatabase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, ItemDefinition> _items = new(StringComparer.OrdinalIgnoreCase);

    public ItemDatabase(string root)
    {
        Root = root;
        Reload();
    }

    public string Root { get; }

    public int Count => _items.Count;

    public IReadOnlyCollection<string> Ids => _items.Keys;

    public void Reload()
    {
        _items.Clear();
        var folder = Path.Combine(Root, "items");

        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var item = JsonSerializer.Deserialize<ItemDefinition>(File.ReadAllText(file), Options);
                if (item is null || string.IsNullOrWhiteSpace(item.Id))
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(item.Name))
                {
                    item.Name = item.Id;
                }

                _items[item.Id] = item;
            }
            catch
            {
                // Un objeto mal formado no debe impedir cargar el resto.
            }
        }
    }

    public ItemDefinition? Get(string? id) =>
        !string.IsNullOrWhiteSpace(id) && _items.TryGetValue(id, out var item) ? item : null;
}
