using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class GraphDebugFrameTests
{
    [Fact]
    public void LaFotoDeVariablesSeTomaDespuesDeLaAccion()
    {
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var graphEntity = scene.Create("Grafo", Vector2.Zero);
        var graph = graphEntity.AddComponent(new NodeGraph());
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 100, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "contador";
        action.Expression = "7";
        start.Next = action.Id;

        GameStateHost.Current = new GameState();
        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            var frame = graph.DebugFrames.Last();

            Assert.Equal(action.Id, frame.NodeId);
            Assert.Equal("7", frame.VariablesAfter!["contador"]);
        }
        finally
        {
            runtime.Stop();
            GameStateHost.Current = null;
        }
    }
}
