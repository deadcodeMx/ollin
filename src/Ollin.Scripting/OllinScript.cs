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

    /// <summary>Mensaje informativo en la consola del editor.</summary>
    protected static void Debug(string message) => Core.Diagnostics.Log.Info(message, "Script");

    /// <summary>Aviso en la consola del editor.</summary>
    protected static void Warn(string message) => Core.Diagnostics.Log.Warn(message, "Script");

    /// <summary>Error en la consola del editor.</summary>
    protected static void Error(string message) => Core.Diagnostics.Log.Error(message, "Script");
}
