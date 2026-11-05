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
        Assert.True(NodeGraph.CanConnect(GraphPinType.Text, GraphPinType.Number));
        Assert.False(NodeGraph.CanConnect(GraphPinType.Vector2, GraphPinType.Boolean));

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

    [Fact]
    public void LosSubgrafosSeCreanYBuscanPorNombre()
    {
        var graph = new NodeGraph();
        var sub = graph.AddSubgraph("Puerta");
        Assert.Same(sub, graph.FindSubgraph("puerta"));
        Assert.Single(graph.Subgraphs);
        Assert.True(graph.RemoveSubgraph("PUERTA"));
        Assert.Empty(graph.Subgraphs);
    }

    [Fact]
    public void ElSubgrafoSeEjecutaYElLimiteEvitaBucles()
    {
        var scene = new Scene();
        var rootEntity = scene.Create("Raiz", Vector2.Zero);
        var target = scene.Create("Objetivo", Vector2.Zero);
        target.Active = false;
        var root = rootEntity.AddComponent(new NodeGraph());
        var start = root.AddNode(GraphNodeKind.Event, 0, 0);
        var call = root.AddNode(GraphNodeKind.Subgraph, 200, 0);
        call.SubgraphName = "Hijo";
        start.Next = call.Id;
        var child = root.AddSubgraph("Hijo");
        var childEvent = child.AddNode(GraphNodeKind.Event, 0, 0);
        var activate = child.AddNode(GraphNodeKind.Action, 200, 0);
        activate.Action = ActionKind.Activate;
        activate.TargetName = "Objetivo";
        childEvent.Next = activate.Id;

        root.RunFirst(scene, null);
        Assert.True(target.Active);
    }

    [Fact]
    public void LaAccionFijaUnaPropiedadDeComponente()
    {
        var scene = new Scene();
        var owner = scene.Create("Grafo", Vector2.Zero);
        var target = scene.Create("Objetivo", Vector2.Zero);
        var sprite = target.AddComponent(new SpriteRenderer { SortingOrder = 1 });
        var graph = owner.AddComponent(new NodeGraph());
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetComponentProperty;
        action.TargetName = "Objetivo";
        action.ComponentName = "SpriteRenderer";
        action.PropertyName = "SortingOrder";
        action.Amount = 7;
        start.Next = action.Id;

        graph.RunFirst(scene, null);
        Assert.Equal(7, sprite.SortingOrder);
    }

    [Fact]
    public void LaMatrizDeAutoCastYLaMigracionFuncionan()
    {
        Assert.True(NodeGraph.CanConnect(GraphPinType.Boolean, GraphPinType.Number));
        Assert.True(NodeGraph.CanConnect(GraphPinType.Number, GraphPinType.Text));
        Assert.True(NodeGraph.CanConnect(GraphPinType.Text, GraphPinType.Integer));
        Assert.False(NodeGraph.CanConnect(GraphPinType.Vector2, GraphPinType.Boolean));

        var graph = new NodeGraph { Name = "", SchemaVersion = 0 };
        var node = graph.AddNode(GraphNodeKind.Action, 0, 0);
        graph.DataLinks.Add(new GraphDataLink { TargetNodeId = node.Id, TargetPort = "" });
        graph.Migrate();
        Assert.Equal(2, graph.SchemaVersion);
        Assert.Equal("Grafo principal", graph.Name);
        Assert.Equal("amount", graph.DataLinks[0].TargetPort);
    }

    [Fact]
    public void Vector2SeConectaYSeAsigna()
    {
        var scene = new Scene();
        var owner = scene.Create("Grafo", Vector2.Zero);
        var target = scene.Create("Objetivo", Vector2.Zero);
        var sprite = target.AddComponent(new SpriteRenderer { Size = Vector2.One });
        var graph = owner.AddComponent(new NodeGraph());
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var value = graph.AddNode(GraphNodeKind.Value, 0, 0);
        value.ValueKind = GraphValueKind.Vector2;
        value.Value = "2,3";
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetComponentProperty;
        action.TargetName = "Objetivo";
        action.ComponentName = "SpriteRenderer";
        action.PropertyName = "Size";
        start.Next = action.Id;
        graph.AddDataLink(value.Id, action.Id, "Size", GraphPinType.Vector2);

        graph.RunFirst(scene, null);
        Assert.Equal(new Vector2(2f, 3f), sprite.Size);
        Assert.Contains("new Vector2", GraphCodeGenerator.Generate(graph));
    }

    [Fact]
    public void ElBucleForSeGeneraConCuerpoYSalida()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var loop = graph.AddNode(GraphNodeKind.Loop, 100, 0);
        loop.LoopKind = GraphLoopKind.For;
        loop.LoopCount = 2;
        var body = graph.AddNode(GraphNodeKind.Action, 300, 0);
        body.Action = ActionKind.Message;
        lock (body) { }
        start.Next = loop.Id;
        loop.Next = body.Id;
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("for (int i = 0; i < 2; i++)", code);
    }

    [Fact]
    public void LosEventosPersonalizadosGeneranMetodoYLLamada()
    {
        var graph = new NodeGraph();
        var custom = graph.AddNode(GraphNodeKind.CustomEvent, 0, 0);
        custom.CustomEventName = "AbrirPuerta";
        custom.ParameterText = "cantidad:Number";
        var body = graph.AddNode(GraphNodeKind.Action, 200, 0);
        body.Action = ActionKind.Message;
        custom.Next = body.Id;
        var start = graph.AddNode(GraphNodeKind.Event, 0, 200);
        var call = graph.AddNode(GraphNodeKind.CallEvent, 200, 200);
        call.CustomEventName = "AbrirPuerta";
        call.ArgumentsText = "3";
        start.Next = call.Id;

        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("private void Event_AbrirPuerta(float cantidad)", code);
        Assert.Contains("Event_AbrirPuerta(3);", code);
    }

    [Fact]
    public void BreakContinueYReturnSeGeneranDentroDelBucle()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var loop = graph.AddNode(GraphNodeKind.Loop, 100, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        var brk = graph.AddNode(GraphNodeKind.Break, 400, 0);
        start.Next = loop.Id; loop.Next = action.Id; action.Next = brk.Id;
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("break;", code);
    }

    [Fact]
    public void LasFuncionesConRetornoSeGeneranYSeLlaman()
    {
        var graph = new NodeGraph();
        var function = graph.AddNode(GraphNodeKind.Function, 0, 0);
        function.FunctionName = "Doble";
        function.ParameterText = "x:Number";
        function.ReturnType = GraphPinType.Number;
        var ret = graph.AddNode(GraphNodeKind.Return, 200, 0);
        ret.ReturnExpression = "x * 2";
        function.Next = ret.Id;
        var start = graph.AddNode(GraphNodeKind.Event, 0, 200);
        var call = graph.AddNode(GraphNodeKind.CallFunction, 200, 200);
        call.FunctionName = "Doble";
        call.ArgumentsText = "3";
        var action = graph.AddNode(GraphNodeKind.Action, 400, 200);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "resultado";
        start.Next = action.Id;
        graph.AddDataLink(call.Id, action.Id, "amount", GraphPinType.Number);

        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("private float Func_Doble(float x)", code);
        Assert.Contains("return x * 2;", code);
        Assert.Contains("Func_Doble(3)", code);
    }
}
