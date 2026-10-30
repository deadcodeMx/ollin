using System.Collections;
using Ollin.Core.Model;

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
