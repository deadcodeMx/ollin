using System.Numerics;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Rendering;
using Xunit;

namespace Ollin.Tests;

public class CameraMathTests
{
    [Fact]
    public void PantallaYWorldSonInversas()
    {
        const int width = 800;
        const int height = 600;
        var camera = new Vector2(3f, -2f);
        var screen = new Vector2(120f, 540f);

        var world = CameraMath.ScreenToWorld(camera, 6f, 1f, width, height, screen);
        var back = CameraMath.WorldToScreen(camera, 6f, 1f, width, height, world);

        Assert.Equal(screen.X, back.X, 3);
        Assert.Equal(screen.Y, back.Y, 3);
    }

    [Fact]
    public void ElCentroDePantallaEsLaPosicionDeLaCamara()
    {
        var world = CameraMath.ScreenToWorld(new Vector2(5f, 7f), 6f, 2f, 1024, 768, new Vector2(512f, 384f));

        Assert.Equal(5f, world.X, 3);
        Assert.Equal(7f, world.Y, 3);
    }

    [Fact]
    public void MasZoomImplicaMasPixelesPorUnidad()
    {
        var normal = CameraMath.PixelsPerUnit(6f, 1f, 600);
        var zoom = CameraMath.PixelsPerUnit(6f, 2f, 600);

        Assert.Equal(50f, normal, 3);
        Assert.Equal(100f, zoom, 3);
    }

    [Fact]
    public void LaYDeMundoCreceHaciaArriba()
    {
        var above = CameraMath.WorldToScreen(Vector2.Zero, 6f, 1f, 800, 600, new Vector2(0, 1));
        var below = CameraMath.WorldToScreen(Vector2.Zero, 6f, 1f, 800, 600, new Vector2(0, -1));

        Assert.True(above.Y < below.Y);
    }
}

public class HitTestTests
{
    private static Entity CreateSprite(Scene scene, Vector2 position, Vector2 size, float rotation = 0f)
    {
        var entity = scene.Create("Sprite", position);
        entity.Transform.Rotation = rotation;
        entity.AddComponent(new SpriteRenderer { Size = size });
        return entity;
    }

    [Fact]
    public void UnPuntoDentroDelSpriteAcierta()
    {
        var scene = new Scene();
        var entity = CreateSprite(scene, new Vector2(2, 0), new Vector2(2, 2));

        Assert.True(Renderer2D.HitTest(scene, entity, new Vector2(2, 0)));
        Assert.True(Renderer2D.HitTest(scene, entity, new Vector2(2.9f, 0.9f)));
    }

    [Fact]
    public void UnPuntoFueraDelSpriteNoAcierta()
    {
        var scene = new Scene();
        var entity = CreateSprite(scene, new Vector2(2, 0), new Vector2(2, 2));

        Assert.False(Renderer2D.HitTest(scene, entity, new Vector2(0, 0)));
        Assert.False(Renderer2D.HitTest(scene, entity, new Vector2(2, 1.5f)));
    }

    [Fact]
    public void ElOrigenDesplazaElAreaDeAcierto()
    {
        var scene = new Scene();
        var entity = scene.Create("Sprite", new Vector2(5, 0));
        entity.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(2, 2),
            Origin = new Vector2(0f, 1f), // esquina inferior izquierda
        });

        Assert.True(Renderer2D.HitTest(scene, entity, new Vector2(5.5f, -1.5f)));
        Assert.False(Renderer2D.HitTest(scene, entity, new Vector2(4.5f, 0.5f)));
    }

    [Fact]
    public void LaRotacionSeRespetaEnElAcierto()
    {
        var scene = new Scene();
        // Sprite de 4x0.5 rotado 90°: pasa a ser vertical (0.5 de ancho, 4 de alto).
        var entity = CreateSprite(scene, Vector2.Zero, new Vector2(4f, 0.5f), MathF.PI / 2f);

        Assert.True(Renderer2D.HitTest(scene, entity, new Vector2(0f, 1.5f)));
        Assert.False(Renderer2D.HitTest(scene, entity, new Vector2(1.5f, 0f)));
    }
}
