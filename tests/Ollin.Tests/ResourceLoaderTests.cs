using Ollin.Core.Resources;
using Xunit;

namespace Ollin.Tests;

public sealed class ResourceLoaderTests
{
    [Fact]
    public void CargaTileSetYReutilizaCache()
    {
        var root = Path.Combine(Path.GetTempPath(), $"ollin-res-{Guid.NewGuid():N}");
        Directory.CreateDirectory(Path.Combine(root, "tilesets"));
        var path = Path.Combine(root, "tilesets", "dungeon.json");
        var previousRoot = ResourceLoader.Root;
        try
        {
            ResourceLoader.Root = root;
            ResourceLoader.Clear();
            var resource = new TileSetResource
            {
                TileSet = new() { Name = "Dungeon", TexturePath = "tiles.png" },
            };
            resource.Dependencies.Add("tilesets/base.json");
            File.WriteAllText(Path.Combine(root, "tilesets", "base.json"), "{\"type\":\"tileset\",\"id\":\"base\",\"tileset\":{}}");
            ResourceLoader.Save(resource, path);

            var first = ResourceLoader.LoadTileSet("res://tilesets/dungeon.json");
            var second = ResourceLoader.LoadTileSet(path);

            Assert.NotNull(first);
            Assert.Same(first, second);
            Assert.Equal("Dungeon", first!.TileSet.Name);
        }
        finally
        {
            ResourceLoader.Root = previousRoot;
            ResourceLoader.Clear();
            Directory.Delete(root, recursive: true);
        }
    }
}
