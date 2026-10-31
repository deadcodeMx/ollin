using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class GraphStepModeTests
{
    [Fact]
    public void ElModoPasoPausaSinBreakpoint()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 3f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(1f, 3f, 0.3f) });

        var graphEntity = scene.Create("Grafo", Vector2.Zero);
        var graph = graphEntity.AddComponent(new NodeGraph());
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 100, 0);
        action.Action = ActionKind.OpenDoor;
        action.TargetName = "Puerta";
        start.Next = action.Id;

        GraphDebugger.StepMode = true;
        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.Equal(start.Id, graph.DebugCursor);
            Assert.False(door.Open);

            runtime.TogglePause();
            Assert.True(runtime.StepGraph(graph));
            Assert.Equal(action.Id, graph.DebugCursor);

            Assert.True(runtime.StepGraph(graph));
            Assert.True(door.Open);
            Assert.Null(graph.DebugCursor);
        }
        finally
        {
            GraphDebugger.StepMode = false;
            runtime.Stop();
        }
    }

    [Fact]
    public void ElGrupoExplícitoSobreviveAlClonado()
    {
        var graph = new NodeGraph();
        var group = graph.AddNode(GraphNodeKind.Group, 0, 0);
        var child = graph.AddNode(GraphNodeKind.Action, 10, 10);
        child.ParentGroupId = group.Id;
        group.Collapsed = true;

        var snapshot = GraphEditing.Capture(graph, group.Id);
        var clone = snapshot.Nodes.First(node => node.Id != group.Id);

        Assert.Equal(group.Id, clone.ParentGroupId);
    }
    [Fact]
    public void ElHistorialTambienSeLimitaPorBytes()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-graph-bytes", Guid.NewGuid().ToString("N"));

        try
        {
            var store = new GraphHistoryStore(root, maxFiles: 100, maxBytes: 1);
            store.Save("escena.ollin.json", Guid.NewGuid(), new NodeGraphHistory());

            Assert.Empty(Directory.GetFiles(root, "*.json"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void EjecutarHastaElNodoSeDetieneEnElObjetivo()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 3f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(1f, 3f, 0.3f) });

        var graphEntity = scene.Create("Grafo", Vector2.Zero);
        var graph = graphEntity.AddComponent(new NodeGraph());
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 100, 0);
        action.Action = ActionKind.OpenDoor;
        action.TargetName = "Puerta";
        start.Next = action.Id;
        GraphDebugger.RunToNodeId = action.Id;

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.False(door.Open);
            Assert.Equal(action.Id, graph.DebugCursor);
            Assert.Null(GraphDebugger.RunToNodeId);
        }
        finally
        {
            GraphDebugger.RunToNodeId = null;
            runtime.Stop();
        }
    }
}