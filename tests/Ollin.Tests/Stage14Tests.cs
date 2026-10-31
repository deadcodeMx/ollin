using System.Numerics;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class WorldTransformTests
{
    [Fact]
    public void LaParedHijaUsaCentroYLimitesDeMundo()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", new Vector2(10f, 20f));
        var child = scene.Create("Hija", new Vector2(2f, 3f), parent.Id);
        var wall = child.AddComponent(new Wall3D { Size = new Vector3(2f, 4f, 2f) });

        var center = wall.Center;
        var (min, max) = wall.Bounds;

        Assert.Equal(12f, center.X, 3);
        Assert.Equal(2f, center.Y, 3);
        Assert.Equal(23f, center.Z, 3);
        Assert.Equal(11f, min.X, 3);
        Assert.Equal(22f, min.Z, 3);
        Assert.Equal(13f, max.X, 3);
        Assert.Equal(24f, max.Z, 3);
    }

    [Fact]
    public void LaCamaraHijaUsaElOjoDeMundo()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", new Vector2(10f, 20f));
        var child = scene.Create("Cámara", new Vector2(2f, 3f), parent.Id);
        var camera = child.AddComponent(new Camera3D { Height = 1.7f });

        Assert.Equal(new Vector3(12f, 1.7f, 23f), camera.Eye);
    }

    [Fact]
    public void SetWorldPositionConvierteALocal()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", new Vector2(10f, 20f));
        var child = scene.Create("Hija", Vector2.Zero, parent.Id);

        scene.SetWorldPosition(child, new Vector2(15f, 25f));

        Assert.Equal(new Vector2(5f, 5f), child.Transform.Position);
        Assert.Equal(new Vector2(15f, 25f), scene.GetWorldPosition(child));
    }

    [Fact]
    public void UnRecogibleHijoSeRecogePorDistanciaDeMundo()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", Vector2.Zero);
        var pickupEntity = scene.Create("Llave", new Vector2(0.2f, 0f), parent.Id);
        pickupEntity.AddComponent(new Pickup { Radius = 1f, ItemId = "llave" });

        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        var inventory = player.AddComponent(new Inventory { Capacity = 4 });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(inventory.Has("llave"));
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void ElJugadorHijoSeMueveEnMundo()
    {
        var scene = new Scene();
        var parent = scene.Create("Padre", new Vector2(10f, 10f));
        var child = scene.Create("Jugador", Vector2.Zero, parent.Id);
        child.AddComponent(new Camera3D());
        child.AddComponent(new FirstPersonController { Speed = 2f, LookWarmupSeconds = 0f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            InputState.SetKey(KeyCode.W, true);
            runtime.Tick(0.1f);

            Assert.True(scene.GetWorldPosition(child).Y > 10.1f, $"world={scene.GetWorldPosition(child)}");
            Assert.NotEqual(Vector2.Zero, child.Transform.Position);
        }
        finally
        {
            InputState.Reset();
            runtime.Stop();
        }
    }
}
