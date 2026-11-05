using Ollin.Core.Model;

namespace Ollin.Core.Scripting;

/// <summary>Señal global del juego. No depende de componentes concretos.</summary>
public sealed record GameSignal(string Name, Entity? Source);

/// <summary>Bus de señales para comunicación entre entidades y scripts.</summary>
public static class SignalBus
{
    private static readonly Dictionary<string, List<Action<GameSignal>>> Subscribers = new(StringComparer.OrdinalIgnoreCase);
    private static readonly object Sync = new();

    public static IDisposable Subscribe(string name, Action<GameSignal> handler)
    {
        if (string.IsNullOrWhiteSpace(name)) return new Subscription(null, handler);
        lock (Sync)
        {
            if (!Subscribers.TryGetValue(name, out var list)) Subscribers[name] = list = new List<Action<GameSignal>>();
            list.Add(handler);
        }
        return new Subscription(name, handler);
    }

    public static void Emit(string name, Entity? source = null)
    {
        if (string.IsNullOrWhiteSpace(name)) return;
        Action<GameSignal>[] handlers;
        lock (Sync) handlers = Subscribers.TryGetValue(name, out var list) ? list.ToArray() : Array.Empty<Action<GameSignal>>();
        foreach (var handler in handlers) handler(new GameSignal(name, source));
    }

    public static void Clear()
    {
        lock (Sync) Subscribers.Clear();
    }

    private sealed class Subscription(string? name, Action<GameSignal> handler) : IDisposable
    {
        public void Dispose()
        {
            if (name is null) return;
            lock (Sync)
            {
                if (Subscribers.TryGetValue(name, out var list)) list.Remove(handler);
            }
        }
    }
}
