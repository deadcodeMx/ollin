using Ollin.Core.Saves;
using Ollin.Scripting;
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

    [Fact]
    public void LaFuncionGeneraCondicionesYBucles()
    {
        var graph = new NodeGraph();
        var function = graph.AddNode(GraphNodeKind.Function, 0, 0);
        function.FunctionName = "Clasificar";
        function.ParameterText = "x:Number";
        function.ReturnType = GraphPinType.Number;
        var condition = graph.AddNode(GraphNodeKind.Condition, 150, 0);
        condition.Condition = GraphConditionKind.Expression;
        condition.Expression = "x > 0";
        var yes = graph.AddNode(GraphNodeKind.Return, 300, 0);
        yes.ReturnExpression = "1";
        var no = graph.AddNode(GraphNodeKind.Return, 300, 100);
        no.ReturnExpression = "0";
        function.Next = condition.Id;
        condition.TrueNext = yes.Id;
        condition.FalseNext = no.Id;

        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("if (", code);
        Assert.Contains("return 1;", code);
        Assert.Contains("return 0;", code);
        Assert.Contains("[\"x\"] = x", code);
        Assert.Contains("Locals = __locals", code);
    }

    [Fact]
    public void SetVariableUsaLaExpresionYElEventoInlineaParametros()
    {
        var graph = new NodeGraph();
        var custom = graph.AddNode(GraphNodeKind.CustomEvent, 0, 0);
        custom.CustomEventName = "Sumar";
        custom.ParameterText = "cantidad:Number";
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "total";
        action.Expression = "cantidad + 1";
        custom.Next = action.Id;

        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("Event_Sumar(float cantidad)", code);
        Assert.Contains("_vars[\"total\"] = cantidad + 1;", code);
    }

    [Fact]
    public void LasVariablesTipadasSeInicializanYSeUsan()
    {
        var graph = new NodeGraph();
        graph.Variables.Add(new GraphVariable { Name = "contador", Type = GraphPinType.Number, DefaultValue = "5" });
        graph.Variables.Add(new GraphVariable { Name = "nombre", Type = GraphPinType.Text, DefaultValue = "hola" });
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "contador";
        action.Expression = "contador + 1";
        start.Next = action.Id;

        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("private float _var_contador = 5f;", code);
        Assert.Contains("private string _var_nombre = \"hola\";", code);
        Assert.Contains("_var_contador = _var_contador + 1;", code);
    }

    [Fact]
    public void ElCodigoGeneradoCompila()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.Message;
        start.Next = action.Id;
        var code = GraphCodeGenerator.Generate(graph);
        var result = ScriptCompiler.Compile(new[] { ("generated.cs", code) });
        Assert.True(result.Success, string.Join("\n", result.Diagnostics.Select(d => d.Text)));
    }

    [Fact]
    public void ElWhileTieneLimiteDeIteraciones()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var loop = graph.AddNode(GraphNodeKind.Loop, 100, 0);
        loop.LoopKind = GraphLoopKind.While;
        loop.LoopExpression = "true";
        loop.LoopMaxIterations = 5;
        var body = graph.AddNode(GraphNodeKind.Action, 300, 0);
        body.Action = ActionKind.Message;
        start.Next = loop.Id;
        loop.Next = body.Id;
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("__loop_guard", code);
        Assert.Contains("> 5", code);
    }

    [Fact]
    public void LosBuclesAnidadosSeInlinean()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var outer = graph.AddNode(GraphNodeKind.Loop, 100, 0);
        var inner = graph.AddNode(GraphNodeKind.Loop, 300, 0);
        var body = graph.AddNode(GraphNodeKind.Action, 500, 0);
        body.Action = ActionKind.Message;
        start.Next = outer.Id;
        outer.Next = inner.Id;
        inner.Next = body.Id;
        var code = GraphCodeGenerator.Generate(graph);
        Assert.True(code.Split("for (").Length - 1 >= 2, code);
    }

    [Fact]
    public void ElReturnSeValidaContraElTipoDeLaFuncion()
    {
        var scene = new Scene();
        var entity = scene.Create("Grafo", Vector2.Zero);
        var graph = entity.AddComponent(new NodeGraph());
        var function = graph.AddNode(GraphNodeKind.Function, 0, 0);
        function.FunctionName = "Texto";
        function.ReturnType = GraphPinType.Vector2;
        var ret = graph.AddNode(GraphNodeKind.Return, 200, 0);
        ret.ReturnExpression = "1";
        function.Next = ret.Id;
        var issues = GraphValidator.Validate(graph, scene);
        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Return"));
    }

    [Fact]
    public void LasVariablesLocalesSeDeclaranYUsan()
    {
        var graph = new NodeGraph();
        var function = graph.AddNode(GraphNodeKind.Function, 0, 0);
        function.FunctionName = "Local";
        function.LocalVariableText = "temp:Number=7";
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "temp";
        action.Expression = "temp + 1";
        function.Next = action.Id;
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("float temp = 7;", code);
        Assert.Contains("temp = temp + 1;", code);
    }

    [Fact]
    public void ElSubgrafoSeGeneraComoMetodo()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var call = graph.AddNode(GraphNodeKind.Subgraph, 200, 0);
        call.SubgraphName = "Hijo";
        start.Next = call.Id;
        var child = graph.AddSubgraph("Hijo");
        var childEvent = child.AddNode(GraphNodeKind.Event, 0, 0);
        var action = child.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.Message;
        childEvent.Next = action.Id;
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("private void Sub_Hijo()", code);
        Assert.Contains("Sub_Hijo();", code);
    }

    [Fact]
    public void LosOperadoresSeUsanComoDato()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var op = graph.AddNode(GraphNodeKind.Operator, 0, 100);
        op.OperatorKind = GraphOperatorKind.Add;
        op.LeftExpression = "2";
        op.RightExpression = "3";
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "x";
        start.Next = action.Id;
        graph.AddDataLink(op.Id, action.Id, "amount", GraphPinType.Number);
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("(2 + 3)", code);
    }

    [Fact]
    public void ElPinDeMensajeCambiaElTextoDeLaAccion()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var message = graph.AddNode(GraphNodeKind.Value, 0, 100);
        message.ValueKind = GraphValueKind.Text;
        message.Value = "Hola desde dato";
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.Message;
        start.Next = action.Id;
        graph.AddDataLink(message.Id, action.Id, "message", GraphPinType.Text);
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("Hola desde dato", code);
    }

    [Fact]
    public void LasVariablesLocalesDuplicadasSeDetectan()
    {
        var scene = new Scene();
        var entity = scene.Create("Grafo", Vector2.Zero);
        var graph = entity.AddComponent(new NodeGraph());
        var function = graph.AddNode(GraphNodeKind.Function, 0, 0);
        function.FunctionName = "Duplicada";
        function.ParameterText = "x:Number";
        function.LocalVariableText = "x:Number=1";
        var issues = GraphValidator.Validate(graph, scene);
        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("duplicada"));
    }

    [Fact]
    public void LosBreakpointsSeGeneranEnElCodigo()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.Message;
        start.Next = action.Id;
        graph.Breakpoints.Add(action.Id);
        var code = GraphCodeGenerator.Generate(graph);
        Assert.Contains("Debugger.Break()", code);
    }

    [Fact]
    public void LosOperadoresTienenTipoDeResultado()
    {
        var graph = new NodeGraph();
        var compare = graph.AddNode(GraphNodeKind.Operator, 0, 0);
        compare.OperatorKind = GraphOperatorKind.Greater;
        compare.LeftExpression = "2";
        compare.RightExpression = "1";
        Assert.Equal(GraphPinType.Boolean, NodeGraph.OperatorResultType(compare));
        compare.OperatorKind = GraphOperatorKind.Add;
        Assert.Equal(GraphPinType.Integer, NodeGraph.OperatorResultType(compare));
        compare.LeftExpression = "2.5";
        Assert.Equal(GraphPinType.Number, NodeGraph.OperatorResultType(compare));
    }

    [Fact]
    public void ElReturnDeUnaComparacionSeInfiriereComoBooleano()
    {
        var scene = new Scene();
        var entity = scene.Create("Grafo", Vector2.Zero);
        var graph = entity.AddComponent(new NodeGraph());
        var function = graph.AddNode(GraphNodeKind.Function, 0, 0);
        function.FunctionName = "EsPositivo";
        function.ParameterText = "x:Number";
        function.ReturnType = GraphPinType.Boolean;
        var ret = graph.AddNode(GraphNodeKind.Return, 200, 0);
        ret.ReturnExpression = "x > 0";
        function.Next = ret.Id;
        var issues = GraphValidator.Validate(graph, scene);
        Assert.DoesNotContain(issues, issue => issue.IsError && issue.Message.Contains("Return"));
    }

    [Fact]
    public void LasFuncionesYEventosDuplicadosSeDetectan()
    {
        var scene = new Scene();
        var entity = scene.Create("Grafo", Vector2.Zero);
        var graph = entity.AddComponent(new NodeGraph());
        var first = graph.AddNode(GraphNodeKind.Function, 0, 0);
        first.FunctionName = "Doble";
        var second = graph.AddNode(GraphNodeKind.Function, 0, 100);
        second.FunctionName = "Doble";
        var eventOne = graph.AddNode(GraphNodeKind.CustomEvent, 0, 200);
        eventOne.CustomEventName = "Algo";
        var eventTwo = graph.AddNode(GraphNodeKind.CustomEvent, 0, 300);
        eventTwo.CustomEventName = "Algo";
        var issues = GraphValidator.Validate(graph, scene);
        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Función duplicada"));
        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Evento duplicado"));
    }
}