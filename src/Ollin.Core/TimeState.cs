namespace Ollin.Core;

/// <summary>
/// Reloj de la partida. El runtime lo avanza en cada cuadro y los scripts lo leen
/// a través de <c>Ollin.Scripting.Time</c>.
/// </summary>
public static class TimeState
{
    /// <summary>Segundos transcurridos desde el último cuadro.</summary>
    public static float DeltaTime { get; private set; }

    /// <summary>Segundos desde que empezó la reproducción.</summary>
    public static float Elapsed { get; private set; }

    /// <summary>Número de cuadros simulados.</summary>
    public static int Frame { get; private set; }

    /// <summary>Avanza el reloj. Lo llama el runtime.</summary>
    public static void Advance(float deltaTime)
    {
        DeltaTime = deltaTime;
        Elapsed += deltaTime;
        Frame++;
    }

    public static void Reset()
    {
        DeltaTime = 0f;
        Elapsed = 0f;
        Frame = 0;
    }
}
