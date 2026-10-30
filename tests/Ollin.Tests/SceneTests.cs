using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public class SceneTests
{
    [Fact]
    public void AddRegistraLaEntidadYSuEscena()
    {
        var scene = new Scene();
        var entity = scene.Create("Héroe", new Vector2(1, 2));

        Assert.Single(scene.Entities);
        Assert.Same(scene, entity.Scene);
        Assert.Equal(new Vector2(1, 2), entity.Transform.Position);
    }

    [Fact]
    public void EliminarReparentaHijosAlAbuelo()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", Vector2.Zero);
        var child = scene.Create("Hijo", Vector2.Zero, parent.Id);
        var grandchild = scene.Create("Nieto", Vector2.Zero, child.Id);

        scene.Remove(child.Id);

        Assert.Null(scene.Find(child.Id));
        Assert.Equal(parent.Id, grandchild.ParentId);
        Assert.Equal(2, scene.Entities.Count);
    }

    [Fact]
    public void LaMatrizDeMundoAcumulaLaJerarquia()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", new Vector2(10, 0));
        var child = scene.Create("Hijo", new Vector2(0, 5), parent.Id);

        var world = scene.GetWorldPosition(child);

        Assert.Equal(10f, world.X, 3);
        Assert.Equal(5f, world.Y, 3);
    }

    [Fact]
    public void CloneCopiaComponentesConNuevaIdentidad()
    {
        var scene = new Scene();
        var original = scene.Create("Original", new Vector2(3, 4));
        original.AddComponent(new SpriteRenderer { Size = new Vector2(2, 2) });

        var copy = original.Clone();

        Assert.NotEqual(original.Id, copy.Id);
        Assert.Equal(original.Name, copy.Name);
        Assert.Equal(original.Transform.Position, copy.Transform.Position);
        Assert.Single(copy.Components);
        Assert.Equal(new Vector2(2, 2), copy.GetComponent<SpriteRenderer>()!.Size);
    }

    [Fact]
    public void GetMainCameraDevuelveLaCamaraPrincipal()
    {
        var scene = DefaultScene.Create();

        var camera = scene.GetMainCamera();

        Assert.NotNull(camera);
        Assert.Equal("Cámara principal", camera!.Owner!.Name);
    }

    [Fact]
    public void LaEscenaPorDefectoTieneSeisEntidades()
    {
        var scene = DefaultScene.Create();

        Assert.Equal(6, scene.Entities.Count);
        Assert.NotNull(scene.GetMainCamera());
        Assert.Equal(4, scene.Entities.Count(e => e.GetComponent<SpriteRenderer>() is not null));
    }
}
