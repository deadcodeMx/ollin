using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Projects;
using Ollin.Core.Serialization;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public class Camera3DTests
{
    [Fact]
    public void ProyectaElCentroDeLaMiradaEnElCentroDeLaPantalla()
    {
        var scene = new Scene();
        var entity = scene.Create("Camera", new Vector2(0f, 0f));
        var camera = entity.AddComponent(new Camera3D { Height = 1.7f });

        var ok = camera.Project(new Vector3(0f, 1.7f, 10f), 800, 600, out var screen, out var depth);

        Assert.True(ok);
        Assert.Equal(10f, depth, 3);
        Assert.Equal(400f, screen.X, 3);
        Assert.Equal(300f, screen.Y, 3);
    }

    [Fact]
    public void LosPuntosDetrasDeLaCamaraNoSeProyectan()
    {
        var scene = new Scene();
        var entity = scene.Create("Camera", Vector2.Zero);
        var camera = entity.AddComponent(new Camera3D());

        Assert.False(camera.Project(new Vector3(0f, 1.7f, -5f), 800, 600, out _, out _));
    }

    [Fact]
    public void LaEscalaBajaConLaDistancia()
    {
        var scene = new Scene();
        var entity = scene.Create("Camera", Vector2.Zero);
        var camera = entity.AddComponent(new Camera3D());

        Assert.True(camera.ScaleAt(2f, 600) > camera.ScaleAt(8f, 600));
    }
}

public class Wall3DTests
{
    [Fact]
    public void LosLimitesSeCalculanDesdeElTransform()
    {
        var wall = new Wall3D { Size = new Vector3(2f, 3f, 4f) };
        var entity = new Entity { Transform = { Position = new Vector2(10f, 20f) } };
        entity.AddComponent(wall);

        var (min, max) = wall.Bounds;

        Assert.Equal(new Vector3(9f, 0f, 18f), min);
        Assert.Equal(new Vector3(11f, 3f, 22f), max);
        Assert.Equal(new Vector3(10f, 1.5f, 20f), wall.Center);
    }
}

public class FirstPersonCollisionTests
{
    [Fact]
    public void ElControladorSeDetieneAntesDeLaPared()
    {
        var scene = new Scene();

        var cameraEntity = scene.Create("Cámara FPS", new Vector2(0f, 0f));
        cameraEntity.AddComponent(new Camera3D());
        cameraEntity.AddComponent(new FirstPersonController { Speed = 4f, CollisionRadius = 0.3f });

        var wall = scene.Create("Pared", new Vector2(2f, 0f));
        wall.AddComponent(new Wall3D { Size = new Vector3(0.4f, 3f, 5f), Collides = true });

        var runtime = new Ollin.Runtime.SceneRuntime();
        runtime.Play(scene);

        try
        {
            Ollin.Core.Input.InputState.SetKey(Ollin.Core.Input.KeyCode.D, true);

            for (var i = 0; i < 30; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.True(cameraEntity.Transform.Position.X < 1.8f, $"x={cameraEntity.Transform.Position.X}");
            Assert.True(cameraEntity.Transform.Position.X > 0.1f, $"x={cameraEntity.Transform.Position.X}");
        }
        finally
        {
            Ollin.Core.Input.InputState.Reset();
            runtime.Stop();
        }
    }
}

public class CorridorTemplateTests
{
    [Fact]
    public void LaPlantillaCorredorTieneCamaraControladorYParedes()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "corridor-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "Pasillo", template: ProjectTemplate.Corridor);
            var scene = SceneSerializer.Load(project.StartSceneFullPath);

            Assert.NotNull(scene.GetMainCamera3D());

            var camera = scene.Find("Cámara FPS")!;
            Assert.NotNull(camera.GetComponent<FirstPersonController>());

            Assert.NotNull(scene.Find("Pared izquierda")!.GetComponent<Wall3D>());
            Assert.NotNull(scene.Find("Pared derecha")!.GetComponent<Wall3D>());
            Assert.NotNull(scene.Find("Puerta")!.GetComponent<Wall3D>());
            Assert.NotNull(scene.Find("Sombra")!.GetComponent<SpriteRenderer>());

            // El Vector3 de la caja debe sobrevivir al JSON (regresión del formato).
            var wall = scene.Find("Pared derecha")!.GetComponent<Wall3D>()!;
            Assert.Equal(new Vector3(0.4f, 3.6f, 26f), wall.Size);
            Assert.True(wall.Collides);

            using var workspace = new Ollin.Scripting.ScriptWorkspace(project.RootPath);
            var result = workspace.Reload();
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Select(d => d.Text)));
            Assert.Contains(result.Types, type => type.Name == "Pasillo");
        }
        finally
        {
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }
}
