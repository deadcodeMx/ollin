using System.Text;
using Ollin.Core.Model.Components;

namespace Ollin.Core.Scripting;

/// <summary>Genera un script C# editable a partir de un NodeGraph.</summary>
public static class GraphCodeGenerator
{
    private static HashSet<string>? _currentLocals;
    public static string Generate(NodeGraph graph, string className = "GraphScript")
    {
        var sb = new StringBuilder();
        sb.AppendLine("using System;");
        sb.AppendLine("using System.Collections.Generic;");
        sb.AppendLine("using System.Numerics;");
        sb.AppendLine("using Ollin.Core.Model.Components;");
        sb.AppendLine("using Ollin.Scripting;");
        sb.AppendLine("using Ollin.Core.Saves;");
        sb.AppendLine("using Ollin.Core.Scripting;");
        sb.AppendLine();
        sb.AppendLine($"public sealed class {className} : OllinScript");
        sb.AppendLine("{");
        sb.AppendLine("    // Generado por Ollin desde el grafo visual. Edita libremente.");
        sb.AppendLine("    private readonly Dictionary<string, float> _vars = new();");
        foreach (var variable in graph.Variables)
        {
            sb.AppendLine($"    private {TypeName(variable.Type.ToString())} {VarField(variable)} = {VarDefault(variable)};");
        }

        var events = graph.Nodes.Where(n => n.Kind == GraphNodeKind.Event).ToList();
        foreach (var node in events)
        {
            switch (node.Event)
            {
                case GraphEventKind.OnStart:
                    sb.AppendLine("    public override void OnStart()");
                    sb.AppendLine("    {");
                    sb.AppendLine("        SyncVariables();");
                    sb.AppendLine($"        {MethodName(node)}();");
                    sb.AppendLine("    }");
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
        foreach (var subgraph in graph.Subgraphs) EmitSubgraph(sb, subgraph, new HashSet<string>(StringComparer.OrdinalIgnoreCase));
        foreach (var function in graph.Nodes.Where(node => node.Kind == GraphNodeKind.Function))
        {
            EmitFunction(sb, graph, function, emitted);
        }
        foreach (var custom in graph.Nodes.Where(node => node.Kind == GraphNodeKind.CustomEvent))
        {
            EmitCustomEvent(sb, graph, custom, emitted);
        }
        EmitStateSync(sb, graph);
        foreach (var node in events)
        {
            EmitChain(sb, graph, node, emitted);
        }

        sb.AppendLine("}");
        return sb.ToString();
    }

    private static void EmitBreakpoint(StringBuilder sb, NodeGraph graph, GraphNode node, int indent)
    {
        if (!graph.Breakpoints.Contains(node.Id)) return;
        sb.AppendLine($"{new string(' ', indent * 4)}System.Diagnostics.Debugger.Break(); // breakpoint");
    }
    private static void EmitInlineChain(StringBuilder sb, NodeGraph graph, GraphNode? node, int indent, bool insideFunction, bool useLocals = false, HashSet<Guid>? visited = null)
    {
        visited ??= new HashSet<Guid>();
        var guard = 0;
        while (node is not null && guard++ < 128 && visited.Add(node.Id))
        {
            EmitBreakpoint(sb, graph, node, indent);
            switch (node.Kind)
            {
                case GraphNodeKind.Action:
                {
                    var action = new StringBuilder();
                    EmitAction(action, graph, node);
                    foreach (var line in action.ToString().Split('\n'))
                        if (line.Trim().Length > 0) sb.AppendLine(new string(' ', indent * 4) + line.TrimStart());
                    node = node.Next is { } next ? Find(graph, next) : null;
                    break;
                }
                case GraphNodeKind.CallEvent:
                    sb.AppendLine($"{new string(' ', indent * 4)}{CustomMethodName(node)}({node.ArgumentsText});");
                    node = node.Next is { } callNext ? Find(graph, callNext) : null;
                    break;
                case GraphNodeKind.CallFunction:
                    sb.AppendLine($"{new string(' ', indent * 4)}Func_{SafeName(node.FunctionName, "Funcion")}({node.ArgumentsText});");
                    node = node.Next is { } funcNext ? Find(graph, funcNext) : null;
                    break;
                case GraphNodeKind.Break:
                    sb.AppendLine($"{new string(' ', indent * 4)}break;");
                    return;
                case GraphNodeKind.Continue:
                    sb.AppendLine($"{new string(' ', indent * 4)}continue;");
                    return;
                case GraphNodeKind.Return:
                    sb.AppendLine($"{new string(' ', indent * 4)}return {(insideFunction ? node.ReturnExpression : string.Empty)};");
                    return;
                case GraphNodeKind.Condition:
                {
                    sb.AppendLine($"{new string(' ', indent * 4)}if ({ConditionExpression(node, useLocals)})");
                    sb.AppendLine($"{new string(' ', indent * 4)}{{");
                    EmitInlineChain(sb, graph, node.TrueNext is { } t ? Find(graph, t) : null, indent + 1, insideFunction, useLocals, visited);
                    sb.AppendLine($"{new string(' ', indent * 4)}}}");
                    sb.AppendLine($"{new string(' ', indent * 4)}else");
                    sb.AppendLine($"{new string(' ', indent * 4)}{{");
                    EmitInlineChain(sb, graph, node.FalseNext is { } f ? Find(graph, f) : null, indent + 1, insideFunction, useLocals, visited);
                    sb.AppendLine($"{new string(' ', indent * 4)}}}");
                    return;
                }
                case GraphNodeKind.Loop:
                {
                    var variable = string.IsNullOrWhiteSpace(node.LoopVariable) ? "i" : node.LoopVariable;
                    var header = node.LoopKind switch
                    {
                        GraphLoopKind.While => $"while ({node.LoopExpression})",
                        GraphLoopKind.ForEach => $"foreach (var {variable} in {node.LoopCollection})",
                        _ => $"for (int {variable} = 0; {variable} < {node.LoopCount}; {variable}++)",
                    };
                    if (node.LoopKind == GraphLoopKind.While)
                        sb.AppendLine($"{new string(' ', indent * 4)}var __loop_guard_{Short(node.Id)} = 0;");
                    sb.AppendLine($"{new string(' ', indent * 4)}{header}");
                    sb.AppendLine($"{new string(' ', indent * 4)}{{");
                    if (node.LoopKind == GraphLoopKind.While)
                        sb.AppendLine($"{new string(' ', (indent + 1) * 4)}if (++__loop_guard_{Short(node.Id)} > {System.Math.Max(1, node.LoopMaxIterations)}) {{ Debug(\"Bucle while detenido por límite\"); break; }}");
                    EmitInlineChain(sb, graph, node.Next is { } b ? Find(graph, b) : null, indent + 1, insideFunction, useLocals, visited);
                    sb.AppendLine($"{new string(' ', indent * 4)}}}");
                    if (node.LoopAfter is { } after) node = Find(graph, after); else return;
                    break;
                }
                case GraphNodeKind.Subgraph:
                    sb.AppendLine($"{new string(' ', indent * 4)}Sub_{SafeName(node.SubgraphName, "Subgrafo")}();");
                    node = node.Next is { } subNext ? Find(graph, subNext) : null;
                    break;
                case GraphNodeKind.Reroute:
                    node = node.Next is { } reroute ? Find(graph, reroute) : null;
                    break;
                default:
                    return;
            }
        }
    }

    private static string ConditionExpression(GraphNode node, bool useLocals = false) => node.Condition switch
    {
        GraphConditionKind.HasItem => $"GameplayReflection.HasItem(Find(\"{Escape(node.TargetName)}\"), \"{Escape(node.ItemId)}\")",
        GraphConditionKind.HealthBelow => $"GameplayReflection.HealthBelow(Find(\"{Escape(node.TargetName)}\"), {node.Threshold:0.###}f)",
        GraphConditionKind.Expression => $"ExpressionEvaluator.EvaluateBool(\"{Escape(node.Expression)}\", new ExpressionContext {{ Player = Find(\"player\"){(useLocals ? ", Locals = __locals" : string.Empty)} }})",
        _ => "true",
    };
    private static void EmitStateSync(StringBuilder sb, NodeGraph graph)
    {

        sb.AppendLine();
        sb.AppendLine("    private void SyncVariables()");
        sb.AppendLine("    {");
        sb.AppendLine("        if (GameStateHost.Current is not { } state) return;");
        foreach (var variable in graph.Variables)
        {
            var name = Escape(variable.Name);
            var field = VarField(variable);
            var line = variable.Type switch
            {
                GraphPinType.Boolean => $"        if (state.Has(\"{name}\")) {field} = state.GetFlag(\"{name}\", {field});",
                GraphPinType.Text => $"        if (state.Has(\"{name}\")) {field} = state.Get(\"{name}\") ?? {field};",
                GraphPinType.Integer => $"        if (state.Has(\"{name}\")) {field} = (int)state.GetNumber(\"{name}\", {field});",
                GraphPinType.Number => $"        if (state.Has(\"{name}\")) {field} = state.GetNumber(\"{name}\", {field});",
                _ => null,
            };
            if (line is not null) sb.AppendLine(line);
        }
        sb.AppendLine("    }");
    }

    private static string StateWrite(NodeGraph graph, string variableName, string field)
    {
        var variable = FindVariable(graph, variableName);
        var name = Escape(variableName);
        return variable?.Type switch
        {
            GraphPinType.Boolean => $" GameStateHost.Current?.SetFlag(\"{name}\", {field});",
            GraphPinType.Text => $" GameStateHost.Current?.Set(\"{name}\", {field});",
            _ => $" GameStateHost.Current?.SetNumber(\"{name}\", {field});",
        };
    }
    private static void EmitSubgraph(StringBuilder sb, NodeGraph subgraph, HashSet<string> emitted)
    {
        var name = "Sub_" + SafeName(subgraph.Name, "Subgrafo");
        if (!emitted.Add(name)) return;
        var start = subgraph.Nodes.FirstOrDefault(node => node.Kind == GraphNodeKind.Event && node.Next is not null)?.Next
            ?? subgraph.Nodes.FirstOrDefault(node => node.Kind is not (GraphNodeKind.Event or GraphNodeKind.Comment or GraphNodeKind.Group))?.Id;
        sb.AppendLine();
        sb.AppendLine($"    private void {name}()");
        sb.AppendLine("    {");
        EmitInlineChain(sb, subgraph, start is { } id ? subgraph.Find(id) : null, 2, insideFunction: false);
        sb.AppendLine("    }");
        foreach (var nested in subgraph.Subgraphs) EmitSubgraph(sb, nested, emitted);
    }
    private static void EmitFunction(StringBuilder sb, NodeGraph graph, GraphNode node, HashSet<Guid> emitted)
    {
        var parameters = ParameterList(node);
        var returnType = TypeName(node.ReturnType.ToString());
        sb.AppendLine();
        sb.AppendLine($"    private {returnType} Func_{SafeName(node.FunctionName, "Funcion")}({parameters})");
        sb.AppendLine("    {");
        var localDeclarations = LocalDeclarations(node);
        foreach (var declaration in localDeclarations) sb.AppendLine("        " + declaration);
        _currentLocals = LocalNames(node);
        var functionLocals = LocalsInitializer(graph, node);
        if (functionLocals.Length > 0) sb.AppendLine("        " + functionLocals);
        EmitInlineChain(sb, graph, node.Next is { } bodyId ? Find(graph, bodyId) : null, 2, insideFunction: true, useLocals: true);
        sb.AppendLine("        return default;");
        sb.AppendLine("    }");
        _currentLocals = null;
    }

    private static string OperatorExpression(NodeGraph graph, GraphNode node)
    {
        var left = RewriteVariables(graph, node.LeftExpression);
        var right = RewriteVariables(graph, node.RightExpression);
        return node.OperatorKind switch
        {
            GraphOperatorKind.Add => $"({left} + {right})",
            GraphOperatorKind.Subtract => $"({left} - {right})",
            GraphOperatorKind.Multiply => $"({left} * {right})",
            GraphOperatorKind.Divide => $"({left} / {right})",
            GraphOperatorKind.Modulo => $"({left} % {right})",
            GraphOperatorKind.Equal => $"({left} == {right})",
            GraphOperatorKind.NotEqual => $"({left} != {right})",
            GraphOperatorKind.Greater => $"({left} > {right})",
            GraphOperatorKind.Less => $"({left} < {right})",
            GraphOperatorKind.And => $"({left} && {right})",
            GraphOperatorKind.Or => $"({left} || {right})",
            GraphOperatorKind.Not => $"(!({left}))",
            _ => "0",
        };
    }
    private static string RewriteVariables(NodeGraph graph, string expression)
    {
        var result = expression;
        foreach (var variable in graph.Variables)
        {
            result = System.Text.RegularExpressions.Regex.Replace(
                result,
                $@"\b{System.Text.RegularExpressions.Regex.Escape(variable.Name)}\b",
                VarField(variable));
        }
        return result;
    }
    private static GraphVariable? FindVariable(NodeGraph graph, string name) =>
        graph.Variables.FirstOrDefault(variable => string.Equals(variable.Name, name, StringComparison.OrdinalIgnoreCase));

    private static string VarField(GraphVariable variable) => "_var_" + SafeName(variable.Name, "variable");

    private static string VarDefault(GraphVariable variable) => variable.Type switch
    {
        GraphPinType.Text => $"\"{Escape(variable.DefaultValue)}\"",
        GraphPinType.Boolean => variable.DefaultValue is "true" or "True" or "1" ? "true" : "false",
        GraphPinType.Integer => int.TryParse(variable.DefaultValue, out var integer) ? integer.ToString() : "0",
        GraphPinType.Vector2 => VectorDefault(variable.DefaultValue),
        _ => float.TryParse(variable.DefaultValue, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) + "f" : "0f",
    };

    private static string VectorDefault(string value)
    {
        var parts = value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        return parts.Length >= 2 ? $"new Vector2({parts[0]}f, {parts[1]}f)" : "new Vector2(0f, 0f)";
    }
    private static HashSet<string> LocalNames(GraphNode function) => function.LocalVariableText
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(item => SafeName(item.Split(':', '=')[0].Trim(), "local"))
        .ToHashSet(StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> LocalDeclarations(GraphNode function)
    {
        foreach (var item in function.LocalVariableText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var parts = item.Split(':', '=');
            var name = SafeName(parts[0].Trim(), "local");
            var type = parts.Length > 1 ? TypeName(parts[1].Trim()) : "float";
            var value = parts.Length > 2 ? parts[2].Trim() : (type == "string" ? "\"\"" : type == "bool" ? "false" : type == "Vector2" ? "new Vector2(0f, 0f)" : "0f");
            yield return $"{type} {name} = {value};";
        }
    }
    private static string LocalsInitializer(NodeGraph graph, GraphNode definition)
    {
        var entries = new List<string>();
        var used = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var parameter in definition.ParameterText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
        {
            var name = SafeName(parameter.Split(':', StringSplitOptions.TrimEntries)[0], "arg");
            if (used.Add(name)) entries.Add($"[\"{name}\"] = {name}");
        }
        foreach (var local in LocalNames(definition))
            if (used.Add(local)) entries.Add($"[\"{local}\"] = {local}");
        foreach (var variable in graph.Variables)
        {
            var name = SafeName(variable.Name, "variable");
            if (used.Add(name))
            {
                var field = FindVariable(graph, variable.Name) is { } typed ? VarField(typed) : $"_vars.GetValueOrDefault(\"{name}\")";
                entries.Add($"[\"{name}\"] = {field}");
            }
        }
        return entries.Count == 0 ? string.Empty : $"var __locals = new Dictionary<string, object> {{ {string.Join(", ", entries)} }};";
    }
    private static string ParameterList(GraphNode node) => string.Join(", ", node.ParameterText
        .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
        .Select(item =>
        {
            var parts = item.Split(':', StringSplitOptions.TrimEntries);
            return $"{TypeName(parts.Length > 1 ? parts[1] : "Number")} {SafeName(parts[0], "arg")}";
        }));

    private static string TypeName(string type) => type.ToLowerInvariant() switch
    {
        "boolean" or "bool" => "bool",
        "integer" or "int" => "int",
        "text" or "string" => "string",
        "vector2" => "Vector2",
        _ => "float",
    };
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
        var eventLocals = LocalsInitializer(graph, node);
        if (eventLocals.Length > 0) sb.AppendLine("        " + eventLocals);
        if (node.Next is { } body) EmitInlineChain(sb, graph, Find(graph, body), 2, insideFunction: false, useLocals: true);
        sb.AppendLine("    }");
    }


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
        EmitBreakpoint(sb, graph, node, 2);
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
        else if (node.Kind == GraphNodeKind.CallFunction)
        {
            sb.AppendLine($"        Func_{SafeName(node.FunctionName, "Funcion")}({node.ArgumentsText});");
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
            if (node.LoopKind == GraphLoopKind.While)
                sb.AppendLine($"        var __loop_guard_{Short(node.Id)} = 0;");
            sb.AppendLine($"        {header}");
            sb.AppendLine("        {");
            if (node.LoopKind == GraphLoopKind.While)
                sb.AppendLine($"            if (++__loop_guard_{Short(node.Id)} > {System.Math.Max(1, node.LoopMaxIterations)}) {{ Debug(\"Bucle while detenido por límite\"); break; }}");
            EmitInlineChain(sb, graph, node.Next is { } bodyId ? Find(graph, bodyId) : null, 3, insideFunction: false, useLocals: false);
            sb.AppendLine("        }");
            if (node.LoopAfter is { } after) sb.AppendLine($"        {MethodName(Find(graph, after))}();");
        }
        else if (node.Kind == GraphNodeKind.Subgraph)
        {
            sb.AppendLine($"        Sub_{SafeName(node.SubgraphName, "Subgrafo")}();");
            if (node.Next is { } subNext) sb.AppendLine($"        {MethodName(Find(graph, subNext))}();");
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
        var variable = FindVariable(graph, node.VariableName);
        var localField = _currentLocals?.Contains(node.VariableName) == true ? SafeName(node.VariableName, "local") : null;
        var variableField = localField ?? (variable is null ? $"_vars[\"{Escape(node.VariableName)}\"]" : VarField(variable));
        switch (node.Action)
        {
            case ActionKind.Message:
                sb.AppendLine($"        Debug({FieldText(graph, node, "message", node.Message)});");
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
                sb.AppendLine($"        {variableField} = {ValueOrExpression(graph, node)};{StateWrite(graph, node.VariableName, variableField)}");
                break;
            case ActionKind.AddVariable:
                sb.AppendLine($"        {variableField} += {ValueOrExpression(graph, node)};{StateWrite(graph, node.VariableName, variableField)}");
                break;
            case ActionKind.ToggleFlag:
                if (variable?.Type == GraphPinType.Boolean) sb.AppendLine($"        {variableField} = !{variableField};{StateWrite(graph, node.VariableName, variableField)}");
                else sb.AppendLine($"        {variableField} = {variableField} > 0f ? 0f : 1f;");
                break;
            default:
                if (VectorOf(graph, node) is { } vector)
                    sb.AppendLine($"        ActionExecutor.Execute(Scene!, ActionKind.{node.Action}, \"{Escape(node.TargetName)}\", {FieldText(graph, node, "message", node.Message)}, \"player\", 0f, {FieldText(graph, node, "component", node.ComponentName)}, {FieldText(graph, node, "property", node.PropertyName)}, {vector}, {node.Radius:0.###}f, {node.Interval:0.###}f, {node.Threshold:0.###}f);");
                else
                    sb.AppendLine($"        ActionExecutor.Execute(Scene!, ActionKind.{node.Action}, \"{Escape(node.TargetName)}\", {FieldText(graph, node, "message", node.Message)}, \"player\", {ValueOf(graph, node)}, {FieldText(graph, node, "component", node.ComponentName)}, {FieldText(graph, node, "property", node.PropertyName)}, default, {node.Radius:0.###}f, {node.Interval:0.###}f, {node.Threshold:0.###}f);");
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
    private static string FieldText(NodeGraph graph, GraphNode node, string port, string fallback)
    {
        if (graph.DataLinkTo(node.Id, port) is { } link && graph.Find(link.SourceNodeId) is { } source)
        {
            if (source.Kind == GraphNodeKind.Value && source.ValueKind == GraphValueKind.Text) return $"\"{Escape(source.Value)}\"";
            if (source.Kind == GraphNodeKind.CallFunction) return $"Func_{SafeName(source.FunctionName, "Funcion")}({source.ArgumentsText})";
        }
        return $"\"{Escape(fallback)}\"";
    }
    private static string FieldNumber(NodeGraph graph, GraphNode node, string port, float fallback)
    {
        if (graph.DataLinkTo(node.Id, port) is { } link && graph.Find(link.SourceNodeId) is { Kind: GraphNodeKind.Value } value)
        {
            return value.ValueKind switch
            {
                GraphValueKind.Integer => int.TryParse(value.Value, out var integer) ? integer.ToString() : "0",
                GraphValueKind.Boolean => value.Value is "true" or "True" or "1" ? "1" : "0",
                GraphValueKind.Vector2 => "0",
                _ => float.TryParse(value.Value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var number) ? number.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture) : "0",
            };
        }
        return fallback.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
    }
    private static string ValueOrExpression(NodeGraph graph, GraphNode node)
    {
        if (graph.DataLinkTo(node.Id, "amount") is not null) return ValueOf(graph, node);
        return string.IsNullOrWhiteSpace(node.Expression)
            ? ValueOf(graph, node)
            : RewriteVariables(graph, node.Expression);
    }
    private static string ValueOf(NodeGraph graph, GraphNode node)
    {
        var port = node.Action == ActionKind.SetComponentProperty && !string.IsNullOrWhiteSpace(node.PropertyName) ? node.PropertyName : "amount";
        if ((graph.DataLinkTo(node.Id, port) ?? graph.DataLinkTo(node.Id, "amount")) is { } link && graph.Find(link.SourceNodeId) is { } source)
        {
            if (source is { Kind: GraphNodeKind.CallFunction }) return $"Func_{SafeName(source.FunctionName, "Funcion")}({source.ArgumentsText})";
            if (source is { Kind: GraphNodeKind.Operator }) return OperatorExpression(graph, source);
            if (source is not { Kind: GraphNodeKind.Value } value) return node.Amount.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
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
