using System.Collections;
using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Scripting;

/// <summary>
/// Clase base de los scripts de usuario. El runtime crea una instancia por entidad
/// con <c>ScriptComponent</c> y llama a <see cref="OnStart"/> una vez y a
/// <see cref="OnUpdate"/> en cada cuadro mientras la escena se reproduce.
/// </summary>
public abstract class OllinScript
{
    /// <summary>Entidad a la que pertenece el script.</summary>
    public Entity Entity { get; private set; } = null!;

    /// <summary>Escena en la que vive la entidad.</summary>
    public Ollin.Core.Scenes.Scene? Scene => Entity?.Scene;

    /// <summary>Vincula el script con su entidad (lo llama el runtime).</summary>
    public void Attach(Entity entity) => Entity = entity;

    internal List<IEnumerator> Coroutines { get; } = new();

    /// <summary>Obtiene un componente de la entidad del script.</summary>
    public T? Get<T>() where T : Component => Entity.GetComponent<T>();

    /// <summary>Obtiene un componente o falla con un mensaje claro.</summary>
    public T Require<T>() where T : Component =>
        Entity.GetComponent<T>() ?? throw new InvalidOperationException($"La entidad '{Entity.Name}' no tiene {typeof(T).Name}.");

    /// <summary>Añade un componente a la entidad.</summary>
    public T Add<T>() where T : Component, new()
    {
        var component = new T();
        Entity.AddComponent(component);
        return component;
    }

    /// <summary>Quita un componente de la entidad.</summary>
    public bool Remove<T>() where T : Component =>
        Entity.GetComponent<T>() is { } component && Entity.RemoveComponent(component);

    /// <summary>Busca una entidad por nombre en la escena.</summary>
    public Entity? Find(string name) => Scene?.Find(name);

    /// <summary>Crea una entidad en la escena actual.</summary>
    public Entity? Create(string name, Vector2 position, Guid? parentId = null) =>
        Scene?.Create(name, position, parentId);

    /// <summary>Elimina una entidad de la escena.</summary>
    public bool Destroy(Entity entity) => Scene?.Remove(entity.Id) ?? false;

    /// <summary>Posición local de la entidad.</summary>
    public Vector2 Position
    {
        get => Entity.Transform.Position;
        set => Entity.Transform.Position = value;
    }
    /// <summary>Posición en mundo (incluye jerarquía).</summary>
    public Vector2 WorldPosition
    {
        get => Scene?.GetWorldPosition(Entity) ?? Position;
        set { if (Scene is not null) Scene.SetWorldPosition(Entity, value); }
    }

    /// <summary>Entidad activa en la escena.</summary>
    public bool Active
    {
        get => Entity.Active;
        set => Entity.Active = value;
    }

    /// <summary>Hijos directos de la entidad.</summary>
    public IEnumerable<Entity> Children => Scene?.ChildrenOf(Entity.Id) ?? Enumerable.Empty<Entity>();

    /// <summary>Cambia el padre conservando la transformación local.</summary>
    public void SetParent(Entity? parent)
    {
        Entity.ParentId = parent?.Id;
        Scene?.Rebind();
    }

    /// <summary>Busca todas las entidades cuyo nombre contiene el texto.</summary>
    public IEnumerable<Entity> FindAll(string namePart) =>
        Scene?.Entities.Where(entity => entity.Name.Contains(namePart, StringComparison.OrdinalIgnoreCase))
        ?? Enumerable.Empty<Entity>();

    /// <summary>Reproduce un clip de AnimationPlayer2D.</summary>
    public bool PlayAnimation(string clip, float fadeSeconds = 0f)
    {
        if (Entity.GetComponent<AnimationPlayer2D>() is not { } player) return false;
        player.Play(clip, fadeSeconds);
        return true;
    }

    /// <summary>Reproduce un efecto de audio.</summary>
    public static void PlaySound(string clip, float volume = 1f, bool loop = false) =>
        Audio.Play(clip, volume, loop);

    /// <summary>Conecta una señal global.</summary>
    public void Connect(string signal, Action<object?> handler) => ScriptSignals.Connect(signal, handler);

    /// <summary>Emite una señal global.</summary>
    public void Emit(string signal, object? payload = null) => ScriptSignals.Emit(signal, payload);
    /// <summary>Canvas de UI de esta entidad.</summary>
    public UiCanvas? Canvas => Entity.GetComponent<UiCanvas>();

    /// <summary>Agente de navegación de esta entidad.</summary>
    public NavigationAgent2D? Agent => Entity.GetComponent<NavigationAgent2D>();

    /// <summary>Mueve la entidad hacia un destino usando NavigationAgent2D.</summary>
    public bool MoveTo(Vector2 destination, float speed = 3f)
    {
        var agent = Entity.GetComponent<NavigationAgent2D>() ?? Entity.AddComponent(new NavigationAgent2D());
        agent.Speed = speed;
        agent.SetTarget(destination);
        return true;
    }

    /// <summary>Detiene la navegación.</summary>
    public void StopMoving() => Agent?.SetTarget(WorldPosition);

    /// <summary>¿Queda camino por recorrer?</summary>
    public bool IsMoving => Agent is { Path.Count: > 1 };

    /// <summary>Cambia el estado de AnimationStateMachine.</summary>
    public bool PlayState(string state)
    {
        if (Entity.GetComponent<AnimationStateMachine>() is not { } fsm) return false;
        fsm.Play(state);
        return true;
    }

