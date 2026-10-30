using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Pathfinding;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class PathfinderTests
{
    [Fact]
    public void EncuentraUnaRutaEnLineaRecta()
    {
        var path = GridPathfinder.FindPath(5, 5, new GridPoint(0, 0), new GridPoint(4, 0), (_, _) => true);

        Assert.NotNull(path);
        Assert.Equal(4, path!.Count);
        Assert.Equal(new GridPoint(4, 0), path[^1]);
    }

    [Fact]
    public void RodeaUnMuro()
    {
        // Muro en la columna 2, salvo la fila 4.
        bool Walkable(int x, int y) => x != 2 || y == 4;

        var path = GridPathfinder.FindPath(5, 5, new GridPoint(0, 0), new GridPoint(4, 0), Walkable, allowDiagonal: false);

        Assert.NotNull(path);
        Assert.Contains(path!, cell => cell.Y == 4);
        Assert.DoesNotContain(path!, cell => cell.X == 2 && cell.Y != 4);
    }

    [Fact]
    public void DevuelveNullSiNoHayRuta()
    {
        // Objetivo encerrado por completo en (2,2).
        bool Walkable(int x, int y)
        {
            if (x == 2 && y == 2)
            {
                return true;
            }

            var blockedRing = x is >= 1 and <= 3 && y is >= 1 and <= 3;
            return !blockedRing;
        }

        var path = GridPathfinder.FindPath(5, 5, new GridPoint(0, 0), new GridPoint(2, 2), Walkable);
        Assert.Null(path);
    }

    [Fact]
    public void NoCortaEsquinasEnDiagonal()
    {
        // (1,0) y (0,1) bloqueadas: no se puede ir en diagonal de (0,0) a (1,1).
        bool Walkable(int x, int y) => !((x == 1 && y == 0) || (x == 0 && y == 1));

        var path = GridPathfinder.FindPath(3, 3, new GridPoint(0, 0), new GridPoint(1, 1), Walkable, allowDiagonal: true);

        Assert.Null(path);
    }
}

public class AiAgentTests
{
    [Fact]
    public void PersigueAlObjetivoCuandoLoVe()
    {
        var scene = new Scene();

        var player = scene.Create("Jugador", new Vector2(3f, 0f));
        player.Tags.Add("player");

        var enemy = scene.Create("Enemigo", Vector2.Zero);
        var agent = enemy.AddComponent(new AiAgent
        {
            TargetTag = "player",
            Speed = 4f,
            VisionRadius = 10f,
        });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 6; i++)
            {
                runtime.Tick(0.1f);
            }

            Assert.True(agent.IsChasing);
            Assert.True(enemy.Transform.Position.X > 1f, $"x={enemy.Transform.Position.X}");
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class SaveServiceTests
{
    [Fact]
    public void GuardaYCargaEscenaConComponentes()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "save-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "partida.json");

        try
        {
            var scene = new Scene { Name = "Nivel guardado" };
            var player = scene.Create("Jugador", new Vector2(4f, -1f));
            player.Tags.Add("player");

            var inventory = player.AddComponent(new Inventory { Capacity = 5 });
            inventory.Add("pocion", 2);

            var stats = player.AddComponent(new Stats());
            stats.Set("Vida", 70f);

            player.AddComponent(new AiAgent { TargetTag = "player" });

            SaveService.Save(scene, path, "scenes/nivel.json");

            var loaded = SaveService.Load(path);
            var loadedPlayer = loaded.Scene.Find("Jugador")!;

            Assert.Equal("Nivel guardado", loaded.Scene.Name);
            Assert.Equal("scenes/nivel.json", loaded.ScenePath);
            Assert.Equal(4f, loadedPlayer.Transform.Position.X, 3);
            Assert.Equal(2, loadedPlayer.GetComponent<Inventory>()!.Count("pocion"));
            Assert.Equal(70f, loadedPlayer.GetComponent<Stats>()!.Get("Vida"), 3);
            Assert.NotNull(loadedPlayer.GetComponent<AiAgent>());
            Assert.Contains("player", loadedPlayer.Tags);
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }
}
