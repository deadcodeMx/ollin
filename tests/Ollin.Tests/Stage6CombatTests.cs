using System.Numerics;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class HealthTests
{
    [Fact]
    public void ElDanoBajaLaSaludYDesactivaAlMorir()
    {
        var scene = new Scene();
        var enemy = scene.Create("Enemigo", Vector2.Zero);
        var health = enemy.AddComponent(new Health { Max = 20f, Current = 20f, DeactivateOnDeath = true });

        health.TakeDamage(25f);

        Assert.True(health.IsDead);
        Assert.False(enemy.Active);
    }
}

public class WeaponTests
{
    [Fact]
    public void ElRayoDanaAlObjetivoEnfrentado()
    {
        var scene = new Scene();
        var cameraEntity = scene.Create("Cámara", Vector2.Zero);
        cameraEntity.AddComponent(new Camera3D());
        cameraEntity.AddComponent(new Weapon { Damage = 30f, Range = 20f, Automatic = true });

        var target = scene.Create("Objetivo", new Vector2(0f, 5f));
        var health = target.AddComponent(new Health { Max = 100f, Current = 100f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            InputState.SetMouseButton(0, true);
            runtime.Tick(0.05f);
            Assert.Equal(70f, health.Current, 3);
        }
        finally
        {
            InputState.Reset();
            runtime.Stop();
        }
    }
}

public class PickupTests
{
    [Fact]
    public void RecogerUnaLlaveLaAnadeAlInventario()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        var inventory = player.AddComponent(new Inventory { Capacity = 4 });

        var key = scene.Create("Llave", new Vector2(0.2f, 0f));
        key.AddComponent(new Pickup { Kind = PickupKind.Item, ItemId = "llave", Radius = 1f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(inventory.Has("llave"));
            Assert.False(key.Active);
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class DoorTests
{
    [Fact]
    public void LaPuertaSeAbreConLaLlave()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        player.AddComponent(new Inventory { Capacity = 4 }).Add("llave", 1);

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 1f));
        var wall = doorEntity.AddComponent(new Wall3D { Size = new Vector3(2f, 3f, 0.3f) });
        var door = doorEntity.AddComponent(new Door { KeyItemId = "llave", Speed = 2f, OpenHeight = 3f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            InputState.SetKey(KeyCode.E, true);
            runtime.Tick(0.05f);
            Assert.True(door.Open);

            for (var i = 0; i < 10; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.True(wall.BaseY > 0f, $"baseY={wall.BaseY}");
        }
        finally
        {
            InputState.Reset();
            runtime.Stop();
        }
    }
}

public class WinLoseRulesTests
{
    [Fact]
    public void GanaCuandoNoQuedanEnemigos()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        player.AddComponent(new Health { Current = 100f, Max = 100f });

        var rules = player.AddComponent(new WinLoseRules { EnemiesTag = "enemy" });

        var enemy = scene.Create("Enemigo", new Vector2(0f, 5f));
        enemy.Tags.Add("enemy");
        var health = enemy.AddComponent(new Health { Max = 10f, Current = 10f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            health.TakeDamage(10f);
            runtime.Tick(0.05f);

            Assert.True(rules.Finished);
        }
        finally
        {
            runtime.Stop();
        }
    }
}

