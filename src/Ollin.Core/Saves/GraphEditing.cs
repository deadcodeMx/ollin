using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Core.Scripting;

namespace Ollin.Core.Saves;

public sealed record GraphSnapshot(List<GraphNode> Nodes, Guid? SelectedId, List<Guid>? SelectedIds = null);

/// <summary>Utilidades de edición del grafo: copia, instantáneas e historial.</summary>
public static class GraphEditing
{
    public static GraphNode CloneNode(GraphNode node, bool keepLinks = true) => new()
    {
        Id = node.Id,
        Kind = node.Kind,
        Event = node.Event,
        Condition = node.Condition,
        Action = node.Action,
        X = node.X,
        Y = node.Y,
        TargetName = node.TargetName,
        Message = node.Message,
        ItemId = node.ItemId,
        VariableName = node.VariableName,
        Expression = node.Expression,
        Radius = node.Radius,
        Interval = node.Interval,
        Amount = node.Amount,
        Threshold = node.Threshold,
        Once = node.Once,
        GroupWidth = node.GroupWidth,
        GroupHeight = node.GroupHeight,
        Collapsed = node.Collapsed,
        ParentGroupId = node.ParentGroupId,
        Next = keepLinks ? node.Next : null,
        TrueNext = keepLinks ? node.TrueNext : null,
        FalseNext = keepLinks ? node.FalseNext : null,
        DisabledPorts = keepLinks ? new HashSet<string>(node.DisabledPorts) : new(),
    };

    public static GraphSnapshot Capture(NodeGraph graph, Guid? selectedId, IEnumerable<Guid>? selectedIds = null) => new(
        graph.Nodes.Select(node => CloneNode(node)).ToList(),
        selectedId,
        selectedIds?.ToList() ?? (selectedId is { } id ? new List<Guid> { id } : new List<Guid>()));

    public static Guid? Restore(NodeGraph graph, GraphSnapshot snapshot)
    {
        graph.Nodes = snapshot.Nodes.Select(node => CloneNode(node)).ToList();
        return snapshot.SelectedId;
    }

    public static GraphNode Paste(GraphNode clipboard)
    {
        var copy = CloneNode(clipboard, keepLinks: false);
        copy.Id = Guid.NewGuid();
        copy.X += 30;
        copy.Y += 30;
        return copy;
    }
}

/// <summary>Historial de instantáneas del grafo (máximo 80 pasos).</summary>
public sealed class NodeGraphHistory
{
    private readonly Stack<GraphSnapshot> _undo = new();
    private readonly Stack<GraphSnapshot> _redo = new();

    public IReadOnlyList<GraphSnapshot> UndoSnapshots => _undo.ToList();

    public IReadOnlyList<GraphSnapshot> RedoSnapshots => _redo.ToList();

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public GraphSnapshot? LastRestored { get; private set; }

    public void Record(GraphSnapshot snapshot)
    {
        _undo.Push(snapshot);

        while (_undo.Count > 80)
        {
            var keep = _undo.Reverse().Take(80).Reverse().ToList();
            _undo.Clear();
            foreach (var item in keep) _undo.Push(item);
        }

        _redo.Clear();
    }

    public bool Undo(NodeGraph graph, Guid? selectedId, out Guid? newSelectedId)
    {
        newSelectedId = selectedId;

        if (_undo.Count == 0)
        {
            return false;
        }

        _redo.Push(GraphEditing.Capture(graph, selectedId));
        LastRestored = _undo.Pop();
        newSelectedId = GraphEditing.Restore(graph, LastRestored);
        return true;
    }

    public bool Redo(NodeGraph graph, Guid? selectedId, out Guid? newSelectedId)
    {
        newSelectedId = selectedId;

        if (_redo.Count == 0)
        {
            return false;
        }

        _undo.Push(GraphEditing.Capture(graph, selectedId));
        LastRestored = _redo.Pop();
        newSelectedId = GraphEditing.Restore(graph, LastRestored);
        return true;
    }

    public void Replace(IEnumerable<GraphSnapshot> undo, IEnumerable<GraphSnapshot> redo)
    {
        _undo.Clear();
        _redo.Clear();
        foreach (var snapshot in undo.Reverse()) _undo.Push(snapshot);
        foreach (var snapshot in redo.Reverse()) _redo.Push(snapshot);
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
    }
}

