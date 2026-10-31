using System.Numerics;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class MouseLookTests
{
    [Fact]
    public void ElDeltaBloqueadoSeConsumeUnaVez()
    {
        MouseLook.IsLocked = true;
        MouseLook.Reset();
        MouseLook.AddDelta(new Vector2(10f, -4f));

        Assert.Equal(new Vector2(10f, -4f), MouseLook.ConsumeDelta());
        Assert.Equal(Vector2.Zero, MouseLook.ConsumeDelta());
        MouseLook.Disable();
    }

    [Fact]
    public void LaSensibilidadSeparadaMueveLosDosEjes()
    {
        var (runtime, camera, controller) = CreateController();
        controller.LookWarmupSeconds = 0f;
        controller.MouseSensitivityX = 0.01f;
        controller.MouseSensitivityY = 0.005f;
        MouseLook.IsLocked = true;
        MouseLook.AddDelta(new Vector2(100f, 100f));

        try
        {
            runtime.Tick(0.016f);
            Assert.Equal(1f, camera.Yaw, 3);
            Assert.Equal(-0.5f, camera.Pitch, 3);
        }
        finally
        {
            MouseLook.Disable();
            runtime.Stop();
        }
    }

    [Fact]
    public void InvertirYCambiaElSignoDelPitch()
    {
        var (runtime, camera, controller) = CreateController();
        controller.LookWarmupSeconds = 0f;
        controller.MouseSensitivityY = 0.01f;
        controller.InvertY = true;
        MouseLook.IsLocked = true;
        MouseLook.AddDelta(new Vector2(0f, 100f));

        try
        {
            runtime.Tick(0.016f);
            Assert.Equal(1f, camera.Pitch, 3);
        }
        finally
        {
            MouseLook.Disable();
            runtime.Stop();
        }
    }

    [Fact]
    public void ElSuavizadoNoSaltaDirectamenteAlObjetivo()
    {
        var (runtime, camera, controller) = CreateController();
        controller.LookWarmupSeconds = 0f;
        controller.MouseSensitivityX = 0.01f;
        controller.LookSmoothing = 0.8f;
        MouseLook.IsLocked = true;
        MouseLook.AddDelta(new Vector2(100f, 0f));

        try
        {
            runtime.Tick(0.016f);
            Assert.True(camera.Yaw > 0f, $"yaw={camera.Yaw}");
            Assert.True(camera.Yaw < 1f, $"yaw={camera.Yaw}");
        }
        finally
        {
            MouseLook.Disable();
            runtime.Stop();
        }
    }

    private static (SceneRuntime Runtime, Camera3D Camera, FirstPersonController Controller) CreateController()
    {
        var scene = new Scene();
        var entity = scene.Create("Jugador", Vector2.Zero);
        var camera = entity.AddComponent(new Camera3D());
        var controller = entity.AddComponent(new FirstPersonController
        {
            LookWithMouse = true,
            LookWarmupSeconds = 0f,
        });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        return (runtime, camera, controller);
    }
}
