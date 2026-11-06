using Xunit;

// HUD, Profiler y Audio son estáticos; la suite de rendimiento necesita medidas
// estables y no debe competir con otras pruebas en paralelo.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
