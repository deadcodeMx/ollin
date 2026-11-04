using Ollin.Core.Model;
using System.Numerics;
using Ollin.Core.Model.Components;
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
}
