using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class NavigationTests
{
    [Fact]
    public void EncuentraRutaDentroDelPoligono()
    {
        var scene = new Scene();
        var region = scene.Create("Region", Vector2.Zero);
        region.AddComponent(new NavigationPolygon2D
        {
            Points = new() { new(-5f, -3f), new(5f, -3f), new(5f, 3f), new(-5f, 3f) },
        });

        var path = NavigationGeometry.FindPath(scene, new Vector2(-4f, 0f), new Vector2(4f, 0f));

        Assert.True(path.Count >= 2);
        Assert.Equal(new Vector2(-4f, 0f), path[0]);
        Assert.Equal(new Vector2(4f, 0f), path[^1]);
    }
}
