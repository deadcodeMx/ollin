using System.Text;
using Ollin.Core.Model.Components;

namespace Ollin.Core.Scripting;

/// <summary>Genera un script C# editable a partir de un NodeGraph.</summary>
public static class GraphCodeGenerator
{
    public static string Generate(NodeGraph graph, string className = "GraphScript")
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Numerics;");
        sb.AppendLine("using Ollin.Core.Model.Components;");
        sb.AppendLine("using Ollin.Scripting;");
        sb.AppendLine();
        sb.AppendLine($"public sealed class {className} : OllinScript");
        sb.AppendLine("{");
        sb.AppendLine("    // Generado por Ollin desde el grafo visual. Edita libremente.");

        var events = graph.Nodes.Where(n => n.Kind == GraphNodeKind.Event).ToList();
        foreach (var node in events)
        {
            switch (node.Event)
            {
                case GraphEventKind.OnStart:
                    sb.AppendLine("    public override void OnStart() => " + MethodName(node) + "();");
                    break;
                case GraphEventKind.OnTimer:
                    sb.AppendLine($"    private float _timer_{Short(node.Id)};");
                    sb.AppendLine("    public override void OnUpdate(float deltaTime)");
                    sb.AppendLine("    {");
                    sb.AppendLine($"        _timer_{Short(node.Id)} += deltaTime;");
                    sb.AppendLine($"        if (_timer_{Short(node.Id)} < {node.Interval:0.###}f) return;");
                    sb.AppendLine($"        _timer_{Short(node.Id)} = 0f;");
                    sb.AppendLine($"        {MethodName(node)}();");
                    sb.AppendLine("    }");
                    break;
                case GraphEventKind.OnInteract:
                    sb.AppendLine("    public override void OnInteract(Ollin.Core.Model.Entity other) => " + MethodName(node) + "();");
                    break;
                case GraphEventKind.OnPlayerEnter:
                    sb.AppendLine("    public override void OnTriggerEnter(Ollin.Core.Model.Entity other) => " + MethodName(node) + "();");
                    break;
                case GraphEventKind.OnPlayerDeath:
                    sb.AppendLine("    // TODO: OnPlayerDeath no tiene evento directo en OllinScript.");
                    break;
            }
        }

        var emitted = new HashSet<Guid>();
        foreach (var node in events)
        {
            EmitChain(sb, graph, node, emitted);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitChain(StringBuilder sb, NodeGraph graph, GraphNode? node, HashSet<Guid> emitted)
    {
        while (node is not null && emitted.Add(node.Id))
        {
            EmitNode(sb, graph, node);
            node = node.Next is { } next ? Find(graph, next) : null;
        }
    }

    private static void EmitNode(StringBuilder sb, NodeGraph graph, GraphNode node)
    {
        sb.AppendLine();
        sb.AppendLine($"    private void {MethodName(node)}()");
        sb.AppendLine("    {");
        if (node.Kind == GraphNodeKind.Event)
        {
            sb.AppendLine($"        // Evento: {node.Event}");
            if (node.Next is { } next) sb.AppendLine($"        {MethodName(Find(graph, next))}();");
        }
        else if (node.Kind == GraphNodeKind.Condition)
        {
            var condition = node.Condition switch
            {
                GraphConditionKind.Always => "true",
                GraphConditionKind.HasItem => $"Find(\"{Escape(node.TargetName)}\")?.GetComponent<Inventory>()?.Has(\"{Escape(node.ItemId)}\") == true",
                GraphConditionKind.HealthBelow => $"Find(\"{Escape(node.TargetName)}\")?.GetComponent<Health>()?.Current <= {node.Threshold:0.###}f",
                GraphConditionKind.Expression => $"ExpressionEvaluator.EvaluateBool(\"{Escape(node.Expression)}\", new ExpressionContext {{ Player = Find(\"player\") }})",
                _ => "true",
            };
            sb.AppendLine($"        // Condición: {node.Condition} {(string.IsNullOrWhiteSpace(node.Expression) ? "" : node.Expression)}");
            sb.AppendLine($"        if ({condition})");
            sb.AppendLine("        {");
            if (node.TrueNext is { } t) sb.AppendLine($"            {MethodName(Find(graph, t))}();");
            sb.AppendLine("        }");
            sb.AppendLine("        else");
            sb.AppendLine("        {");
            if (node.FalseNext is { } f) sb.AppendLine($"            {MethodName(Find(graph, f))}();");
            sb.AppendLine("        }");
        }
        else if (node.Kind == GraphNodeKind.Action)
        {
            EmitAction(sb, node);
            if (node.Next is { } next) sb.AppendLine($"        {MethodName(Find(graph, next))}();");
        }
        sb.AppendLine("    }");
    }

    private static void EmitAction(StringBuilder sb, GraphNode node)
    {
        var target = string.IsNullOrWhiteSpace(node.TargetName) ? "null" : $"Find(\"{Escape(node.TargetName)}\")";
        switch (node.Action)
        {
            case ActionKind.Message:
                sb.AppendLine($"        Debug(\"{Escape(node.Message)}\");");
                break;
            case ActionKind.Activate:
                sb.AppendLine($"        if ({target} is {{ }} activate) activate.Active = true;");
                break;
            case ActionKind.Deactivate:
                sb.AppendLine($"        if ({target} is {{ }} deactivate) deactivate.Active = false;");
                break;
            case ActionKind.Teleport:
                sb.AppendLine($"        if ({target} is {{ }} point) Position = Scene!.GetWorldPosition(point);");
                break;
            default:
                sb.AppendLine($"        ActionExecutor.Execute(Scene!, ActionKind.{node.Action}, \"{Escape(node.TargetName)}\", \"{Escape(node.Message)}\", \"player\", {node.Amount:0.###}f);");
                break;
        }
    }

    private static GraphNode? Find(NodeGraph graph, Guid id) => graph.Nodes.FirstOrDefault(n => n.Id == id);

    private static string MethodName(GraphNode? node) => node is null ? "Noop" : "Node_" + Short(node.Id);

    private static string Short(Guid id) => id.ToString("N")[..8];

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
