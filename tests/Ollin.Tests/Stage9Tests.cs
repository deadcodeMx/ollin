using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Core.Scripting;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class ExpressionTests
{
    [Fact]
    public void RespetaLaPrecedenciaYLosParentesis()
    {
        var context = new ExpressionContext { State = new GameState() };
        Assert.True(ExpressionEvaluator.EvaluateBool("2 + 3 * 4 == 14", context));
        Assert.True(ExpressionEvaluator.EvaluateBool("(2 + 3) * 4 == 20", context));
    }

    [Fact]
    public void LeeVariablesYFlags()
    {
        var state = new GameState();
        state.SetNumber("contador", 3);
        state.SetFlag("abierto", true);
        var context = new ExpressionContext { State = state };

        Assert.True(ExpressionEvaluator.EvaluateBool("contador >= 3 && abierto", context));
        Assert.Equal(4f, ExpressionEvaluator.EvaluateNumber("contador + 1", context), 3);
    }
}

public class NodeGraphVariableTests
{
    [Fact]
    public void LosContadoresSeSumanYSeComparan()
    {
        GameStateHost.Current = new GameState();
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var doorEntity = scene.Create("Puerta", new Vector2(0f, 5f));
        var door = doorEntity.AddComponent(new Door());
        doorEntity.AddComponent(new Wall3D { Size = new Vector3(1f, 3f, 0.3f) });

        var graphEntity = scene.Create("Grafo", Vector2.Zero);
        var graph = graphEntity.AddComponent(new NodeGraph());

        var start = graph.AddNode(GraphNodeKind.Event, 0f, 0f);
        start.Event = GraphEventKind.OnStart;

        var set = graph.AddNode(GraphNodeKind.Action, 0f, 100f);
        set.Action = ActionKind.SetVariable;
        set.VariableName = "contador";
        set.Expression = "2";

        var add = graph.AddNode(GraphNodeKind.Action, 0f, 200f);
        add.Action = ActionKind.AddVariable;
        add.VariableName = "contador";
        add.Expression = "1";

        var condition = graph.AddNode(GraphNodeKind.Condition, 0f, 300f);
        condition.Condition = GraphConditionKind.Expression;
        condition.Expression = "contador == 3";

        var open = graph.AddNode(GraphNodeKind.Action, 0f, 400f);
        open.Action = ActionKind.OpenDoor;
        open.TargetName = "Puerta";

        start.Next = set.Id;
        set.Next = add.Id;
        add.Next = condition.Id;
        condition.TrueNext = open.Id;

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(door.Open);
            Assert.Equal(3f, GameStateHost.Current.GetNumber("contador"), 3);
        }
        finally
        {
            runtime.Stop();
        }
    }

    [Fact]
    public void LosFlagsSeAlternan()
    {
        GameStateHost.Current = new GameState();
        var scene = new Scene();
        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var graphEntity = scene.Create("Grafo", Vector2.Zero);
        var graph = graphEntity.AddComponent(new NodeGraph());

        var start = graph.AddNode(GraphNodeKind.Event, 0f, 0f);
        start.Event = GraphEventKind.OnStart;

        var flag = graph.AddNode(GraphNodeKind.Action, 0f, 100f);
        flag.Action = ActionKind.ToggleFlag;
        flag.VariableName = "abierto";
        start.Next = flag.Id;

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            runtime.Tick(0.05f);
            Assert.True(GameStateHost.Current.GetFlag("abierto"));
        }
        finally
        {
            runtime.Stop();
        }
    }
}
