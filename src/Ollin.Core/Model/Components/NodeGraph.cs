using System.Text.Json.Serialization;
using Ollin.Core.Saves;
using Ollin.Core.Scripting;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

public enum GraphNodeKind
{
    Event,
    Condition,
    Action,
    Reroute,
    Comment,
    Group,
}

public enum GraphEventKind
{
    OnStart,
    OnTimer,
    OnPlayerEnter,
    OnInteract,
    OnPlayerDeath,
}

public enum GraphConditionKind
{
    Always,
    HasItem,
    HealthBelow,
    Expression,
}

public sealed class GraphDebugFrame
{
    public GraphDebugFrame(Guid nodeId, Dictionary<string, string> variablesBefore)
    {
        NodeId = nodeId;
        VariablesBefore = variablesBefore;
    }

    public Guid NodeId { get; }

    public Dictionary<string, string> VariablesBefore { get; }

    public Dictionary<string, string>? VariablesAfter { get; internal set; }
}

/// <summary>Nodo de un grafo visual de ejecución.</summary>
public sealed class GraphNode
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public GraphNodeKind Kind { get; set; } = GraphNodeKind.Action;

    public GraphEventKind Event { get; set; } = GraphEventKind.OnStart;

    public GraphConditionKind Condition { get; set; } = GraphConditionKind.Always;

    public ActionKind Action { get; set; } = ActionKind.Message;

    public float X { get; set; }

    public float Y { get; set; }

    public string TargetName { get; set; } = string.Empty;

    public string Message { get; set; } = "¡Hola!";

    public string ItemId { get; set; } = "llave";

    public string VariableName { get; set; } = "contador";

    public string Expression { get; set; } = "1";

    public float GroupWidth { get; set; } = 280f;

    public float GroupHeight { get; set; } = 160f;

    public bool Collapsed { get; set; }

    /// <summary>Grupo contenedor explícito (opcional).</summary>
    public Guid? ParentGroupId { get; set; }

    public float Radius { get; set; } = 2.5f;

    public float Interval { get; set; } = 3f;

    public float Amount { get; set; } = 20f;

    public float Threshold { get; set; } = 50f;

    public bool Once { get; set; } = true;

    public Guid? Next { get; set; }

    public Guid? TrueNext { get; set; }

    public Guid? FalseNext { get; set; }

    /// <summary>Puertos deshabilitados: "next", "true" o "false".</summary>
    public HashSet<string> DisabledPorts { get; set; } = new();

    [JsonIgnore]
    internal bool Done { get; set; }

    [JsonIgnore]
    internal bool Inside { get; set; }

    [JsonIgnore]
    internal float Timer { get; set; }

    public GraphNode Clone() => new()
    {
        Id = Guid.NewGuid(),
        Kind = Kind,
        Event = Event,
        Condition = Condition,
        Action = Action,
        X = X + 30f,
        Y = Y + 30f,
        TargetName = TargetName,
        Message = Message,
        ItemId = ItemId,
        VariableName = VariableName,
        Expression = Expression,
        Radius = Radius,
        Interval = Interval,
        Amount = Amount,
        Threshold = Threshold,
        Once = Once,
        GroupWidth = GroupWidth,
        GroupHeight = GroupHeight,
        Collapsed = Collapsed,
        ParentGroupId = ParentGroupId,
        DisabledPorts = new HashSet<string>(DisabledPorts),
    };
}

/// <summary>
/// Grafo visual ilimitado: eventos que inician una cadena de condiciones y acciones.
/// Se edita en el editor de nodos y se ejecuta en el runtime sin código.
/// </summary>
public sealed class NodeGraph : Component, ITickable
{
    public List<GraphNode> Nodes { get; set; } = new();

    [JsonIgnore]
    public NodeGraphHistory History { get; } = new();

    [JsonIgnore]
    public HashSet<Guid> Breakpoints { get; } = new();

    [JsonIgnore]
    public Guid? LastNodeId { get; private set; }

    [JsonIgnore]
    public Guid? DebugCursor { get; private set; }

