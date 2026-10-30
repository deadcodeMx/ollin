using System.Collections;

namespace Ollin.Scripting;

/// <summary>Espera indicada por una rutina antes de continuar.</summary>
public sealed class WaitForSeconds
{
    public WaitForSeconds(float seconds) => Seconds = MathF.Max(0f, seconds);

    public float Seconds { get; }
}

/// <summary>Cola de rutinas que el runtime recoge en cada cuadro.</summary>
public static class CoroutineHost
{
    internal static readonly List<(OllinScript Owner, IEnumerator Routine)> Pending = new();

    /// <summary>El runtime vacía la cola; uso interno del motor.</summary>
    public static (OllinScript Owner, IEnumerator Routine)[] Drain()
    {
        var items = Pending.ToArray();
        Pending.Clear();
        return items;
    }
}