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

    /// <summary>Mensaje de depuración que aparece en la consola del editor.</summary>
    protected static void Debug(string message) => Core.Diagnostics.Log.Info(message, "Script");
}