    [JsonIgnore]
    public bool StepModeEnabled { get; set; }

    public bool MoveHiddenMembers { get; set; }

    [JsonIgnore]
    public List<GraphDebugFrame> DebugFrames { get; } = new();

    [JsonIgnore]
    private GraphDebugFrame? _currentDebugFrame;

    [JsonIgnore]
    public List<Guid> ExecutionHistory { get; } = new();

    public GraphNode AddNode(GraphNodeKind kind, float x, float y)
    {
        var node = new GraphNode { Kind = kind, X = x, Y = y };
        Nodes.Add(node);
        return node;
    }

    public GraphNode? Find(Guid? id) => id is null ? null : Nodes.FirstOrDefault(node => node.Id == id);

    public void RemoveNode(Guid id)
    {
        Nodes.RemoveAll(node => node.Id == id);

        foreach (var node in Nodes)
        {
            if (node.Next == id) node.Next = null;
            if (node.TrueNext == id) node.TrueNext = null;
            if (node.FalseNext == id) node.FalseNext = null;
            if (node.ParentGroupId == id) node.ParentGroupId = null;
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (Owner?.Scene is not { } scene)
        {
            return;
        }

        var player = ActionExecutor.FindPlayer(scene);

        foreach (var node in Nodes.ToList())
        {
            if (node.Kind != GraphNodeKind.Event || (node.Done && (node.Once || node.Event == GraphEventKind.OnStart)))
            {
                continue;
            }

            var fire = false;

            switch (node.Event)
            {
                case GraphEventKind.OnStart:
                    fire = true;
                    node.Done = true;
                    break;

                case GraphEventKind.OnTimer:
                    node.Timer -= deltaTime;

                    if (node.Timer <= 0f)
                    {
                        node.Timer = MathF.Max(0.25f, node.Interval);
                        fire = true;
                    }
                    break;

                case GraphEventKind.OnPlayerEnter:
                {
                    var inside = player is not null &&
                                 scene.WorldDistance(Owner, player) <= node.Radius;

                    if (inside && !node.Inside) fire = true;
                    node.Inside = inside;
                    break;
                }

                case GraphEventKind.OnInteract:
                    fire = player is not null &&
                           scene.WorldDistance(Owner, player) <= node.Radius &&
                           InputState.WasKeyPressed(KeyCode.E);
                    break;

                case GraphEventKind.OnPlayerDeath:
                    fire = GameplayReflection.IsDead(player);
                    break;
            }

            if (!fire)
            {
                continue;
            }

            if (node.Once)
            {
                node.Done = true;
            }

            if (RecordExecution(node.Id))
            {
                DebugCursor = node.Id;
                return;
            }

            CompleteDebugFrame();
            Execute(scene, player, Link(node, "next"));
        }
    }

    private void Execute(Scenes.Scene scene, Entity? player, Guid? start, bool singleStep = false, bool ignoreBreakpoint = false)
    {
        var current = start;
        var steps = 0;

        while (current is not null && steps++ < 128)
        {
            var node = Find(current);

            if (node is null)
            {
                return;
            }

            if (RecordExecution(node.Id, !(ignoreBreakpoint && steps == 1)))
            {
                DebugCursor = current;
                return;
            }

            Guid? next;

            if (node.Kind == GraphNodeKind.Condition)
            {
                next = ConditionMet(node, player) ? Link(node, "true") : Link(node, "false");
            }
            else
            {
                if (node.Kind == GraphNodeKind.Action)
                {
                    if (node.Action is ActionKind.SetVariable or ActionKind.AddVariable or ActionKind.ToggleFlag)
                    {
                        ApplyVariable(node, player);
                    }
                    else
                    {
                        ActionExecutor.Execute(scene, node.Action, node.TargetName, node.Message, "player", node.Amount);
                    }
                }

                next = Link(node, "next");
            }

            CompleteDebugFrame();

            if (singleStep)
            {
                DebugCursor = next;
                return;
            }

            current = next;
        }

        DebugCursor = null;
    }

    /// <summary>Avanza un nodo desde el breakpoint actual.</summary>
    public bool StepOnce(Scenes.Scene scene, Entity? player)
    {
        if (DebugCursor is not { } cursor)
        {
            return false;
        }

        DebugCursor = null;
        Execute(scene, player, cursor, singleStep: true, ignoreBreakpoint: true);
        return true;
    }
    private void CompleteDebugFrame()
    {
        if (_currentDebugFrame is null) return;

        _currentDebugFrame.VariablesAfter = GameStateHost.Current is { } state
            ? new Dictionary<string, string>(state.Values, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _currentDebugFrame = null;
    }

    /// <summary>Destino de un puerto, respetando conexiones deshabilitadas.</summary>
    public Guid? Link(GraphNode node, string port)
    {
        if (node.DisabledPorts.Contains(port)) return null;

        return port switch
        {
            "true" => node.TrueNext,
            "false" => node.FalseNext,
            _ => node.Next,
        };
    }

    private static bool ConditionMet(GraphNode node, Entity? player)
    {
        if (player is null)
        {
            return node.Condition == GraphConditionKind.Always;
        }

        return node.Condition switch
        {
            GraphConditionKind.HasItem => GameplayReflection.HasItem(player, node.ItemId),
            GraphConditionKind.HealthBelow => GameplayReflection.HealthBelow(player, node.Threshold),
            GraphConditionKind.Expression => ExpressionEvaluator.EvaluateBool(node.Expression, new ExpressionContext
            {
                State = GameStateHost.Current,
                Player = player,
            }),
            _ => true,
        };
    }

    private bool RecordExecution(Guid nodeId, bool honorBreakpoint = true)
    {
        LastNodeId = nodeId;
        ExecutionHistory.Add(nodeId);
        var variables = GameStateHost.Current is { } state
            ? new Dictionary<string, string>(state.Values, StringComparer.OrdinalIgnoreCase)
            : new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        _currentDebugFrame = new GraphDebugFrame(nodeId, variables);
        DebugFrames.Add(_currentDebugFrame);

        while (ExecutionHistory.Count > 24)
        {
            ExecutionHistory.RemoveAt(0);
        }

        while (DebugFrames.Count > 24)
        {
            DebugFrames.RemoveAt(0);
        }

        return honorBreakpoint && GraphDebugger.Raise(this, nodeId);
    }

    private static void ApplyVariable(GraphNode node, Entity? player)
    {
        var state = GameStateHost.Current ??= new GameState();
        var context = new ExpressionContext { State = state, Player = player };
        var value = ExpressionEvaluator.Evaluate(node.Expression, context);

        switch (node.Action)
        {
            case ActionKind.SetVariable:
                state.Set(node.VariableName, ExpressionEvaluator.Format(value));
                break;

            case ActionKind.AddVariable:
                state.SetNumber(node.VariableName, state.GetNumber(node.VariableName) + ExpressionEvaluator.ToNumber(value));
                break;

            case ActionKind.ToggleFlag:
                state.SetFlag(node.VariableName, !state.GetFlag(node.VariableName));
                break;
        }
    }

    public override Component CloneFor(Entity newOwner)
    {
        var copy = new NodeGraph { Owner = newOwner };
        var map = new Dictionary<Guid, Guid>();

        foreach (var node in Nodes)
        {
            var clone = node.Clone();
            map[node.Id] = clone.Id;
            copy.Nodes.Add(clone);
        }

        for (var i = 0; i < Nodes.Count; i++)
        {
            var source = Nodes[i];
            var clone = copy.Nodes[i];
            clone.Next = source.Next is { } next && map.TryGetValue(next, out var mapped) ? mapped : null;
            clone.TrueNext = source.TrueNext is { } t && map.TryGetValue(t, out var tm) ? tm : null;
            clone.FalseNext = source.FalseNext is { } f && map.TryGetValue(f, out var fm) ? fm : null;
        }

        return copy;
    }
}
