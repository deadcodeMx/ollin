using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Definición de un tile dentro de un TileSet.</summary>
public sealed class TileDefinition
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    /// <summary>Región de textura (x, y, ancho, alto). Si es cero se calcula por columnas.</summary>
    public Vector4 Region { get; set; }

    public Vector2 CollisionSize { get; set; } = Vector2.One;

    public Vector2 CollisionOffset { get; set; }

    public bool OneWay { get; set; }

    public string? Tag { get; set; }

    [JsonIgnore] public bool HasCollision => CollisionSize.X > 0f && CollisionSize.Y > 0f;
}

/// <summary>Conjunto de tiles con textura, tamaño y colisión por tile.</summary>
public sealed class TileSet
{
    public string? TexturePath { get; set; }

    public int TileWidth { get; set; } = 32;

    public int TileHeight { get; set; } = 32;

    public int Columns { get; set; } = 4;

    public List<TileDefinition> Tiles { get; set; } = new();

    public TileDefinition? Get(int id) => Tiles.FirstOrDefault(tile => tile.Id == id);

    public Vector4 RegionFor(int id)
    {
        var tile = Get(id);
        if (tile is not null && tile.Region.Z > 0f && tile.Region.W > 0f) return tile.Region;
        var columns = System.Math.Max(1, Columns);
        var column = id % columns;
        var row = id / columns;
        return new Vector4(column * TileWidth, row * TileHeight, TileWidth, TileHeight);
    }

    public TileSet Clone() => new()
    {
        TexturePath = TexturePath,
        TileWidth = TileWidth,
        TileHeight = TileHeight,
        Columns = Columns,
        Tiles = Tiles.Select(tile => new TileDefinition
        {
            Id = tile.Id, Name = tile.Name, Region = tile.Region, CollisionSize = tile.CollisionSize,
            CollisionOffset = tile.CollisionOffset, OneWay = tile.OneWay, Tag = tile.Tag,
        }).ToList(),
    };
}

/// <summary>
/// Capa de mosaicos en una rejilla. Varias capas pueden convivir en una entidad;
/// el TileSet define colisión y región por tile.
/// </summary>
public class TileMapLayer : Component
{
    public int Width { get; set; } = 16;

    public int Height { get; set; } = 8;

    public float TileSize { get; set; } = 1f;

    public Vector2 Offset { get; set; }

    public string? TexturePath { get; set; }

    public int TilesetColumns { get; set; } = 4;

    public int SortingOrder { get; set; } = -5;

    public bool Visible { get; set; } = true;

    public bool Collides { get; set; }

    public TileSet? TileSet { get; set; }

    public bool Autotile { get; set; }

    public int AutotileBase { get; set; }

    public int[] Tiles { get; set; } = CreateEmpty(16, 8);

    public string? EffectiveTexture => TileSet?.TexturePath ?? TexturePath;

    public int EffectiveColumns => TileSet?.Columns ?? TilesetColumns;

    protected static int[] CreateEmpty(int width, int height)
    {
        var tiles = new int[System.Math.Max(1, width) * System.Math.Max(1, height)];
        Array.Fill(tiles, -1);
        return tiles;
    }

    protected T CopyBase<T>(T copy, Entity newOwner) where T : TileMapLayer
    {
        copy.Enabled = Enabled;
        copy.Width = Width;
        copy.Height = Height;
        copy.TileSize = TileSize;
        copy.Offset = Offset;
        copy.TexturePath = TexturePath;
        copy.TilesetColumns = TilesetColumns;
        copy.SortingOrder = SortingOrder;
        copy.Visible = Visible;
        copy.Collides = Collides;
        copy.TileSet = TileSet?.Clone();
        copy.Autotile = Autotile;
        copy.AutotileBase = AutotileBase;
        copy.Tiles = (int[])Tiles.Clone();
        copy.Owner = newOwner;
        return copy;
    }

    public override Component CloneFor(Entity newOwner) => CopyBase(new TileMapLayer(), newOwner);

    public void Resize(int width, int height)
    {
        width = System.Math.Max(1, width);
        height = System.Math.Max(1, height);
        var resized = new int[width * height];
        Array.Fill(resized, -1);
        for (var y = 0; y < System.Math.Min(height, Height); y++)
            for (var x = 0; x < System.Math.Min(width, Width); x++)
                resized[y * width + x] = TileAt(x, y);
        Width = width;
        Height = height;
        Tiles = resized;
    }

    public int TileAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return -1;
        var index = y * Width + x;
        return index < Tiles.Length ? Tiles[index] : -1;
    }

    public void SetTile(int x, int y, int tile)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height) return;
        var index = y * Width + x;
        if (index >= Tiles.Length)
        {
            var resized = new int[Width * Height];
            Array.Copy(Tiles, resized, Tiles.Length);
            Tiles = resized;
        }
        Tiles[index] = tile;
    }

    public int DisplayTileAt(int x, int y)
    {
        var tile = TileAt(x, y);
        if (!Autotile || tile < 0) return tile;
        var mask = 0;
        if (TileAt(x, y - 1) >= 0) mask |= 1;
        if (TileAt(x + 1, y) >= 0) mask |= 2;
        if (TileAt(x, y + 1) >= 0) mask |= 4;
        if (TileAt(x - 1, y) >= 0) mask |= 8;
        return AutotileBase + mask;
    }

    public TileDefinition? DefinitionAt(int x, int y) => TileSet?.Get(TileAt(x, y));

    public bool IsSolidAt(int x, int y)
    {
        var tile = TileAt(x, y);
        if (tile < 0) return false;
        var definition = TileSet?.Get(tile);
        return definition is not null ? definition.HasCollision : Collides;
    }

    public (int X, int Y) WorldToCell(Vector2 localPoint)
    {
        var local = localPoint - Offset;
        return ((int)MathF.Floor(local.X / MathF.Max(0.0001f, TileSize)),
                (int)MathF.Floor(-local.Y / MathF.Max(0.0001f, TileSize)));
    }

    public Vector2 CellOrigin(int x, int y) =>
        new(x * TileSize + Offset.X, -(y + 1) * TileSize + Offset.Y);
}

/// <summary>Compatibilidad: Tilemap es una capa única con el comportamiento clásico.</summary>
public sealed class Tilemap : TileMapLayer
{
    public override Component CloneFor(Entity newOwner) => CopyBase(new Tilemap(), newOwner);
}
