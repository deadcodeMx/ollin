using Ollin.Core.Model;
using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Serialization;
using Xunit;

namespace Ollin.Tests;

public sealed class TileSetTests
{
    [Fact]
    public void TileSetDefineColisionPorTile()
    {
        var layer = new TileMapLayer
        {
            Width = 2,
            Height = 1,
            Tiles = new[] { 0, 1 },
            TileSet = new TileSet
            {
                TileWidth = 16,
                TileHeight = 16,
                Tiles = new()
                {
                    new TileDefinition { Id = 0, CollisionSize = Vector2.Zero },
                    new TileDefinition { Id = 1, CollisionSize = new Vector2(1f, 1f), OneWay = true },
                },
            },
        };

        Assert.False(layer.IsSolidAt(0, 0));
        Assert.True(layer.IsSolidAt(1, 0));
        Assert.True(layer.DefinitionAt(1, 0)!.OneWay);
    }

    [Fact]
    public void UnaEntidadPuedeTenerVariasCapas()
    {
        var entity = new Entity { Name = "Nivel" };
        entity.AddComponent(new TileMapLayer { SortingOrder = -20 });
        entity.AddComponent(new TileMapLayer { SortingOrder = 10 });

        Assert.Equal(2, entity.GetComponents<TileMapLayer>().Count());
    }

    [Fact]
    public void TileSetSeGuardaYSeCargaComoAsset()
    {
        var path = Path.Combine(Path.GetTempPath(), $"ollin-tileset-{Guid.NewGuid():N}.json");
        var original = new TileSet
        {
            Name = "Mazmorra",
            TexturePath = "assets/tiles.png",
            Tiles = new() { new TileDefinition { Id = 4, Name = "Muro", CollisionSize = new Vector2(1f, 1f) } },
        };
        try
        {
            TileSetSerializer.Save(original, path);
            var loaded = TileSetSerializer.Load(path);
            Assert.Equal("Mazmorra", loaded.Name);
            Assert.Single(loaded.Tiles);
            Assert.Equal("Muro", loaded.Tiles[0].Name);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact]
    public void AutotileUsaMascaraDeVecinosYTerreno()
    {
        var layer = new TileMapLayer
        {
            Width = 3, Height = 3, Autotile = true,
            Tiles = new[] { 0, 0, 0, 0, 0, 0, 0, 0, 0 },
            TileSet = new TileSet
            {
                Tiles = new()
                {
                    new TileDefinition
                    {
                        Id = 0, Terrain = "pasto",
                        AutotileVariants = new() { 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25 },
                    },
                },
            },
        };

        var mask = layer.AutotileMask(1, 1);
        Assert.Equal(15, mask);
        Assert.Equal(25, layer.DisplayTileAt(1, 1));
        Assert.Equal(25, layer.TileSet!.ResolveTerrain("pasto", 15));
        Assert.Equal(-1, layer.TileSet!.ResolveTerrain("piedra", 15));
    }

    [Fact]
    public void PintadoPorLotesRellenaRectangulosYMapa()
    {
        var layer = new TileMapLayer { Width = 5, Height = 4, Tiles = Enumerable.Repeat(-1, 20).ToArray() };

        layer.PaintRect(1, 1, 2, 2, 3);
        Assert.Equal(3, layer.TileAt(1, 1));
        Assert.Equal(3, layer.TileAt(2, 2));
        Assert.Equal(-1, layer.TileAt(0, 0));

        layer.Fill(7);
        Assert.All(layer.Tiles, tile => Assert.Equal(7, tile));
    }

    [Fact]
    public void MapaGrandeAceptaPintadoPorLotes()
    {
        var layer = new TileMapLayer { Width = 256, Height = 256, TileSize = 1f };
        layer.PaintRect(32, 48, 96, 64, 4);

        Assert.Equal(4, layer.TileAt(32, 48));
        Assert.Equal(4, layer.TileAt(127, 111));
        Assert.Equal(-1, layer.TileAt(0, 0));
        Assert.Equal(256 * 256, layer.Tiles.Length);
    }

    [Fact]
    public void NavegacionYObstruccionPorTile()
    {
        var layer = new TileMapLayer
        {
            Width = 1, Height = 1, Tiles = new[] { 0 },
            TileSet = new TileSet
            {
                Tiles = new() { new TileDefinition { Id = 0, NavigationCost = -1, Occludes = true, CollisionSize = Vector2.Zero } },
            },
        };

        Assert.False(layer.IsWalkableAt(0, 0));
        Assert.True(layer.DefinitionAt(0, 0)!.Occludes);
    }
}
