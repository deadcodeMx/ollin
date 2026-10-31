using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class NodeGraphTests
{
    [Fact]
    public void EventoInicioEjecutaLaCadenaDeAcciones()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 5f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(2f, 3f, 0.3f) });

        var graphEntity = scene.Create("Grafo", new Vector2(0f, 2f));
        var graph = graphEntity.AddComponent(new NodeGraph());
        var start = graph.AddNode(GraphNodeKind.Event, 20f, 20f);
        start.Event = GraphEventKind.OnStart;
        var action = graph.AddNode(GraphNodeKind.Action, 260f, 20f);
        action.Action = ActionKind.OpenDoor;
        action.TargetName = "Puerta";
        start.Next = action.Id;

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(door.Open);
            Assert.Equal(action.Id, graph.LastNodeId);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void LaCondicionEligeLaRamaCorrecta()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");
        player.AddComponent(new Inventory { Capacity = 4 });

        var doorA = scene.Create("Puerta A", new Vector2(0f, 5f));
        var doorAComponent = doorA.AddComponent(new Door());
        doorA.AddComponent(new Wall3D { Size = new Vector3(1f, 3f, 0.3f) });

        var doorB = scene.Create("Puerta B", new Vector2(0f, 7f));
        var doorBComponent = doorB.AddComponent(new Door());
        doorB.AddComponent(new Wall3D { Size = new Vector3(1f, 3f, 0.3f) });

        var graphEntity = scene.Create("Grafo", Vector2.Zero);
        var graph = graphEntity.AddComponent(new NodeGraph());

        var start = graph.AddNode(GraphNodeKind.Event, 20f, 20f);
        start.Event = GraphEventKind.OnStart;

        var condition = graph.AddNode(GraphNodeKind.Condition, 240f, 20f);
        condition.Condition = GraphConditionKind.HasItem;
        condition.ItemId = "llave";

        var yes = graph.AddNode(GraphNodeKind.Action, 470f, 20f);
        yes.Action = ActionKind.OpenDoor;
        yes.TargetName = "Puerta A";

        var no = graph.AddNode(GraphNodeKind.Action, 470f, 170f);
        no.Action = ActionKind.OpenDoor;
        no.TargetName = "Puerta B";

        start.Next = condition.Id;
        condition.TrueNext = yes.Id;
        condition.FalseNext = no.Id;

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.False(doorAComponent.Open);
            Assert.True(doorBComponent.Open);
        }
        finally
        {
            runtime.Stop();
        }
    }
}
