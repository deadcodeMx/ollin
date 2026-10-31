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
[JsonDerivedType(typeof(Components.SpriteAnimator), "animator")]
[JsonDerivedType(typeof(Components.Tilemap), "tilemap")]
[JsonDerivedType(typeof(Components.BoxCollider), "collider")]
[JsonDerivedType(typeof(Components.Rigidbody2D), "rigidbody")]
[JsonDerivedType(typeof(Components.AudioSource), "audio")]
[JsonDerivedType(typeof(Components.Interactable), "interactable")]
[JsonDerivedType(typeof(Components.Light2D), "light")]
[JsonDerivedType(typeof(Components.Inventory), "inventory")]
[JsonDerivedType(typeof(Components.Stats), "stats")]
[JsonDerivedType(typeof(Components.DayNightCycle), "daynight")]
[JsonDerivedType(typeof(Components.AiAgent), "ai")]
[JsonDerivedType(typeof(Components.Camera3D), "camera3d")]
[JsonDerivedType(typeof(Components.Wall3D), "wall3d")]
[JsonDerivedType(typeof(Components.FirstPersonController), "fps")]
public abstract class Component
{
    /// <summary>Entidad propietaria (no se serializa).</summary>
    [JsonIgnore]
    public Entity? Owner { get; internal set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Copia el componente para otra entidad (usado al duplicar).</summary>
    public abstract Component CloneFor(Entity newOwner);
}
