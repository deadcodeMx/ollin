using System.Numerics;
using Ollin.Core.Items;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class InventoryTests
{
    [Fact]
    public void AnadePilasYRespetaLaCapacidad()
    {
        var inventory = new Inventory { Capacity = 2 };

        Assert.Equal(4, inventory.Add("pocion", 4, maxStack: 3));
        Assert.Equal(4, inventory.Count("pocion"));
        Assert.Equal(2, inventory.Slots.Count);
        Assert.Equal(3, inventory.Slots[0].Quantity);
        Assert.Equal(1, inventory.Slots[1].Quantity);

        // La capacidad está llena: no entra nada más de otro objeto.
        Assert.Equal(0, inventory.Add("moneda", 5));
        Assert.True(inventory.Has("pocion", 4));
        Assert.False(inventory.Has("moneda"));
    }

    [Fact]
    public void QuitarEliminaPilasVacias()
    {
        var inventory = new Inventory { Capacity = 4 };
        inventory.Add("moneda", 3);

        Assert.Equal(2, inventory.Remove("moneda", 2));
        Assert.Equal(1, inventory.Count("moneda"));

        Assert.Equal(1, inventory.Remove("moneda"));
        Assert.Empty(inventory.Slots);
    }

    [Fact]
    public void ElClonCopiaElContenido()
    {
        var inventory = new Inventory { Capacity = 3 };
        inventory.Add("pocion", 2);

        var clone = (Inventory)inventory.CloneFor(new Entity());

        clone.Remove("pocion", 2);

        Assert.Equal(2, inventory.Count("pocion"));
        Assert.Equal(0, clone.Count("pocion"));
    }
}

public class StatsTests
{
    [Fact]
    public void AjustaYLimitaLosValores()
    {
        var stats = new Stats();
        stats.Set("Vida", 150f);
        Assert.Equal(100f, stats.Get("Vida"), 3);

        stats.Add("Vida", -30f);
        Assert.Equal(70f, stats.Get("Vida"), 3);

        stats.Add("Vida", -100f);
        Assert.Equal(0f, stats.Get("Vida"), 3);
    }

    [Fact]
    public void ElDesgasteBajaElValorConElTiempo()
    {
        var stats = new Stats();
        stats.Ensure("Hambre").DecayPerSecond = 10f;

        stats.OnUpdate(2f);
        Assert.Equal(80f, stats.Get("Hambre"), 3);
    }
}

public class DayNightTests
{
    [Fact]
    public void ElTiempoAvanzaYSeEnvuelve()
    {
        var cycle = new DayNightCycle { DayLengthSeconds = 10f, TimeOfDay = 0.9f };
        cycle.OnUpdate(2f);

        Assert.Equal(0.1f, cycle.TimeOfDay, 3);
    }

    [Fact]
    public void ElFactorDeDiaVaDeNocheAMediodia()
    {
        var cycle = new DayNightCycle { TimeOfDay = 0f };
        Assert.Equal(0f, cycle.DayFactor, 3);

        cycle.TimeOfDay = 0.5f;
        Assert.Equal(1f, cycle.DayFactor, 3);
    }

    [Fact]
    public void ElRuntimeAplicaElAmbienteALasCamaras()
    {
        var scene = new Scene();
        var clockEntity = scene.Create("Reloj", Vector2.Zero);
        var clock = clockEntity.AddComponent(new DayNightCycle
        {
            DayLengthSeconds = 100000f,
            TimeOfDay = 0.5f,
            DayAmbient = Vector4.One,
            NightAmbient = new Vector4(0.1f, 0.1f, 0.1f, 1f),
        });

        var cameraEntity = scene.Create("Cámara", Vector2.Zero);
        var camera = cameraEntity.AddComponent(new Camera2D());

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.016f);
            Assert.True(camera.Ambient.X > 0.99f);

            clock.TimeOfDay = 0f;
            runtime.Tick(0.016f);
            Assert.True(camera.Ambient.X < 0.2f);
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class ItemDatabaseTests
{
    [Fact]
    public void CargaObjetosDelProyecto()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "item-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "items"));
        File.WriteAllText(Path.Combine(root, "items", "pocion.json"),
            """{ "id": "pocion", "name": "Poción", "maxStack": 5 }""");

        try
        {
            var database = new ItemDatabase(root);
            var item = database.Get("pocion");

            Assert.NotNull(item);
            Assert.Equal("Poción", item!.Name);
            Assert.Equal(5, item.MaxStack);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
