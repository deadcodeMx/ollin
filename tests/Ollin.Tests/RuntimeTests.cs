using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class SceneRuntimeTests
{
    [Fact]
    public void ElRotadorGiraSegunElTiempoTranscurrido()
    {
        var scene = new Scene();
        var entity = scene.Create("Moneda", Vector2.Zero);
        entity.AddComponent(new RotatorComponent { DegreesPerSecond = 90f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        // 90°/s durante un segundo, en cuadros de 0.1 s (el runtime recorta deltas mayores).
        for (var i = 0; i < 10; i++)
        {
            runtime.Tick(0.1f);
        }

        Assert.Equal(MathF.PI / 2f, entity.Transform.Rotation, 3);
        Assert.True(runtime.IsPlaying);
    }

    [Fact]
    public void LaPausaCongelaLaSimulacion()
    {
        var scene = new Scene();
        var entity = scene.Create("Moneda", Vector2.Zero);
        entity.AddComponent(new RotatorComponent { DegreesPerSecond = 90f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(1f);
        var antes = entity.Transform.Rotation;

        runtime.TogglePause();
        runtime.Tick(5f);

        Assert.True(runtime.IsPaused);
        Assert.Equal(antes, entity.Transform.Rotation, 4);

        runtime.TogglePause();
        runtime.Tick(1f);

        Assert.False(runtime.IsPaused);
        Assert.True(entity.Transform.Rotation > antes);
    }

    [Fact]
    public void DetenerRestauraElEstadoAnteriorALaReproduccion()
    {
        var scene = new Scene();
        var coin = scene.Create("Moneda", new Vector2(4f, 1f));
        coin.AddComponent(new RotatorComponent { DegreesPerSecond = 360f });

        var crate = scene.Create("Caja", new Vector2(2f, 0f));
        crate.AddComponent(new OscillatorComponent { Amplitude = 1f, Frequency = 1f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(0.5f);

        Assert.NotEqual(0f, coin.Transform.Rotation);

        var restored = runtime.Stop();

        Assert.NotNull(restored);
        Assert.False(runtime.IsPlaying);

        var restoredCoin = restored!.Find("Moneda");
        var restoredCrate = restored.Find("Caja");
        Assert.NotNull(restoredCoin);
        Assert.NotNull(restoredCrate);
        Assert.Equal(0f, restoredCoin!.Transform.Rotation, 4);
        Assert.Equal(new Vector2(4f, 1f), restoredCoin.Transform.Position);
        Assert.Equal(new Vector2(2f, 0f), restoredCrate!.Transform.Position);
    }

    [Fact]
    public void DetenerSinReproducirNoDevuelveEscena()
    {
        var runtime = new SceneRuntime();

        Assert.Null(runtime.Stop());
        Assert.False(runtime.IsPlaying);
    }

    [Fact]
    public void ElDeltaSeRecortaParaEvitarSaltos()
    {
        var scene = new Scene();
        scene.Create("Vacía", Vector2.Zero);

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(5f);

        Assert.InRange(runtime.Elapsed, 0f, 0.1f);
    }

    [Fact]
    public void LosComponentesDeshabilitadosNoSeActualizan()
    {
        var scene = new Scene();
        var entity = scene.Create("Moneda", Vector2.Zero);
        entity.AddComponent(new RotatorComponent { DegreesPerSecond = 90f, Enabled = false });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(1f);

        Assert.Equal(0f, entity.Transform.Rotation, 4);
    }

    [Fact]
    public void ElOsciladorAnimaYVuelveAlOrigenAlDetener()
    {
        var scene = new Scene();
        var entity = scene.Create("Plataforma", new Vector2(0f, 3f));
        entity.AddComponent(new OscillatorComponent { Axis = new Vector2(0f, 1f), Amplitude = 0.5f, Frequency = 1f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(0.25f);

        Assert.NotEqual(3f, entity.Transform.Position.Y, 3);

        var restored = runtime.Stop();

        Assert.Equal(new Vector2(0f, 3f), restored!.Find("Plataforma")!.Transform.Position);
    }
}
