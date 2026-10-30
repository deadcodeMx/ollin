using System.Text.Json.Serialization;

namespace Ollin.Core.Model;

/// <summary>
/// Base de todos los componentes. La lista de tipos derivados se declara aquí para
/// que System.Text.Json pueda reconstruir la escena con el discriminador "$type".
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Components.SpriteRenderer), "sprite")]
[JsonDerivedType(typeof(Components.Camera2D), "camera")]
[JsonDerivedType(typeof(Components.ScriptComponent), "script")]
[JsonDerivedType(typeof(Components.RotatorComponent), "rotator")]
[JsonDerivedType(typeof(Components.OscillatorComponent), "oscillator")]
public abstract class Component
{
    /// <summary>Entidad propietaria (no se serializa).</summary>
    [JsonIgnore]
    public Entity? Owner { get; internal set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Copia el componente para otra entidad (usado al duplicar).</summary>
    public abstract Component CloneFor(Entity newOwner);
}
