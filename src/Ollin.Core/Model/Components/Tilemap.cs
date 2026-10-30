using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Mapa de mosaicos en rejilla. Cada celda guarda un índice del tileset
/// (-1 = vacía). El renderer dibuja solo las celdas visibles.
/// </summary>
public sealed class Tilemap : Component
{
    public int Width { get; set; } = 16;

    public int Height { get; set; } = 8;

    /// <summary>Tamaño de cada celda en unidades de mundo.</summary>
    public float TileSize { get; set; } = 1f;

    /// <summary>Textura con la rejilla de mosaicos.</summary>
    public string? TexturePath { get; set; }

    /// <summary>Columnas del tileset (el alto se deduce del número de mosaicos).</summary>
    public int TilesetColumns { get; set; } = 4;

    public int SortingOrder { get; set; } = -5;

    /// <summary>Índices por fila: <c>Tiles[y * Width + x]</c> (-1 = celda vacía).</summary>
    public int[] Tiles { get; set; } = CreateEmpty(16, 8);

    private static int[] CreateEmpty(int width, int height)
    {
        var tiles = new int[System.Math.Max(1, width) * System.Math.Max(1, height)];
        Array.Fill(tiles, -1);
        return tiles;
    }

    public override Component CloneFor(Entity newOwner) => new Tilemap
    {
        Enabled = Enabled,
        Width = Width,
        Height = Height,
        TileSize = TileSize,
        TexturePath = TexturePath,
        TilesetColumns = TilesetColumns,
        SortingOrder = SortingOrder,
        Tiles = (int[])Tiles.Clone(),
        Owner = newOwner,
    };

    /// <summary>Ajusta el arreglo al tamaño actual conservando lo que quepa.</summary>
    public void Resize(int width, int height)
    {
        width = System.Math.Max(1, width);
        height = System.Math.Max(1, height);
        var resized = new int[width * height];
        Array.Fill(resized, -1);

        for (var y = 0; y < System.Math.Min(height, Height); y++)
        {
            for (var x = 0; x < System.Math.Min(width, Width); x++)
            {
                resized[y * width + x] = TileAt(x, y);
            }
        }

        Width = width;
        Height = height;
        Tiles = resized;
    }

    public int TileAt(int x, int y)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return -1;
        }

        var index = y * Width + x;
        return index < Tiles.Length ? Tiles[index] : -1;
    }

    public void SetTile(int x, int y, int tile)
    {
        if (x < 0 || y < 0 || x >= Width || y >= Height)
        {
            return;
        }

        var index = y * Width + x;
        if (index >= Tiles.Length)
        {
            var resized = new int[Width * Height];
            Array.Copy(Tiles, resized, Tiles.Length);
            Tiles = resized;
        }

        Tiles[index] = tile;
    }

    /// <summary>Convierte un punto de mundo (local a la entidad) a celda.</summary>
    public (int X, int Y) WorldToCell(Vector2 localPoint)
    {
        var cellX = (int)MathF.Floor(localPoint.X / MathF.Max(0.0001f, TileSize));
        var cellY = (int)MathF.Floor(-localPoint.Y / MathF.Max(0.0001f, TileSize));
        return (cellX, cellY);
    }

    /// <summary>Esquina superior izquierda de una celda en coordenadas locales.</summary>
    public Vector2 CellOrigin(int x, int y) => new(x * TileSize, -(y + 1) * TileSize);
}