    /// <summary>Cambia el texto de un elemento de UI por id.</summary>
    public bool SetUiText(string id, string text)
    {
        if (Entity.GetComponent<UiCanvas>()?.Find(id) is not { } element) return false;
        element.Text = text;
        return true;
    }

    /// <summary>Muestra u oculta un elemento de UI por id.</summary>
    public bool SetUiVisible(string id, bool visible)
    {
        if (Entity.GetComponent<UiCanvas>()?.Find(id) is not { } element) return false;
        element.Visible = visible;
        return true;
    }

    /// <summary>Muestra un mensaje temporal en pantalla.</summary>
    public static void ShowMessage(string text, float seconds = 2f) => Hud.Message(text, seconds);

    /// <summary>Orienta la entidad hacia un punto del mundo.</summary>
    public void LookAt(Vector2 worldPoint)
    {
        var delta = worldPoint - Position;
        if (delta.LengthSquared() > 0.0001f) Entity.Transform.Rotation = MathF.Atan2(delta.Y, delta.X);
    }

    /// <summary>Distancia en mundo a otra entidad.</summary>
    public float DistanceTo(Entity other) => Scene?.WorldDistance(Entity, other) ?? 0f;

    /// <summary>Teletransporta la entidad en mundo.</summary>
    public void Teleport(Vector2 worldPoint) => WorldPosition = worldPoint;
    /// <summary>Instancia un prefab en la escena actual.</summary>
    public Entity? Instantiate(string prefabPath, Vector2 position)
    {
        try
        {
            var prefab = Ollin.Core.Prefabs.PrefabSerializer.Load(prefabPath);
            var entity = Ollin.Core.Prefabs.PrefabSerializer.Instantiate(prefab, position);
            Scene?.Add(entity);
            return entity;
        }
        catch (Exception exception)
        {
            Error($"No se pudo instanciar {prefabPath}: {exception.Message}");
            return null;
        }
    }

    /// <summary>Interpola la posición local durante un tiempo.</summary>
    public void TweenPosition(Vector2 target, float duration, Action? completed = null)
    {
        StartCoroutine(TweenRoutine(target, MathF.Max(0.01f, duration), completed));
    }

    private IEnumerator TweenRoutine(Vector2 target, float duration, Action? completed)
    {
        var start = Position;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.DeltaTime;
            Position = Vector2.Lerp(start, target, MathF.Min(1f, elapsed / duration));
            yield return null;
        }
        Position = target;
        completed?.Invoke();
    }

    /// <summary>Sacude la cámara principal durante un tiempo.</summary>
    public void ShakeCamera(float intensity, float duration)
    {
        var camera = Scene?.Entities.FirstOrDefault(entity => entity.GetComponent<Camera2D>() is { Enabled: true });
        if (camera is null) return;
        StartCoroutine(ShakeRoutine(camera, intensity, MathF.Max(0.01f, duration)));
    }

    private IEnumerator ShakeRoutine(Entity camera, float intensity, float duration)
    {
        var origin = camera.Transform.Position;
        var elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.DeltaTime;
            var falloff = 1f - elapsed / duration;
            camera.Transform.Position = origin + new Vector2(
                (Random.Shared.NextSingle() * 2f - 1f) * intensity * falloff,
                (Random.Shared.NextSingle() * 2f - 1f) * intensity * falloff);
            yield return null;
        }
        camera.Transform.Position = origin;
    }
    /// <summary>Lanza una rutina: <c>yield return new WaitForSeconds(1f)</c>.</summary>
    public void StartCoroutine(IEnumerator routine) => CoroutineHost.Pending.Add((this, routine));

    /// <summary>Cancela las rutinas pendientes de este script.</summary>
    public void StopAllCoroutines() => Coroutines.Clear();

    /// <summary>Se ejecuta una vez al comenzar la reproducción.</summary>
    public virtual void OnStart()
    {
    }

    /// <summary>Se ejecuta cada cuadro. <paramref name="deltaTime"/> en segundos.</summary>
    public virtual void OnUpdate(float deltaTime)
    {
    }

    /// <summary>Se ejecuta al detener la reproducción.</summary>
    public virtual void OnDestroy()
    {
    }

    /// <summary>
    /// Se ejecuta después de una recarga en caliente, cuando el estado de los campos
    /// ya se restauró. Úsalo para reenganchar referencias externas.
    /// </summary>
    public virtual void OnReload()
    {
    }

    /// <summary>Colisión sólida con otra entidad.</summary>
    public virtual void OnCollisionEnter(Entity other)
    {
    }

    /// <summary>Entrada en una zona de disparo.</summary>
    public virtual void OnTriggerEnter(Entity other)
    {
    }

    /// <summary>Salida de una zona de disparo.</summary>
    /// <summary>Se llama al interactuar con esta entidad (tecla E estando cerca).</summary>
    public virtual void OnInteract(Entity other)
    {
    }

    public virtual void OnTriggerExit(Entity other)
    {
    }

    /// <summary>Mensaje informativo en la consola del editor.</summary>
    protected static void Debug(string message) => Core.Diagnostics.Log.Info(message, "Script");

    /// <summary>Aviso en la consola del editor.</summary>
    protected static void Warn(string message) => Core.Diagnostics.Log.Warn(message, "Script");

    /// <summary>Error en la consola del editor.</summary>
    protected static void Error(string message) => Core.Diagnostics.Log.Error(message, "Script");
}
