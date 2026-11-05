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
        sb.AppendLine("    private readonly Dictionary<string, float> _vars = new();");

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
        foreach (var custom in graph.Nodes.Where(node => node.Kind == GraphNodeKind.CustomEvent))
        {
            EmitCustomEvent(sb, graph, custom, emitted);
        }
        foreach (var node in events)
        {
            EmitChain(sb, graph, node, emitted);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitCustomEvent(StringBuilder sb, NodeGraph graph, GraphNode node, HashSet<Guid> emitted)
    {
        var parameters = string.Join(", ", node.ParameterText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Select(item =>
            {
                var parts = item.Split(':', StringSplitOptions.TrimEntries);
                var name = SafeName(parts[0], "arg");
                var type = parts.Length > 1 ? parts[1] : "Number";
                return $"{TypeName(type)} {name}";
            }));
        sb.AppendLine();
        sb.AppendLine($"    private void {CustomMethodName(node)}({parameters})");
        sb.AppendLine("    {");
        if (node.Next is { } body) sb.AppendLine($"        {MethodName(Find(graph, body))}();");
        sb.AppendLine("    }");
        if (node.Next is { } next) EmitChain(sb, graph, Find(graph, next), emitted);
    }

    private static string TypeName(string type) => type.ToLowerInvariant() switch
    {
        "boolean" or "bool" => "bool",
        "integer" or "int" => "int",
        "text" or "string" => "string",
        "vector2" => "Vector2",
        _ => "float",
    };

    private static string CustomMethodName(GraphNode node) => "Event_" + SafeName(node.CustomEventName, "Evento");

    private static string SafeName(string value, string fallback)
    {
        var builder = new StringBuilder();
        foreach (var character in value) if (char.IsLetterOrDigit(character) || character == '_') builder.Append(character);
        var result = builder.Length == 0 ? fallback : builder.ToString();
        return char.IsDigit(result[0]) ? "E" + result : result;
    }
    private static void EmitChain(StringBuilder sb, NodeGraph graph, GraphNode? node, HashSet<Guid> emitted)
    {
        while (node is not null && emitted.Add(node.Id))
        {
            EmitNode(sb, graph, node);
            if (node.Kind == GraphNodeKind.Loop)
            {
                if (node.Next is { } body) EmitChain(sb, graph, Find(graph, body), emitted);
                node = node.LoopAfter is { } after ? Find(graph, after) : null;
            }
            else node = node.Next is { } next ? Find(graph, next) : null;
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
                GraphConditionKind.HasItem => $"GameplayReflection.HasItem(Find(\"{Escape(node.TargetName)}\"), \"{Escape(node.ItemId)}\")",
                GraphConditionKind.HealthBelow => $"GameplayReflection.HealthBelow(Find(\"{Escape(node.TargetName)}\"), {node.Threshold:0.###}f)",
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
        else if (node.Kind == GraphNodeKind.CallEvent)
        {
            sb.AppendLine($"        {CustomMethodName(node)}({node.ArgumentsText});");
        }
        else if (node.Kind == GraphNodeKind.Loop)
        {
            var variable = string.IsNullOrWhiteSpace(node.LoopVariable) ? "i" : node.LoopVariable;
            var header = node.LoopKind switch
            {
                GraphLoopKind.While => $"while ({node.LoopExpression})",
                GraphLoopKind.ForEach => $"foreach (var {variable} in {node.LoopCollection})",
                _ => $"for (int {variable} = 0; {variable} < {node.LoopCount}; {variable}++)",
            };
            sb.AppendLine($"        {header}");
            sb.AppendLine("        {");
            var body = node.Next is { } bodyId ? Find(graph, bodyId) : null;
            var guard = 0;
            while (body is not null && guard++ < 64)
            {
                if (body.Kind == GraphNodeKind.Break) { sb.AppendLine("            break;"); body = null; break; }
                if (body.Kind == GraphNodeKind.Continue) { sb.AppendLine("            continue;"); body = null; break; }
                if (body.Kind == GraphNodeKind.Return) { sb.AppendLine("            return;"); body = null; break; }
                if (body.Kind == GraphNodeKind.Action)
                {
                    var actionCode = new StringBuilder();
                    EmitAction(actionCode, graph, body);
                    foreach (var line in actionCode.ToString().Split('\n'))
                        if (line.Trim().Length > 0) sb.AppendLine("            " + line.TrimStart());
                }
                else
                {
                    sb.AppendLine($"            {MethodName(body)}();");
                    break;
                }
                body = body.Next is { } nextBody ? Find(graph, nextBody) : null;
            }
            sb.AppendLine("        }");
            if (node.LoopAfter is { } after) sb.AppendLine($"        {MethodName(Find(graph, after))}();");
        }
        else if (node.Kind == GraphNodeKind.Reroute)
        {
            if (node.Next is { } rerouteNext) sb.AppendLine($"        {MethodName(Find(graph, rerouteNext))}();");
        }
        else if (node.Kind == GraphNodeKind.Action)
        {
            EmitAction(sb, graph, node);
            if (node.Next is { } next) sb.AppendLine($"        {MethodName(Find(graph, next))}();");
        }
        sb.AppendLine("    }");
    }

    private static void EmitAction(StringBuilder sb, NodeGraph graph, GraphNode node)
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
            case ActionKind.SetVariable:
                sb.AppendLine($"        _vars[\"{Escape(node.VariableName)}\"] = {ValueOf(graph, node)};");
                break;
            case ActionKind.AddVariable:
                sb.AppendLine($"        _vars[\"{Escape(node.VariableName)}\"] = _vars.GetValueOrDefault(\"{Escape(node.VariableName)}\") + {ValueOf(graph, node)};");
                break;
            case ActionKind.ToggleFlag:
                sb.AppendLine($"        _vars[\"{Escape(node.VariableName)}\"] = _vars.GetValueOrDefault(\"{Escape(node.VariableName)}\") > 0f ? 0f : 1f;");
                break;
            default:
                if (VectorOf(graph, node) is { } vector)
                    sb.AppendLine($"        ActionExecutor.Execute(Scene!, ActionKind.{node.Action}, \"{Escape(node.TargetName)}\", \"{Escape(node.Message)}\", \"player\", 0f, \"{Escape(node.ComponentName)}\", \"{Escape(node.PropertyName)}\", {vector});");
                else
                    sb.AppendLine($"        ActionExecutor.Execute(Scene!, ActionKind.{node.Action}, \"{Escape(node.TargetName)}\", \"{Escape(node.Message)}\", \"player\", {ValueOf(graph, node)}, \"{Escape(node.ComponentName)}\", \"{Escape(node.PropertyName)}\");");
                break;
        }
    }

    private static string? VectorOf(NodeGraph graph, GraphNode node)
    {
        var port = string.IsNullOrWhiteSpace(node.PropertyName) ? "amount" : node.PropertyName;
        var link = graph.DataLinkTo(node.Id, port) ?? graph.DataLinkTo(node.Id, "amount");
        if (link is null) return null;
        var source = graph.Find(link.SourceNodeId);
        var guard = 0;
        while (source is { Kind: GraphNodeKind.TunnelOut } && guard++ < 16 && graph.DataLinkTo(source.Id, "value") is { } tunnelLink)
            source = graph.Find(tunnelLink.SourceNodeId);
        if (source is not { Kind: GraphNodeKind.Value, ValueKind: GraphValueKind.Vector2 }) return null;
        var parts = source.Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length < 2) return null;
        return $"new Vector2({parts[0]}f, {parts[1]}f)";
    }
    private static string ValueOf(NodeGraph graph, GraphNode node)
    {
        var port = node.Action == ActionKind.SetComponentProperty && !string.IsNullOrWhiteSpace(node.PropertyName) ? node.PropertyName : "amount";
        if ((graph.DataLinkTo(node.Id, port) ?? graph.DataLinkTo(node.Id, "amount")) is { } link && graph.Find(link.SourceNodeId) is { Kind: GraphNodeKind.Value } value)
        {
            return value.ValueKind switch
            {
                GraphValueKind.Text => $"\"{Escape(value.Value)}\"",
                GraphValueKind.Boolean => value.Value is "true" or "True" or "1" ? "true" : "false",
                GraphValueKind.Integer => int.TryParse(value.Value, out var integer) ? integer.ToString() : "0",
                _ => float.TryParse(value.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "0",
            };
        }
        return node.Amount.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
    private static GraphNode? Find(NodeGraph graph, Guid id) => graph.Nodes.FirstOrDefault(n => n.Id == id);

    private static string MethodName(GraphNode? node) => node is null ? "Noop" : "Node_" + Short(node.Id);

    private static string Short(Guid id) => id.ToString("N")[..8];

    private static string Escape(string value) => value.Replace("\\", "\\\\").Replace("\"", "\\\"");
}
