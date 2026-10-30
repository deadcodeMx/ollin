using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;
using Ollin.Rendering;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class LightingTests
{
    [Fact]
    public void LaLuzSeGuardaYSeRecupera()
    {
        var scene = new Scene();
        var lamp = scene.Create("Lámpara", new Vector2(1f, 2f));
        lamp.AddComponent(new Light2D
        {
            Kind = LightKind.Cone,
            Color = new Vector4(1f, 0.8f, 0.4f, 1f),
            Radius = 7.5f,
            Intensity = 0.8f,
            ConeAngle = 45f,
            FlickerAmplitude = 0.2f,
        });

        var restored = SceneSerializer.Deserialize(SceneSerializer.Serialize(scene));
        var light = restored.Find("Lámpara")!.GetComponent<Light2D>()!;

        Assert.Equal(LightKind.Cone, light.Kind);
        Assert.Equal(7.5f, light.Radius, 3);
        Assert.Equal(45f, light.ConeAngle, 3);
        Assert.Equal(0.2f, light.FlickerAmplitude, 3);
    }

    [Fact]
    public void ElParpadeoSeMantieneEnUnRangoRazonable()
    {
        var light = new Light2D { Intensity = 1f, FlickerAmplitude = 0.9f, FlickerSpeed = 9f };

        for (var i = 0; i < 400; i++)
        {
            light.OnUpdate(0.016f);
            Assert.InRange(light.CurrentIntensity, 0f, 2.5f);
        }
    }

    [Fact]
    public void ElParalajeDesplazaElSpriteRespectoALaCamara()
    {
        var camera = new Vector2(10f, -4f);

        Assert.Equal(Vector2.Zero, Renderer2D.ParallaxOffset(camera, 1f));
        Assert.Equal(new Vector2(10f, -4f), Renderer2D.ParallaxOffset(camera, 0f));
        Assert.Equal(new Vector2(5f, -2f), Renderer2D.ParallaxOffset(camera, 0.5f));
    }

    [Fact]
    public void LaCamaraSigueAlJugadorConSuavizado()
    {
        var scene = new Scene();
        var camera = scene.Create("Cámara", Vector2.Zero);
        camera.AddComponent(new Camera2D { FollowTag = "player", FollowSmoothing = 10f });

        var player = scene.Create("Jugador", new Vector2(10f, 0f));
        player.Tags.Add("player");

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        for (var i = 0; i < 20; i++)
        {
            runtime.Tick(0.1f);
        }

        Assert.InRange(camera.Transform.Position.X, 9f, 10f);
        Assert.InRange(camera.Transform.Position.Y, -0.01f, 0.01f);
    }

    [Fact]
    public void ElAmbienteDeLaCamaraSeGuarda()
    {
        var scene = new Scene();
        var camera = scene.Create("Cámara", Vector2.Zero);
        camera.AddComponent(new Camera2D
        {
            Ambient = new Vector4(0.2f, 0.3f, 0.4f, 1f),
            FollowTag = "player",
        });

        var restored = SceneSerializer.Deserialize(SceneSerializer.Serialize(scene));
        var loaded = restored.Find("Cámara")!.GetComponent<Camera2D>()!;

        Assert.Equal(0.2f, loaded.Ambient.X, 3);
        Assert.Equal("player", loaded.FollowTag);
    }
}
