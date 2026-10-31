using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Navigation;
using Ollin.Core.Model.Components;
using Ollin.Core.Projects;
using Ollin.Core.Serialization;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class AiAgent3DTests
{
    [Fact]
    public void PersigueAlObjetivoEnElPlanoXZ()
    {
        var scene = new Scene();

        var player = scene.Create("Jugador", new Vector2(0f, 4f));
        player.Tags.Add("player");

        var enemy = scene.Create("Enemigo", Vector2.Zero);
        var agent = enemy.AddComponent(new AiAgent3D
        {
            TargetTag = "player",
            Speed = 2f,
            VisionRadius = 10f,
        });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 10; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.True(agent.IsChasing);
            Assert.True(enemy.Transform.Position.Y > 0.4f, $"z={enemy.Transform.Position.Y}");
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void UnaParedBloqueaLaVision()
    {
        var scene = new Scene();

        var player = scene.Create("Jugador", new Vector2(0f, 4f));
        player.Tags.Add("player");

        var wall = scene.Create("Muro", new Vector2(0f, 2f));
        wall.AddComponent(new Wall3D { Size = new Vector3(4f, 3f, 0.5f), Collides = true });

        var enemy = scene.Create("Enemigo", Vector2.Zero);
        var agent = enemy.AddComponent(new AiAgent3D
        {
            TargetTag = "player",
            Speed = 2f,
            VisionRadius = 10f,
        });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.False(agent.IsChasing);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void LaPlantillaCorredorIncluyeAgente3D()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "ai3d-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "PasilloIA", template: ProjectTemplate.Corridor);
            var scene = SceneSerializer.Load(project.StartSceneFullPath);

            Assert.NotNull(scene.Find("Sombra")!.GetComponent<AiAgent3D>());
            Assert.Contains("player", scene.Find("Cámara FPS")!.Tags);
        }
        finally
        {
            if (Directory.Exists(parent))
            {
                Directory.Delete(parent, recursive: true);
            }
        }
    }
    [Fact]
    public void LaRejillaEncuentraUnCaminoAlrededorDeUnaPared()
    {
        var scene = new Scene();
        var wall = scene.Create("Muro", new Vector2(0f, 3f));
        wall.AddComponent(new Wall3D { Size = new Vector3(9f, 3f, 0.4f), Collides = true });

        var grid = NavGrid3D.Build(scene, 0.5f, 0.4f);

        Assert.True(grid.TryFindPath(new Vector2(0f, 0f), new Vector2(0f, 6f), out var path));
        Assert.True(path.Count > 2, string.Join(" | ", path));
        Assert.Contains(path, point => point.X > 4f);
    }
}