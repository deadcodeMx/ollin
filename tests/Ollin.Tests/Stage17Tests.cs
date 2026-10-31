using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class GraphDisabledLinkTests
{
    [Fact]
    public void UnPuertoDeshabilitadoNoEjecutaLaAccion()
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
        start.DisabledPorts.Add("next");

        Assert.Null(graph.Link(start, "next"));

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

    [Fact]
    public void ElHistorialPersisteLosBreakpoints()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-graph-breakpoints", Guid.NewGuid().ToString("N"));

        try
        {
            var graph = new NodeGraph();
            var node = graph.AddNode(GraphNodeKind.Action, 0, 0);
            var history = new NodeGraphHistory();
            var store = new GraphHistoryStore(root);
            var entityId = Guid.NewGuid();

            store.Save("escena.ollin.json", entityId, history, new[] { node.Id });

            var loaded = new NodeGraphHistory();
            var breakpoints = new HashSet<Guid>();
            Assert.True(store.Load("escena.ollin.json", entityId, loaded, breakpoints));
            Assert.Contains(node.Id, breakpoints);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
    [Fact]
    public void ElPasoEjecutaUnNodoDesdeElBreakpoint()
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
        graph.Breakpoints.Add(action.Id);

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.False(door.Open);
            Assert.Equal(action.Id, graph.DebugCursor);

            runtime.TogglePause();
            Assert.True(runtime.StepGraph(graph));
            Assert.True(door.Open);
            Assert.Null(graph.DebugCursor);
        }
        finally
        {
            runtime.Stop();
        }
    }
    [Fact]
    public void ElHistorialSeLimitaPorCantidadDeArchivos()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-graph-limit", Guid.NewGuid().ToString("N"));

        try
        {
            var store = new GraphHistoryStore(root, maxFiles: 2);

            for (var i = 0; i < 4; i++)
            {
                store.Save("escena.ollin.json", Guid.NewGuid(), new NodeGraphHistory());
            }

            Assert.True(Directory.GetFiles(root, "*.json").Length <= 2);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}