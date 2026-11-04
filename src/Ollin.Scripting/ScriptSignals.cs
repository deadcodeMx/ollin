using System.Collections.Concurrent;

namespace Ollin.Scripting;

/// <summary>Bus de señales simple para comunicación entre scripts.</summary>
public static class ScriptSignals
{
    private static readonly ConcurrentDictionary<string, List<Action<object?>>> Handlers = new();

    public static void Connect(string signal, Action<object?> handler)
    {
        var list = Handlers.GetOrAdd(signal, _ => new List<Action<object?>>());
        lock (list) list.Add(handler);
    }

    public static void Disconnect(string signal, Action<object?> handler)
    {
        if (!Handlers.TryGetValue(signal, out var list)) return;
        lock (list) list.Remove(handler);
    }

    public static void Emit(string signal, object? payload = null)
    {
        if (!Handlers.TryGetValue(signal, out var list)) return;
        Action<object?>[] snapshot;
        lock (list) snapshot = list.ToArray();
        foreach (var handler in snapshot) handler(payload);
    }

    public static void Clear() => Handlers.Clear();
}
