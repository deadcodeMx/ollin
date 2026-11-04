using System.Collections;
using System.Numerics;
using Ollin.Core;
using Ollin.Core.Diagnostics;
using Ollin.Core.Input;
using Ollin.Core.Audio;
using Ollin.Core.Dialogue;
using Ollin.Core.Hud;
using Ollin.Core.Interaction;
using Ollin.Core.Saves;
using GameAudio = Ollin.Core.Audio.Audio;
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
    private readonly List<CoroutineEntry> _coroutines = new();
    private readonly PhysicsSystem _physics = new();
    private readonly CombatSystem _combat = new();
    private readonly DialogueRunner _dialogue = new();
    private readonly HudModel _hud = new();
    private readonly GameState _gameState = new();
    private readonly List<(AudioSource Source, int Handle)> _spatialAudio = new();
    private bool _skipDialogueAdvance;
    private Scene? _scene;
    private ScriptWorkspace? _scriptsWorkspace;
    private string? _snapshot;

    /// <summary>Scripts activos en la reproducción actual.</summary>
    public int ScriptCount => _scripts.Count;

    public bool IsPlaying { get; private set; }

    /// <summary>Diálogos del proyecto (los carga el editor antes de reproducir).</summary>
    public DialogueDatabase? Dialogues { get; set; }

    /// <summary>Diálogo en curso, o inactivo.</summary>
    public DialogueRunner Dialogue => _dialogue;

    /// <summary>HUD de la partida (barras, etiquetas, mensajes y fin de partida).</summary>
    public HudModel Hud => _hud;

    /// <summary>Inventario del jugador (etiqueta "player") o del primer inventario activo.</summary>
    public Ollin.Core.Model.Components.Inventory? PlayerInventory
    {
        get
        {
            if (_scene is null) return null;
            var player = _scene.Entities.FirstOrDefault(entity => entity.Active &&
                entity.Tags.Any(tag => tag.Equals("player", StringComparison.OrdinalIgnoreCase)) &&
                entity.GetComponent<Ollin.Core.Model.Components.Inventory>() is not null);
            if (player is not null) return player.GetComponent<Ollin.Core.Model.Components.Inventory>();
            return _scene.Entities.FirstOrDefault(entity => entity.Active &&
                entity.GetComponent<Ollin.Core.Model.Components.Inventory>() is not null)
                ?.GetComponent<Ollin.Core.Model.Components.Inventory>();
        }
    }

    /// <summary>Datos globales que los scripts pueden guardar entre partidas.</summary>
    public GameState GameState => _gameState;

    public bool IsPaused { get; private set; }

    public float Elapsed { get; private set; }

    public int UpdateCount { get; private set; }

    /// <summary>Limpia las variables globales al cambiar de escena.</summary>
    public void ResetGameState()
    {
        _gameState.Clear();
        GameStateHost.Current = _gameState;
    }

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
        DebugDraw.Clear();
        _coroutines.Clear();
        _physics.Reset();
        CoroutineHost.Drain();
        _dialogue.Stop();
        _skipDialogueAdvance = false;
        InteractionState.Reset();
        _hud.Clear();
        GameStateHost.Current = _gameState;

        // Los manejadores deben existir antes de OnStart: los scripts ya usan Hud y Dialogue.
        HudHost.BarHandler = _hud.SetBar;
        HudHost.LabelHandler = _hud.SetLabel;
        HudHost.MessageHandler = _hud.ShowMessage;
        HudHost.GameOverHandler = _hud.GameOver;
        HudHost.ClearHandler = _hud.Clear;
        HudHost.TransitionHandler = _hud.ShowTransition;
        DialogueHost.PlayHandler = PlayDialogue;
        DialogueHost.IsActiveHandler = () => _dialogue.IsActive;
        DialogueHost.AdvanceHandler = _dialogue.Advance;

        StartScripts();
        StartAudio(scene);
        Log.Info("Reproducción iniciada", "Runtime");
    }

    /// <summary>Mueve las cámaras con FollowTag hacia su objetivo.</summary>
    private void UpdateCameraFollow(float dt)
    {
        if (_scene is null)
        {
            return;
        }

        foreach (var entity in _scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Camera2D>() is not { Enabled: true } camera ||
                string.IsNullOrWhiteSpace(camera.FollowTag))
            {
                continue;
            }

            Entity? target = null;
            foreach (var candidate in _scene.Entities)
            {
                if (candidate.Active &&
                    candidate.Tags.Any(tag => tag.Equals(camera.FollowTag, StringComparison.OrdinalIgnoreCase)))
                {
                    target = candidate;
                    break;
                }
            }

            if (target is null)
            {
                continue;
            }

            var targetPosition = _scene.GetWorldPosition(target);
            var current = _scene.GetWorldPosition(entity);
            var smoothing = MathF.Max(0.1f, camera.FollowSmoothing);
            var t = 1f - MathF.Exp(-smoothing * dt);
            var next = Vector2.Lerp(current, targetPosition, t);
            entity.Transform.Position += next - current;
        }
    }

    /// <summary>Aplica la luz ambiente del ciclo de día/noche a las cámaras.</summary>
    private void ApplyDayNight()
    {
        if (_scene is null)
        {
            return;
        }

        DayNightCycle? cycle = null;
        foreach (var entity in _scene.Entities)
        {
            if (entity.Active && entity.GetComponent<DayNightCycle>() is { Enabled: true } candidate)
            {
                cycle = candidate;
                break;
            }
        }

        if (cycle is null)
        {
            return;
        }

        var ambient = cycle.AmbientNow;
        foreach (var entity in _scene.Entities)
        {
            if (entity.Active && entity.GetComponent<Camera2D>() is { Enabled: true } camera)
            {
                camera.Ambient = ambient;
            }
        }
    }

    private bool PlayDialogue(string id)
    {
        var file = Dialogues?.Get(id);
        if (file is null)
        {
            Log.Warn($"No existe el diálogo '{id}' (carpeta dialogs)", "Diálogo");
            return false;
        }

        var started = _dialogue.Start(file);
        if (started)
        {
            _skipDialogueAdvance = true;
        }

        return started;
    }

    /// <summary>Interacción con E y avance de diálogos.</summary>
    private void UpdateInteractions()
    {
        if (_scene is null)
        {
            return;
        }

        if (_dialogue.IsActive)
        {
            InteractionState.Reset();

            if (_skipDialogueAdvance)
            {
                _skipDialogueAdvance = false;
                return;
            }

            if (InputState.WasKeyPressed(KeyCode.E) ||
                InputState.WasKeyPressed(KeyCode.Space) ||
                InputState.WasKeyPressed(KeyCode.Enter))
            {
                _dialogue.Advance();
            }

            return;
        }

        InteractionState.Reset();

        Entity? player = null;
        foreach (var entity in _scene.Entities)
        {
            if (entity.Active && entity.Tags.Any(tag => tag.Equals("player", StringComparison.OrdinalIgnoreCase)))
            {
                player = entity;
                break;
            }
        }

        if (player is null)
        {
            return;
        }

        Interactable? best = null;
        Entity? bestEntity = null;
        var bestDistance = float.MaxValue;

        foreach (var entity in _scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Interactable>() is not { Enabled: true } interactable)
            {
                continue;
            }

            if (interactable.Once && interactable.Used)
            {
                continue;
            }

            var distance = Vector2.Distance(entity.Transform.Position, player.Transform.Position);
            if (distance <= interactable.Radius && distance < bestDistance)
            {
                best = interactable;
                bestEntity = entity;
                bestDistance = distance;
            }
        }

        if (best is null || bestEntity is null)
        {
            return;
        }

        InteractionState.Set(best.Prompt, bestEntity.Id);

        if (!InputState.WasKeyPressed(KeyCode.E))
        {
            return;
        }

        if (best.Once)
        {
            best.Used = true;
        }

        NotifyInteract(bestEntity, player);

        if (!string.IsNullOrWhiteSpace(best.DialogueId))
        {
            PlayDialogue(best.DialogueId!);
        }
    }

    private void NotifyInteract(Entity target, Entity player)
    {
        foreach (var script in _scripts)
        {
            if (script.Entity != target)
            {
                continue;
            }

            try
            {
                script.OnInteract(player);
            }
            catch (Exception exception)
            {
                Log.Error($"Error en OnInteract de {script.GetType().Name}: {exception.Message}", "Runtime");
            }
        }
    }
    private void StartAudio(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active)
            {
                continue;
            }

            var source = entity.GetComponent<AudioSource>();
            if (source is not { Enabled: true, PlayOnStart: true } || string.IsNullOrWhiteSpace(source.Clip))
            {
                continue;
            }

            var handle = GameAudio.Play(
                source.Clip!,
                source.Volume,
                source.Loop,
                source.Music ? AudioChannel.Music : AudioChannel.Effect);

            if (handle >= 0 && source.FadeIn > 0f)
            {
                GameAudio.Fade(handle, source.Volume, source.FadeIn);
            }

            if (handle >= 0 && source.Spatial)
            {
                _spatialAudio.Add((source, handle));
            }
        }
    }

    /// <summary>Atenúa los sonidos posicionales según la cámara 3D.</summary>
    private void UpdateSpatialAudio()
    {
        if (_scene is null || _spatialAudio.Count == 0)
        {
            return;
        }

        var camera = _scene.GetMainCamera3D();

        if (camera is null)
        {
            return;
        }

        var eye = new Vector2(camera.Eye.X, camera.Eye.Z);

        foreach (var (source, handle) in _spatialAudio)
        {
            if (source.Owner is null || !source.Enabled)
            {
                continue;
            }

            var position = source.Owner.Transform.Position;
            var distance = Vector2.Distance(eye, position);
            var falloff = System.Math.Clamp(1f - distance / MathF.Max(0.01f, source.MaxDistance), 0f, 1f);
            GameAudio.SetVolume(handle, source.Volume * falloff * falloff);
        }
    }

    /// <summary>Ejecuta un solo nodo del grafo desde el breakpoint actual.</summary>
    public bool StepGraph(NodeGraph graph)
    {
        if (!IsPaused || _scene is null)
        {
            return false;
        }

        var player = _scene.Entities.FirstOrDefault(entity =>
            entity.Active && entity.Tags.Any(tag => tag.Equals("player", StringComparison.OrdinalIgnoreCase)));

        return graph.StepOnce(_scene, player);
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
        GameAudio.StopAll();
        _spatialAudio.Clear();

        var restored = SceneSerializer.Deserialize(_snapshot);
        var elapsed = Elapsed;

        IsPlaying = false;
        IsPaused = false;
        Elapsed = 0f;
        _coroutines.Clear();
        _physics.Reset();
        DebugDraw.Clear();
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
        _hud.Update(dt);

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
        DrainCoroutines();
        // Física en subpasos: evita que un cuerpo rápido atraviese un sólido
        // cuando el frame se alarga (pérdida de foco, recarga, etc.).
        var maxSpeed = _scene.Entities
            .Select(entity => entity.GetComponent<Rigidbody2D>()?.Velocity.Length() ?? 0f)
            .DefaultIfEmpty(0f).Max();
        var speedSteps = (int)MathF.Ceiling(maxSpeed * dt / 0.25f);
        var physicsSteps = System.Math.Clamp(System.Math.Max((int)MathF.Ceiling(dt / 0.016f), speedSteps), 1, 16);
        var physicsDelta = dt / physicsSteps;

        for (var step = 0; step < physicsSteps; step++)
        {
            _physics.Step(_scene, _scripts, physicsDelta);
        }
        UpdateCoroutines(dt);
        UpdateCameraFollow(dt);
        ApplyDayNight();
        UpdateSpatialAudio();
        UpdateInteractions();

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
        _coroutines.Clear();
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
    /// <summary>Recoge las rutinas lanzadas por los scripts en este cuadro.</summary>
    private void DrainCoroutines()
    {
        foreach (var (owner, routine) in CoroutineHost.Drain())
        {
            if (_scripts.Contains(owner))
            {
                _coroutines.Add(new CoroutineEntry { Owner = owner, Routine = routine });
            }
        }
    }

    /// <summary>Avanza las rutinas respetando <c>WaitForSeconds</c>.</summary>
    private void UpdateCoroutines(float deltaTime)
    {
        for (var i = _coroutines.Count - 1; i >= 0; i--)
        {
            var entry = _coroutines[i];

            if (entry.Wait > 0f)
            {
                entry.Wait -= deltaTime;
                if (entry.Wait > 0f)
                {
                    continue;
                }
            }

            bool moved;
            try
            {
                moved = entry.Routine.MoveNext();
            }
            catch (Exception exception)
            {
                Log.Error($"Error en una rutina de {entry.Owner.GetType().Name}: {exception.Message}", "Runtime");
                moved = false;
            }

            if (!moved || !_scripts.Contains(entry.Owner))
            {
                _coroutines.RemoveAt(i);
                continue;
            }

            entry.Wait = entry.Routine.Current is WaitForSeconds wait ? wait.Seconds : 0f;
        }
    }

    private sealed class CoroutineEntry
    {
        public required OllinScript Owner { get; init; }

        public required IEnumerator Routine { get; init; }

        public float Wait { get; set; }
    }
    private static void ApplyScriptValues(OllinScript script, ScriptComponent component)
    {
        if (component.Values is null || component.Values.Count == 0) return;
        var type = script.GetType();
        foreach (var field in type.GetFields(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!component.Values.TryGetValue(field.Name, out var text) || string.IsNullOrWhiteSpace(text)) continue;
            try
            {
                object? value = field.FieldType == typeof(float) ? float.Parse(text, System.Globalization.CultureInfo.InvariantCulture)
                    : field.FieldType == typeof(double) ? double.Parse(text, System.Globalization.CultureInfo.InvariantCulture)
                    : field.FieldType == typeof(int) ? int.Parse(text, System.Globalization.CultureInfo.InvariantCulture)
                    : field.FieldType == typeof(bool) ? bool.Parse(text)
                    : field.FieldType == typeof(string) ? text
                    : null;
                if (value is not null) field.SetValue(script, value);
            }
            catch { }
        }

        foreach (var property in type.GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance))
        {
            if (!property.CanWrite || !component.Values.TryGetValue(property.Name, out var text) || string.IsNullOrWhiteSpace(text)) continue;
            try
            {
                object? value = property.PropertyType == typeof(float) ? float.Parse(text, System.Globalization.CultureInfo.InvariantCulture)
                    : property.PropertyType == typeof(double) ? double.Parse(text, System.Globalization.CultureInfo.InvariantCulture)
                    : property.PropertyType == typeof(int) ? int.Parse(text, System.Globalization.CultureInfo.InvariantCulture)
                    : property.PropertyType == typeof(bool) ? bool.Parse(text)
                    : property.PropertyType == typeof(string) ? text
                    : null;
                if (value is not null) property.SetValue(script, value);
            }
            catch { }
        }
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
