using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class GraphHistoryStoreTests
{
    [Fact]
    public void GuardaYCargaElHistorial()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-graph-history", Guid.NewGuid().ToString("N"));

        try
        {
            var graph = new NodeGraph();
            var node = graph.AddNode(GraphNodeKind.Event, 0, 0);
            var history = new NodeGraphHistory();
            var initial = GraphEditing.Capture(graph, node.Id);

            graph.AddNode(GraphNodeKind.Action, 100, 0);
            history.Record(initial);

            var store = new GraphHistoryStore(root);
            var entityId = Guid.NewGuid();
            store.Save("escena.ollin.json", entityId, history);

            var loaded = new NodeGraphHistory();
            Assert.True(store.Load("escena.ollin.json", entityId, loaded));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

public class GraphCommentAndBreakpointTests
{
    [Fact]
    public void UnComentarioNoSeValidaComoNodoSuelto()
    {
        var graph = new NodeGraph();
        graph.AddNode(GraphNodeKind.Comment, 0, 0);

        var issues = GraphValidator.Validate(graph, new Scene());

        Assert.DoesNotContain(issues, issue => issue.Node?.Kind == GraphNodeKind.Comment);
    }

    [Fact]
    public void UnBreakpointDetieneLaAccion()
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

        var hit = false;
        void OnHit(NodeGraph _, Guid __) => hit = true;
        GraphDebugger.BreakpointHit += OnHit;

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(hit);
            Assert.False(door.Open);
            Assert.Equal(action.Id, graph.LastNodeId);
        }
        finally
        {
            GraphDebugger.BreakpointHit -= OnHit;
            runtime.Stop();
        }
    }
}
