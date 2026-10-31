using System.Numerics;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class ProjectileTests
{
    [Fact]
    public void ElProyectilAvanzaYDana()
    {
        var scene = new Scene();
        var target = scene.Create("Objetivo", new Vector2(0f, 2f));
        var health = target.AddComponent(new Health { Max = 100f, Current = 100f });

        Projectile.Spawn(scene, Vector2.Zero, Vector2.UnitY, 25f, Guid.Empty, speed: 20f);

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 5; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.True(health.Current < 100f, $"vida={health.Current}");
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class WeaponAmmoTests
{
    [Fact]
    public void RecargaElCargadorDesdeLaReserva()
    {
        var scene = new Scene();
        var camera = scene.Create("Cámara", Vector2.Zero);
        camera.AddComponent(new Camera3D());
        var weapon = camera.AddComponent(new Weapon
        {
            Ammo = 1,
            MagazineSize = 1,
            ReserveAmmo = 1,
            ReloadSeconds = 0.2f,
            Automatic = true,
        });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            InputState.SetMouseButton(0, true);
            runtime.Tick(0.05f);
            Assert.Equal(0, weapon.Ammo);

            InputState.SetMouseButton(0, false);
            InputState.SetKey(KeyCode.R, true);
            runtime.Tick(0.05f);
            InputState.SetKey(KeyCode.R, false);

            for (var i = 0; i < 10; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.Equal(1, weapon.Ammo);
            Assert.Equal(0, weapon.ReserveAmmo);
        }
        finally
        {
            InputState.Reset();
            runtime.Stop();
        }
    }
}

public class RespawnTests
{
    [Fact]
    public void ReapareceEnElPuntoDeControl()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", new Vector2(1f, 2f));
        player.Tags.Add("player");
        var health = player.AddComponent(new Health { Max = 50f, Current = 50f, DeactivateOnDeath = false });
        var respawn = player.AddComponent(new Respawn { Lives = 2, RespawnSeconds = 0.2f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            respawn.SetCheckpoint(new Vector2(3f, 4f));
            health.TakeDamage(60f);
            runtime.Tick(0.05f);
            Assert.Equal(1, respawn.LivesRemaining);

            for (var i = 0; i < 10; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.Equal(50f, health.Current, 3);
            Assert.Equal(new Vector2(3f, 4f), player.Transform.Position);
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class Stage6InteractionTests
{
    [Fact]
    public void UnBotonAbreLaPuerta()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 2f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(2f, 3f, 0.3f) });

        var buttonEntity = scene.Create("Botón", new Vector2(0f, 0.5f));
        buttonEntity.AddComponent(new Button3D { Radius = 2f, TargetName = "Puerta", Action = ActionKind.OpenDoor });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            InputState.SetKey(KeyCode.E, true);
            runtime.Tick(0.05f);
            Assert.True(door.Open);
        }
        finally
        {
            InputState.Reset();
            runtime.Stop();
        }
    }

    [Fact]
    public void UnDisparadorAbreLaPuertaAlEntrar()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 3f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(2f, 3f, 0.3f) });

        var trigger = scene.Create("Trigger", new Vector2(0f, 0.5f));
        trigger.AddComponent(new Trigger3D { Radius = 2f, TargetName = "Puerta", Action = ActionKind.OpenDoor });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(door.Open);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void ElGeneradorCreaUnaCopiaDeLaPlantilla()
    {
        var scene = new Scene();
        var template = scene.Create("Plantilla", new Vector2(5f, 5f));
        template.Active = false;
        template.AddComponent(new SpriteRenderer());

        var spawner = scene.Create("Generador", Vector2.Zero);
        spawner.AddComponent(new Spawner3D
        {
            TemplateName = "Plantilla",
            Interval = 0.1f,
            MaxAlive = 2,
            AutoStart = true,
        });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 20; i++)
            {
                runtime.Tick(0.1f);
            }

            Assert.Equal(2, scene.Entities.Count(entity => entity.Name.StartsWith("Plantilla ") && entity.Active));
        }
        finally
        {
            runtime.Stop();
        }
    }
}