public sealed record GraphIssue(GraphNode? Node, string Message, bool IsError);

/// <summary>Validación de grafos: enlaces, objetivos, variables y ciclos.</summary>
public static class GraphValidator
{
    public static List<GraphIssue> Validate(NodeGraph graph, Scene scene)
    {
        var issues = new List<GraphIssue>();

        if (graph.Nodes.All(node => node.Kind != GraphNodeKind.Event))
        {
            issues.Add(new GraphIssue(null, "El grafo no tiene ningún evento de entrada.", true));
        }

        var knownVariables = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var actionNode in graph.Nodes.Where(node => node.Action is ActionKind.SetVariable or ActionKind.AddVariable or ActionKind.ToggleFlag))
        {
            if (!string.IsNullOrWhiteSpace(actionNode.VariableName))
            {
                knownVariables.Add(actionNode.VariableName);
            }
        }

        var incoming = new HashSet<Guid>();

        foreach (var node in graph.Nodes)
        {
            if (node.Kind is GraphNodeKind.Comment or GraphNodeKind.Group)
            {
                continue;
            }

            if (node.Next is { } next) incoming.Add(next);
            if (node.TrueNext is { } yes) incoming.Add(yes);
            if (node.FalseNext is { } no) incoming.Add(no);

            CheckLink(graph, node, node.Next, "salida", issues);
            CheckLink(graph, node, node.TrueNext, "rama Sí", issues);
            CheckLink(graph, node, node.FalseNext, "rama No", issues);

            if (node.Kind == GraphNodeKind.Event && node.Next is null)
            {
                issues.Add(new GraphIssue(node, "El evento no está conectado a ninguna acción.", false));
            }

            if (node.Kind == GraphNodeKind.Condition)
            {
                if (node.TrueNext is null) issues.Add(new GraphIssue(node, "La condición no tiene rama Sí.", false));
                if (node.FalseNext is null) issues.Add(new GraphIssue(node, "La condición no tiene rama No.", false));
            }

            if (node.Kind == GraphNodeKind.Action)
            {
                if (RequiresTarget(node.Action) && string.IsNullOrWhiteSpace(node.TargetName))
                {
                    issues.Add(new GraphIssue(node, "La acción necesita un objetivo.", true));
                }
                else if (!string.IsNullOrWhiteSpace(node.TargetName) && scene.Find(node.TargetName) is null)
                {
                    issues.Add(new GraphIssue(node, $"No existe la entidad «{node.TargetName}».", false));
                }

                if (node.Action is ActionKind.SetVariable or ActionKind.AddVariable or ActionKind.ToggleFlag)
                {
                    if (string.IsNullOrWhiteSpace(node.VariableName))
                    {
                        issues.Add(new GraphIssue(node, "Falta el nombre de la variable.", true));
                    }

                    if (node.Action != ActionKind.ToggleFlag)
                    {
                        var expressionError = ExpressionEvaluator.Validate(node.Expression);

                        if (expressionError is not null)
                        {
                            issues.Add(new GraphIssue(node, $"Expresión inválida: {expressionError}", true));
                        }
                        else
                        {
                            ValidateSemantics(node, node.Expression, knownVariables, issues);
                        }
                    }
                }
            }

            if (node.Kind == GraphNodeKind.Condition && node.Condition == GraphConditionKind.Expression)
            {
                var expressionError = ExpressionEvaluator.Validate(node.Expression);

                if (expressionError is not null)
                {
                    issues.Add(new GraphIssue(node, $"Expresión inválida: {expressionError}", true));
                }
                else
                {
                    ValidateSemantics(node, node.Expression, knownVariables, issues);
                }
            }
        }

        foreach (var node in graph.Nodes.Where(node => node.Kind is not (GraphNodeKind.Event or GraphNodeKind.Comment or GraphNodeKind.Group) && !incoming.Contains(node.Id)))
        {
            issues.Add(new GraphIssue(node, "Nodo sin conexión de entrada.", false));
        }

