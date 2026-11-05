using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scripting;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class GraphV2Tests
{
    [Fact]
    public void LosEnlacesDeDatosSeCreanValidanYSeLimpian()
    {
        var scene = new Scene();
        var entity = scene.Create("Grafo", Vector2.Zero);
        var graph = entity.AddComponent(new NodeGraph());
        var source = graph.AddNode(GraphNodeKind.Action, 0, 0);
        var target = graph.AddNode(GraphNodeKind.Action, 200, 0);

        var link = graph.AddDataLink(source.Id, target.Id, "value", GraphPinType.Number);
        Assert.NotNull(link);
        Assert.Single(graph.DataLinks);
        Assert.Equal(target.Id, graph.DataLinkTo(target.Id, "value")!.TargetNodeId);
        Assert.Null(graph.AddDataLink(source.Id, target.Id, "value", GraphPinType.Text));
        Assert.True(NodeGraph.CanConnect(GraphPinType.Integer, GraphPinType.Number));
        Assert.False(NodeGraph.CanConnect(GraphPinType.Text, GraphPinType.Number));

        graph.RemoveNode(source.Id);
        Assert.Empty(graph.DataLinks);
    }

    [Fact]
    public void ElCodigoGeneradoUsaElValorConectado()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var value = graph.AddNode(GraphNodeKind.Value, 0, 0);
        value.ValueKind = GraphValueKind.Number;
        value.Value = "42";
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "puntos";
        start.Next = action.Id;
        graph.AddDataLink(value.Id, action.Id, "amount", GraphPinType.Number);

        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("42", code);
        Assert.DoesNotContain("Amount", code);
    }
}


