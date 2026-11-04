using Ollin.Core.Model.Components;
using Ollin.Core.Scripting;
using Xunit;

namespace Ollin.Tests;

public sealed class GraphCodeGeneratorTests
{
    [Fact]
    public void GeneratesScript()
    {
        var graph = new NodeGraph();
        var start = graph.AddNode(GraphNodeKind.Event, 0, 0);
        start.Event = GraphEventKind.OnStart;
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.Message;
        action.Message = "Hola desde el grafo";
        start.Next = action.Id;

        var code = GraphCodeGenerator.Generate(graph, "MiGrafo");

        if (!code.Contains("class MiGrafo : OllinScript")) throw new Exception("Falta la clase.");
        if (!code.Contains("OnStart()")) throw new Exception("Falta OnStart.");
        if (!code.Contains("Hola desde el grafo")) throw new Exception("Falta el mensaje.");
    }
}
