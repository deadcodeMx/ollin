namespace Ollin.Core.Model;

/// <summary>
/// Componente que participa en el bucle de juego. El runtime llama a
/// <see cref="OnUpdate"/> una vez por cuadro mientras la escena se reproduce.
/// </summary>
public interface ITickable
{
    void OnUpdate(float deltaTime);
}
