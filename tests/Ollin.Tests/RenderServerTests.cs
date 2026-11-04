using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Rendering;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class RenderServerTests
{
    [Fact]
    public void RecolectaYOrdenaSpritesVisibles()
    {
        var scene = new Scene();
        var low = scene.Create("low", Vector2.Zero);
        low.AddComponent(new SpriteRenderer());
        low.AddComponent(new CanvasItem2D { ZIndex = -1 });
        var hidden = scene.Create("hidden", Vector2.Zero);
        hidden.AddComponent(new SpriteRenderer());
        hidden.AddComponent(new CanvasItem2D { Visible = false });
        var high = scene.Create("high", Vector2.Zero);
        high.AddComponent(new SpriteRenderer());
        high.AddComponent(new CanvasItem2D { ZIndex = 2 });

        var server = new RenderServer2D();
        server.Submit(scene);

        Assert.Equal(2, server.SubmittedSprites);
        Assert.Equal("low", server.Sprites[0].Entity.Name);
        Assert.Equal("high", server.Sprites[1].Entity.Name);
    }
}
