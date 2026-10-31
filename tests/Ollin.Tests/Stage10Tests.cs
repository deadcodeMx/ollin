using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Projects;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class TechnicalDemoTests
{
    [Fact]
    public void LaDemoTecnicaTieneJugadorEnemigosLlavePuertaYGrafo()
    {
        var scene = StarterProject.CreateScene(ProjectTemplate.TechnicalDemo);

        var player = scene.Find("Jugador")!;
        Assert.Contains("player", player.Tags);
        Assert.NotNull(player.GetComponent<Health>());
        Assert.NotNull(player.GetComponent<WeaponGroup>());
        Assert.NotNull(player.GetComponent<WinLoseRules>());
        Assert.NotNull(player.GetComponent<VariableHud>());

        var enemies = scene.Entities.Where(entity => entity.Tags.Contains("enemy")).ToList();
        Assert.Equal(3, enemies.Count);
        Assert.All(enemies, enemy => Assert.NotNull(enemy.GetComponent<AiAgent3D>()));

        Assert.NotNull(scene.Find("Llave")!.GetComponent<Pickup>());
        Assert.NotNull(scene.Find("Puerta")!.GetComponent<Door>());
        Assert.NotNull(scene.Find("Guía")!.GetComponent<NodeGraph>());
        Assert.NotNull(scene.Find("Lava")!.GetComponent<Hazard3D>());
        Assert.NotNull(scene.Find("Portal")!.GetComponent<Teleporter3D>());
    }

    [Fact]
    public void AlMorirUnEnemigoSeIncrementaElContador()
    {
        GameStateHost.Current = new GameState();
        var scene = new Scene();
        var enemy = scene.Create("Enemigo", Vector2.Zero);
        var health = enemy.AddComponent(new Health
        {
            Max = 10f,
            Current = 10f,
            DeathVariable = "enemigos_muertos",
        });

        health.TakeDamage(20f);

        Assert.Equal(1f, GameStateHost.Current.GetNumber("enemigos_muertos"), 3);
    }
    [Fact]
    public void ElCilindroExpulsaAlJugador()
    {
        var scene = new Scene();
        var pillar = scene.Create("Pilar", Vector2.Zero);
        pillar.AddComponent(new CylinderCollider3D { Radius = 1f, Height = 3f });

        var resolved = CylinderCollision.Resolve(scene, new Vector2(0.2f, 0f), 0.35f, 1.2f);

        Assert.True(Vector2.Distance(resolved, Vector2.Zero) >= 1.34f, $"dist={Vector2.Distance(resolved, Vector2.Zero)}");
    }
}