        ValidateReturns(graph, issues);
        ValidateDuplicateVariables(graph, issues);
        ValidateDuplicateDefinitions(graph, issues);
        DetectCycles(graph, issues);
        return issues;
    }

    private static void ValidateDuplicateDefinitions(NodeGraph graph, List<GraphIssue> issues)
    {
        var functions = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var function in graph.Nodes.Where(node => node.Kind == GraphNodeKind.Function))
        {
            if (string.IsNullOrWhiteSpace(function.FunctionName))
                issues.Add(new GraphIssue(function, "Función sin nombre.", true));
            else if (!functions.Add(function.FunctionName))
                issues.Add(new GraphIssue(function, $"Función duplicada: «{function.FunctionName}».", true));
        }

        var events = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var custom in graph.Nodes.Where(node => node.Kind == GraphNodeKind.CustomEvent))
        {
            if (string.IsNullOrWhiteSpace(custom.CustomEventName))
                issues.Add(new GraphIssue(custom, "Evento personalizado sin nombre.", true));
            else if (!events.Add(custom.CustomEventName))
                issues.Add(new GraphIssue(custom, $"Evento duplicado: «{custom.CustomEventName}».", true));
        }

        foreach (var call in graph.Nodes.Where(node => node.Kind is GraphNodeKind.CallFunction or GraphNodeKind.CallEvent))
        {
            var exists = call.Kind == GraphNodeKind.CallFunction
                ? graph.Nodes.Any(node => node.Kind == GraphNodeKind.Function && node.FunctionName.Equals(call.FunctionName, StringComparison.OrdinalIgnoreCase))
                : graph.Nodes.Any(node => node.Kind == GraphNodeKind.CustomEvent && node.CustomEventName.Equals(call.CustomEventName, StringComparison.OrdinalIgnoreCase));
            if (!exists)
                issues.Add(new GraphIssue(call, $"Llamada a {call.FunctionName}{call.CustomEventName} sin definición.", true));
        }
    }
    private static void ValidateDuplicateVariables(NodeGraph graph, List<GraphIssue> issues)
    {
        var globals = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var variable in graph.Variables)
        {
            if (!globals.Add(variable.Name))
                issues.Add(new GraphIssue(null, $"Variable global duplicada: «{variable.Name}».", true));
        }

        foreach (var function in graph.Nodes.Where(node => node.Kind == GraphNodeKind.Function))
        {
            var names = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var parameter in function.ParameterText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var name = parameter.Split(':', StringSplitOptions.TrimEntries)[0];
                if (!names.Add(name))
                    issues.Add(new GraphIssue(function, $"Parámetro duplicado en {function.FunctionName}: «{name}».", true));
            }
            foreach (var local in function.LocalVariableText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var name = local.Split(':', '=', StringSplitOptions.TrimEntries)[0];
                if (!names.Add(name))
                    issues.Add(new GraphIssue(function, $"Variable local duplicada o igual a un parámetro en {function.FunctionName}: «{name}».", true));
            }
        }
    }
    private static void ValidateReturns(NodeGraph graph, List<GraphIssue> issues)
    {
        var visitedReturns = new HashSet<Guid>();
        foreach (var function in graph.Nodes.Where(node => node.Kind == GraphNodeKind.Function))
        {
            var parameters = new Dictionary<string, GraphPinType>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in function.ParameterText.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
            {
                var parts = item.Split(':', StringSplitOptions.TrimEntries);
                parameters[parts[0]] = parts.Length > 1 && Enum.TryParse<GraphPinType>(parts[1], true, out var type) ? type : GraphPinType.Number;
            }

            foreach (var node in Walk(function.Next, graph))
            {
                if (node.Kind != GraphNodeKind.Return || !visitedReturns.Add(node.Id)) continue;
                var actual = InferReturnType(node.ReturnExpression, graph, parameters);
                if (actual != GraphPinType.Any && !NodeGraph.CanConnect(actual, function.ReturnType))
                    issues.Add(new GraphIssue(node, $"Return de tipo {actual}; la función espera {function.ReturnType}.", true));
            }
        }

        foreach (var node in graph.Nodes.Where(node => node.Kind == GraphNodeKind.Return && !visitedReturns.Contains(node.Id)))
            issues.Add(new GraphIssue(node, "Return fuera de una función.", false));
    }

    private static IEnumerable<GraphNode> Walk(Guid? start, NodeGraph graph)
    {
        var pending = new Stack<GraphNode>();
        var visited = new HashSet<Guid>();
        if (start is { } id && graph.Find(id) is { } first) pending.Push(first);
        while (pending.Count > 0)
        {
            var node = pending.Pop();
            if (!visited.Add(node.Id)) continue;
            yield return node;
            if (node.Next is { } next && graph.Find(next) is { } n) pending.Push(n);
            if (node.TrueNext is { } yes && graph.Find(yes) is { } y) pending.Push(y);
            if (node.FalseNext is { } no && graph.Find(no) is { } f) pending.Push(f);
            if (node.LoopAfter is { } after && graph.Find(after) is { } a) pending.Push(a);
        }
    }

    private static GraphPinType InferReturnType(string expression, NodeGraph graph, IReadOnlyDictionary<string, GraphPinType> parameters)
    {
        var value = expression.Trim();
        if (value.Length >= 2 && value[0] == '"' && value[^1] == '"') return GraphPinType.Text;
        if (value is "true" or "false" or "True" or "False") return GraphPinType.Boolean;
        if (value.Contains(',')) return GraphPinType.Vector2;
        if (int.TryParse(value, out _)) return GraphPinType.Integer;
        if (float.TryParse(value, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out _)) return GraphPinType.Number;
        if (parameters.TryGetValue(value, out var parameter)) return parameter;
        var variable = graph.Variables.FirstOrDefault(item => string.Equals(item.Name, value, StringComparison.OrdinalIgnoreCase));
        if (variable is not null) return variable.Type;
        var function = graph.Nodes.FirstOrDefault(item => item.Kind == GraphNodeKind.Function && item.FunctionName.Equals(value, StringComparison.OrdinalIgnoreCase));
        return function?.ReturnType ?? GraphPinType.Any;
    }
    private static readonly HashSet<string> SystemIdentifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "vida", "health", "vidamax", "healthmax", "vidas", "lives",
    };

    private static readonly HashSet<string> KnownFunctions = new(StringComparer.OrdinalIgnoreCase)
    {
        "has", "tiene", "min", "max",
    };

    private static void ValidateSemantics(GraphNode node, string expression, HashSet<string> knownVariables, List<GraphIssue> issues)
    {
        var analysis = ExpressionEvaluator.Analyze(expression);

        foreach (var function in analysis.Functions.Where(function => !KnownFunctions.Contains(function)))
        {
            issues.Add(new GraphIssue(node, $"Función desconocida: {function}.", true));
        }

        foreach (var identifier in analysis.Identifiers.Where(identifier =>
                     !SystemIdentifiers.Contains(identifier) && !knownVariables.Contains(identifier)))
        {
            issues.Add(new GraphIssue(node, $"Variable no definida: {identifier}.", false));
        }
    }

    private static void CheckLink(NodeGraph graph, GraphNode node, Guid? target, string label, List<GraphIssue> issues)
    {
        if (target is { } id && graph.Find(id) is null)
        {
            issues.Add(new GraphIssue(node, $"La {label} apunta a un nodo inexistente.", true));
        }
    }

    private static bool RequiresTarget(ActionKind action) => action is
        ActionKind.OpenDoor or ActionKind.Activate or ActionKind.Deactivate or
        ActionKind.Spawn or ActionKind.Teleport or ActionKind.Damage;

    private static void DetectCycles(NodeGraph graph, List<GraphIssue> issues)
    {
        var state = new Dictionary<Guid, int>();

        foreach (var node in graph.Nodes)
        {
            Walk(node, state);
        }

        void Walk(GraphNode node, Dictionary<Guid, int> colors)
        {
            if (colors.TryGetValue(node.Id, out var color))
            {
                if (color == 1)
                {
                    issues.Add(new GraphIssue(node, "Ciclo detectado: la ejecución puede repetirse sin fin.", true));
                }

                return;
            }

            colors[node.Id] = 1;

            foreach (var id in new[] { node.Next, node.TrueNext, node.FalseNext }.Where(id => id is not null))
            {
                if (graph.Find(id) is { } next) Walk(next, colors);
            }

            colors[node.Id] = 2;
        }
    }
}
