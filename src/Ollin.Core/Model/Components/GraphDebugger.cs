namespace Ollin.Core.Model.Components;

/// <summary>Puente de depuración entre el grafo y el editor.</summary>
public static class GraphDebugger
{
    public static event Action<NodeGraph, Guid>? BreakpointHit;

    /// <summary>Pausa en cada nodo, aunque no haya breakpoint.</summary>
    public static bool StepMode { get; set; }

    public static Guid? RunToNodeId { get; set; }

    public static bool Raise(NodeGraph graph, Guid nodeId)
    {
        if (!StepMode && !graph.Breakpoints.Contains(nodeId) && RunToNodeId != nodeId)
        {
            return false;
        }

        if (RunToNodeId == nodeId) RunToNodeId = null;
        BreakpointHit?.Invoke(graph, nodeId);
        return true;
    }
}