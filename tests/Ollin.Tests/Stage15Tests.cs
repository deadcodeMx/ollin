using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Core.Scripting;
using Xunit;

namespace Ollin.Tests;

public class ExpressionSyntaxTests
{
    [Fact]
    public void ValidaExpresionesCorrectas()
    {
        Assert.Null(ExpressionEvaluator.Validate("2 + 3 * 4"));
        Assert.Null(ExpressionEvaluator.Validate("(contador + 1) >= 3 && tiene(\"llave\")"));
    }

    [Fact]
    public void DetectaExpresionesIncompletas()
    {
        Assert.NotNull(ExpressionEvaluator.Validate("2 +"));
        Assert.NotNull(ExpressionEvaluator.Validate("(1 + 2"));
        Assert.NotNull(ExpressionEvaluator.Validate(""));
        Assert.NotNull(ExpressionEvaluator.Validate("1 + @"));
    }

    [Fact]
    public void ElValidadorDelGrafoMarcaUnaExpresionInvalida()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var condition = graph.AddNode(GraphNodeKind.Condition, 200, 0);
        condition.Condition = GraphConditionKind.Expression;
        condition.Expression = "1 +";
        eventNode.Next = condition.Id;

        var issues = GraphValidator.Validate(graph, scene);

        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Expresión inválida"));
    }

    [Fact]
    public void ElValidadorMarcaUnaVariableConExpresionInvalida()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.SetVariable;
        action.VariableName = "contador";
        action.Expression = "(2 +";
        eventNode.Next = action.Id;

        var issues = GraphValidator.Validate(graph, scene);

        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Expresión inválida"));
    }

    [Fact]
    public void ElHistorialSigueFuncionandoConLaValidacion()
    {
        var graph = new NodeGraph();
        var node = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var history = new NodeGraphHistory();
        var initial = GraphEditing.Capture(graph, node.Id);

        graph.AddNode(GraphNodeKind.Action, 100, 0);
        history.Record(initial);

        Assert.True(history.Undo(graph, node.Id, out _));
        Assert.Single(graph.Nodes);
    }
    [Fact]
    public void ElAnalizadorSeparaVariablesYFunciones()
    {
        var analysis = ExpressionEvaluator.Analyze("contador >= 3 && tiene(\"llave\")");

        Assert.Contains("contador", analysis.Identifiers);
        Assert.Contains("tiene", analysis.Functions);
    }

    [Fact]
    public void ElValidadorAvisaDeVariableNoDefinida()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var condition = graph.AddNode(GraphNodeKind.Condition, 100, 0);
        condition.Condition = GraphConditionKind.Expression;
        condition.Expression = "contador >= 3";
        eventNode.Next = condition.Id;

        var issues = GraphValidator.Validate(graph, scene);

        Assert.Contains(issues, issue => !issue.IsError && issue.Message.Contains("Variable no definida"));
    }

    [Fact]
    public void ElValidadorAceptaUnaVariableDefinidaPorUnaAccion()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var set = graph.AddNode(GraphNodeKind.Action, 100, 0);
        set.Action = ActionKind.SetVariable;
        set.VariableName = "contador";
        set.Expression = "0";
        var condition = graph.AddNode(GraphNodeKind.Condition, 200, 0);
        condition.Condition = GraphConditionKind.Expression;
        condition.Expression = "contador >= 1";
        eventNode.Next = set.Id;
        set.Next = condition.Id;

        var issues = GraphValidator.Validate(graph, scene);

        Assert.DoesNotContain(issues, issue => issue.Message.Contains("Variable no definida"));
    }

    [Fact]
    public void ElValidadorRechazaFuncionesDesconocidas()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var condition = graph.AddNode(GraphNodeKind.Condition, 100, 0);
        condition.Condition = GraphConditionKind.Expression;
        condition.Expression = "foo(1) > 0";
        eventNode.Next = condition.Id;

        var issues = GraphValidator.Validate(graph, scene);

        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Función desconocida"));
    }
}