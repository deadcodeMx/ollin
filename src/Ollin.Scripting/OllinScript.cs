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
