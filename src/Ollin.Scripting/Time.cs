using Ollin.Core;

namespace Ollin.Scripting;

/// <summary>Reloj de la partida para los scripts.</summary>
public static class Time
{
    /// <summary>Segundos transcurridos desde el cuadro anterior.</summary>
    public static float DeltaTime => TimeState.DeltaTime;

    /// <summary>Segundos desde que empezó la reproducción.</summary>
    public static float Elapsed => TimeState.Elapsed;

    /// <summary>Número de cuadros simulados desde el inicio.</summary>
    public static int Frame => TimeState.Frame;
}
