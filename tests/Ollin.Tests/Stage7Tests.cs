using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class VisualScriptTests
{
    [Fact]
    public void UnaReglaAlIniciarAbreLaPuerta()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 5f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(2f, 3f, 0.3f) });

        var script = scene.Create("Script", new Vector2(0f, 2f));
        var visual = script.AddComponent(new VisualScript());
        visual.GetRule(0).Event = ScriptEvent.OnStart;
        visual.GetRule(0).Action = ActionKind.OpenDoor;
        visual.GetRule(0).TargetName = "Puerta";

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
    public void UnaReglaConCondicionDeObjetoNoSeEjecutaSinEl()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        player.AddComponent(new Inventory { Capacity = 4 });

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 5f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(2f, 3f, 0.3f) });

        var script = scene.Create("Script", new Vector2(0f, 2f));
        var visual = script.AddComponent(new VisualScript());
        visual.GetRule(0).Event = ScriptEvent.OnStart;
        visual.GetRule(0).Action = ActionKind.OpenDoor;
        visual.GetRule(0).TargetName = "Puerta";
        visual.GetRule(0).Condition = ScriptCondition.HasItem;
        visual.GetRule(0).ConditionItem = "llave";

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.False(door.Open);
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class WorldComponentTests
{
    [Fact]
    public void ElPeligroDanaAlJugador()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        var health = player.AddComponent(new Health { Max = 100f, Current = 100f });

        var hazard = scene.Create("Lava", Vector2.Zero);
        hazard.AddComponent(new Hazard3D { Radius = 2f, DamagePerSecond = 40f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 10; i++) runtime.Tick(0.1f);
            Assert.True(health.Current < 100f, $"vida={health.Current}");
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void ElTeletransporteMueveAlJugador()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        player.AddComponent(new Health { Max = 100f, Current = 100f });

        var destination = scene.Create("Destino", new Vector2(5f, 6f));
        destination.AddComponent(new Wall3D { Size = new Vector3(1f, 1f, 1f), Collides = false });

        var teleporter = scene.Create("Portal", Vector2.Zero);
        teleporter.AddComponent(new Teleporter3D { Radius = 2f, TargetName = "Destino" });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.Equal(new Vector2(5f, 6f), player.Transform.Position);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void ElSeguidorSeAcercaAlJugador()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", new Vector2(0f, 5f));
        player.Tags.Add("player");

        var follower = scene.Create("Compañero", Vector2.Zero);
        follower.AddComponent(new Follower3D { Speed = 2f, FollowDistance = 1f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 20; i++) runtime.Tick(0.05f);
            Assert.True(follower.Transform.Position.Y > 0.5f);
        }
        finally
        {
            runtime.Stop();
        }
    }
}
