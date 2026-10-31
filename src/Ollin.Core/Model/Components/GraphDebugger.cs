namespace Ollin.Core.Model.Components;

/// <summary>Puente de depuración entre el grafo y el editor.</summary>
public static class GraphDebugger
{
    public static event Action<NodeGraph, Guid>? BreakpointHit;

    public static bool Raise(NodeGraph graph, Guid nodeId)
    {
        if (!graph.Breakpoints.Contains(nodeId))
        {
            return false;
        }

        BreakpointHit?.Invoke(graph, nodeId);
        return true;
    }
}