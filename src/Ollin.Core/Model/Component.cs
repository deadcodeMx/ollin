using System.Text.Json.Serialization;

namespace Ollin.Core.Model;

/// <summary>
/// Base de todos los componentes. La lista de tipos derivados se declara aquí para
/// que System.Text.Json pueda reconstruir la escena con el discriminador "$type".
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(Components.SpriteRenderer), "sprite")]
[JsonDerivedType(typeof(Components.Camera2D), "camera")]
[JsonDerivedType(typeof(Components.SubViewport2D), "subviewport2d")]
[JsonDerivedType(typeof(Components.ScriptComponent), "script")]
[JsonDerivedType(typeof(Components.RotatorComponent), "rotator")]
[JsonDerivedType(typeof(Components.OscillatorComponent), "oscillator")]
[JsonDerivedType(typeof(Components.SpriteAnimator), "animator")]
[JsonDerivedType(typeof(Components.Tilemap), "tilemap")]
[JsonDerivedType(typeof(Components.TileMapLayer), "tilemaplayer")]
[JsonDerivedType(typeof(Components.CanvasMaterial2D), "canvasmaterial2d")]
[JsonDerivedType(typeof(Components.CanvasItem2D), "canvasitem2d")]
[JsonDerivedType(typeof(Components.CanvasLayer), "canvaslayer")]
[JsonDerivedType(typeof(Components.AnimationPlayer2D), "animationplayer")]
[JsonDerivedType(typeof(Components.AnimationBlendTree2D), "animationblendtree")]
[JsonDerivedType(typeof(Components.UiCanvas), "uicanvas")]
[JsonDerivedType(typeof(Components.NetworkIdentity), "networkidentity")]
[JsonDerivedType(typeof(Components.NetworkTransform), "networktransform")]
[JsonDerivedType(typeof(Components.Mesh2D), "mesh2d")]
[JsonDerivedType(typeof(Components.MultiMesh2D), "multimesh2d")]
[JsonDerivedType(typeof(Components.SkinnedMesh2D), "skinnedmesh2d")]
[JsonDerivedType(typeof(Components.Line2D), "line2d")]
[JsonDerivedType(typeof(Components.Polygon2D), "polygon2d")]
[JsonDerivedType(typeof(Components.NavigationPolygon2D), "navigationpolygon2d")]
[JsonDerivedType(typeof(Components.NavigationAgent2D), "navigationagent2d")]
[JsonDerivedType(typeof(Components.AudioSource2D), "audiosource2d")]
[JsonDerivedType(typeof(Components.AudioListener2D), "audiolistener2d")]
[JsonDerivedType(typeof(Components.RayCast2D), "raycast2d")]
[JsonDerivedType(typeof(Components.SpringJoint2D), "springjoint2d")]
[JsonDerivedType(typeof(Components.PinJoint2D), "pinjoint2d")]
[JsonDerivedType(typeof(Components.ShapeCast2D), "shapecast2d")]
[JsonDerivedType(typeof(Components.BoxCollider), "collider")]
[JsonDerivedType(typeof(Components.CircleCollider2D), "circlecollider")]
[JsonDerivedType(typeof(Components.CapsuleCollider2D), "capsulecollider")]
[JsonDerivedType(typeof(Components.CylinderCollider2D), "cylindercollider")]
[JsonDerivedType(typeof(Components.ConeCollider2D), "conecollider")]
[JsonDerivedType(typeof(Components.PolygonCollider2D), "polygoncollider")]
[JsonDerivedType(typeof(Components.Area2D), "area")]
[JsonDerivedType(typeof(Components.EdgeCollider2D), "edgecollider")]
[JsonDerivedType(typeof(Components.MovingPlatform2D), "movingplatform2d")]
[JsonDerivedType(typeof(Components.ParticleEmitter2D), "particles2d")]
[JsonDerivedType(typeof(Components.Trail2D), "trail2d")]
[JsonDerivedType(typeof(Components.AnimationStateMachine), "animationfsm")]
[JsonDerivedType(typeof(Components.ScreenTransition2D), "transition2d")]
[JsonDerivedType(typeof(Components.Skeleton2D), "skeleton2d")]
[JsonDerivedType(typeof(Components.Rigidbody2D), "rigidbody")]
[JsonDerivedType(typeof(Components.CharacterBody2D), "character")]
[JsonDerivedType(typeof(Components.AudioSource), "audio")]
[JsonDerivedType(typeof(Components.Light2D), "light")]
[JsonDerivedType(typeof(Components.DayNightCycle), "daynight")]
[JsonDerivedType(typeof(Components.AiAgent), "ai")]
[JsonDerivedType(typeof(Components.VisualScript), "visual")]
[JsonDerivedType(typeof(Components.NodeGraph), "nodegraph")]
[JsonDerivedType(typeof(Components.VariableHud), "variablehud")]
public abstract class Component
{
    /// <summary>Entidad propietaria (no se serializa).</summary>
    [JsonIgnore]
    public Entity? Owner { get; internal set; }

    public bool Enabled { get; set; } = true;

    /// <summary>Copia el componente para otra entidad (usado al duplicar).</summary>
    public abstract Component CloneFor(Entity newOwner);
}
