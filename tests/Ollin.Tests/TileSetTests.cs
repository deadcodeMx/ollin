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
}
