using Ollin.Core;
using Ollin.Core.Diagnostics;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Scripting;
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

    private readonly List<OllinScript> _scripts = new();
    private Scene? _scene;
    private ScriptWorkspace? _scriptsWorkspace;
    private string? _snapshot;

    /// <summary>Scripts activos en la reproducción actual.</summary>
    public int ScriptCount => _scripts.Count;

    public bool IsPlaying { get; private set; }

    public bool IsPaused { get; private set; }

    public float Elapsed { get; private set; }

    public int UpdateCount { get; private set; }

    /// <summary>Comienza la reproducción guardando una instantánea de la escena.</summary>
    public void Play(Scene scene, ScriptWorkspace? scripts = null)
    {
        _scene = scene;
        _scriptsWorkspace = scripts;
        _snapshot = SceneSerializer.Serialize(scene);
        IsPlaying = true;
        IsPaused = false;
        Elapsed = 0f;
        UpdateCount = 0;
        TimeState.Reset();
        InputState.Reset();
        StartScripts();
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

        StopScripts();

        var restored = SceneSerializer.Deserialize(_snapshot);
        var elapsed = Elapsed;

        IsPlaying = false;
        IsPaused = false;
        Elapsed = 0f;
        TimeState.Reset();
        InputState.Reset();
        _scene = null;
        _snapshot = null;
        _scriptsWorkspace = null;

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
        TimeState.Advance(dt);
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

        UpdateScripts(dt);

        // Cierra el cuadro de entrada: calcula flancos y delta del ratón.
        InputState.Update();
    }

    /// <summary>
    /// Recompila los scripts sin detener la reproducción: conserva las entidades y
    /// copia el estado de los campos compatibles a las nuevas instancias.
    /// </summary>
    public int HotReload(ScriptWorkspace workspace)
    {
        if (!IsPlaying || _scene is null)
        {
            return 0;
        }

        var snapshots = new List<(string ClassName, Entity Entity, Dictionary<string, object?> State)>();
        foreach (var script in _scripts)
        {
            var entity = script.Entity;
            if (entity is null)
            {
                continue;
            }

            var className = entity.GetComponent<ScriptComponent>()?.ClassName;
            if (string.IsNullOrWhiteSpace(className))
            {
                className = script.GetType().Name;
            }

            snapshots.Add((className!, entity, ScriptState.Capture(script)));
        }

        // Se sueltan las instancias del ensamblado anterior para poder descargarlo.
        _scripts.Clear();
        _scriptsWorkspace = workspace;

        var result = workspace.Reload();
        if (!result.Success)
        {
            Log.Error("La recarga en caliente falló: los scripts se detuvieron (ver consola)", "Runtime");
            return 0;
        }

        foreach (var snapshot in snapshots)
        {
            var instance = workspace.CreateInstance(snapshot.ClassName);
            if (instance is null)
            {
                continue;
            }

            instance.Attach(snapshot.Entity);
            var restored = ScriptState.Restore(instance, snapshot.State);

            try
            {
                instance.OnReload();
            }
            catch (Exception exception)
            {
                Log.Error($"Error en OnReload de {snapshot.ClassName}: {exception.Message}", "Runtime");
            }

            _scripts.Add(instance);
            Log.Info($"Script recargado: {snapshot.ClassName} en '{snapshot.Entity.Name}' ({restored} campos)", "Runtime");
        }

        Log.Info($"Recarga en caliente completada: {_scripts.Count} script(s) activos", "Runtime");
        return _scripts.Count;
    }
    private void StartScripts()
    {
        if (_scene is null || _scriptsWorkspace is null)
        {
            return;
        }

        foreach (var entity in _scene.Entities.ToArray())
        {
            var component = entity.GetComponent<ScriptComponent>();
            if (component is not { Enabled: true } || string.IsNullOrWhiteSpace(component.ClassName))
            {
                continue;
            }

            var script = _scriptsWorkspace.CreateInstance(component.ClassName);
            if (script is null)
            {
                continue;
            }

            script.Attach(entity);
            try
            {
                script.OnStart();
                _scripts.Add(script);
                Log.Info($"Script iniciado: {component.ClassName} en '{entity.Name}'", "Runtime");
            }
            catch (Exception exception)
            {
                Log.Error($"Error en OnStart de {component.ClassName}: {exception.Message}", "Runtime");
            }
        }
    }

    private void UpdateScripts(float deltaTime)
    {
        for (var i = _scripts.Count - 1; i >= 0; i--)
        {
            var script = _scripts[i];
            try
            {
                script.OnUpdate(deltaTime);
            }
            catch (Exception exception)
            {
                Log.Error($"Error en OnUpdate de {script.GetType().Name}: {exception.Message}", "Runtime");
                _scripts.RemoveAt(i);
            }
        }
    }

    private void StopScripts()
    {
        foreach (var script in _scripts)
        {
            try
            {
                script.OnDestroy();
            }
            catch (Exception exception)
            {
                Log.Error($"Error en OnDestroy de {script.GetType().Name}: {exception.Message}", "Runtime");
            }
        }

        _scripts.Clear();
    }
}
