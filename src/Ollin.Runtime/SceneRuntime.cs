using Ollin.Core.Diagnostics;
using Ollin.Core.Model;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;

namespace Ollin.Runtime;

/// <summary>
/// Bucle de juego sobre una escena: reproduce, pausa y detiene. Al detenerse
/// restaura exactamente el estado que tenía la escena al pulsar Reproducir,
/// gracias a una instantánea serializada.
/// </summary>
public sealed class SceneRuntime
{
    private static readonly float MaxDeltaTime = 0.1f;

    private Scene? _scene;
    private string? _snapshot;

    public bool IsPlaying { get; private set; }

    public bool IsPaused { get; private set; }

    public float Elapsed { get; private set; }

    public int UpdateCount { get; private set; }

    /// <summary>Comienza la reproducción guardando una instantánea de la escena.</summary>
    public void Play(Scene scene)
    {
        _scene = scene;
        _snapshot = SceneSerializer.Serialize(scene);
        IsPlaying = true;
        IsPaused = false;
        Elapsed = 0f;
        UpdateCount = 0;
        Log.Info("Reproducción iniciada", "Runtime");
    }

    public void TogglePause()
    {
        if (!IsPlaying)
        {
            return;
        }

        IsPaused = !IsPaused;
        Log.Info(IsPaused ? "Reproducción en pausa" : "Reproducción reanudada", "Runtime");
    }

    /// <summary>
    /// Detiene la reproducción y devuelve la escena restaurada al estado previo.
    /// Devuelve null si no se estaba reproduciendo.
    /// </summary>
    public Scene? Stop()
    {
        if (!IsPlaying || _snapshot is null)
        {
            return null;
        }

        var restored = SceneSerializer.Deserialize(_snapshot);
        var elapsed = Elapsed;

        IsPlaying = false;
        IsPaused = false;
        Elapsed = 0f;
        _scene = null;
        _snapshot = null;

        Log.Info($"Reproducción detenida tras {elapsed:0.0} s", "Runtime");
        return restored;
    }

    /// <summary>Avanza la simulación. El delta se recorta para evitar saltos tras un parón.</summary>
    public void Tick(float deltaTime)
    {
        if (!IsPlaying || IsPaused || _scene is null)
        {
            return;
        }

        var dt = Math.Clamp(deltaTime, 0f, MaxDeltaTime);
        Elapsed += dt;
        UpdateCount++;

        // Copia de la lista: un componente podría añadir o quitar componentes.
        var entities = _scene.Entities.ToArray();
        foreach (var entity in entities)
        {
            if (!entity.Active)
            {
                continue;
            }

            foreach (var component in entity.Components.ToArray())
            {
                if (component is ITickable tickable && component.Enabled)
                {
                    try
                    {
                        tickable.OnUpdate(dt);
                    }
                    catch (Exception exception)
                    {
                        Log.Error($"Error en {component.GetType().Name} de '{entity.Name}': {exception.Message}", "Runtime");
                    }
                }
            }
        }
    }
}
