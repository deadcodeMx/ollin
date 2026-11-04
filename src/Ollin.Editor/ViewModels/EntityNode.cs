using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.ViewModels;

public sealed record ScriptFieldDefinition(string Name, string TypeName, string DefaultValue);

public sealed class ScriptField
{
    private readonly Action<string> _set;
    private string _value;

    public ScriptField(string name, string typeName, string value, Action<string> set)
    {
        Name = name; TypeName = typeName; _value = value; _set = set;
    }

    public string Name { get; }
    public string TypeName { get; }
    public string Value
    {
        get => _value;
        set
        {
            if (_value == value) return;
            _value = value ?? string.Empty;
            _set(_value);
        }
    }
}

/// <summary>
/// Nodo del árbol de escena. Envuelve una <see cref="Entity"/> y expone las
/// propiedades que edita el inspector (con conversión a unidades amigables).
/// </summary>
public sealed record TilePaletteItem(int Index, IImage Preview);

public sealed class ScriptComponentEditor
{
    private readonly EntityNode _owner;
    private IReadOnlyList<ScriptField>? _fields;

    public ScriptComponentEditor(EntityNode owner, ScriptComponent component, Action remove, Action open)
    {
        _owner = owner;
        Component = component;
        Remove = remove;
        Open = open;
    }

    public ScriptComponent Component { get; }

    public Action Remove { get; }

    public Action Open { get; }

    public string Title => string.IsNullOrWhiteSpace(Component.ClassName) ? "Script" : Component.ClassName;

    public string ScriptPath
    {
        get => Component.ScriptPath ?? string.Empty;
        set
        {
            if (Component.ScriptPath == value) return;
            Component.ScriptPath = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            _owner.NotifyEdit();
        }
    }

    public string ClassName
    {
        get => Component.ClassName;
        set
        {
            if (Component.ClassName == value) return;
            Component.ClassName = value ?? string.Empty;
            _fields = null;
            _owner.NotifyEdit();
        }
    }

    public IReadOnlyList<ScriptField> Fields
    {
        get
        {
            if (_fields is not null) return _fields;
            _fields = _owner.GetScriptFields(ClassName)
                .Select(definition => new ScriptField(
                    definition.Name,
                    definition.TypeName,
                    Component.Values?.TryGetValue(definition.Name, out var value) == true ? value : definition.DefaultValue,
                    newValue =>
                    {
                        Component.Values ??= new Dictionary<string, string>();
                        Component.Values[definition.Name] = newValue;
                        _owner.NotifyEdit();
                    }))
                .ToList();
            return _fields;
        }
    }
}
public sealed class EntityNode : ObservableObject
{
    private readonly MainViewModel _owner;
    private IImage? _texturePreview;

    public EntityNode(Entity entity, MainViewModel owner)
    {
        Entity = entity;
        _owner = owner;
    }

    public Entity Entity { get; }

    public ObservableCollection<EntityNode> Children { get; } = new();

    public Guid Id => Entity.Id;

    public bool IsExpanded { get; set; } = true;

    private static readonly IBrush NeutralBrush = new SolidColorBrush(Color.Parse("#C7D0DE"));

    public string Icon =>
        !string.IsNullOrWhiteSpace(Entity.EditorIcon)
            ? Entity.EditorIcon!
            : Entity.GetComponent<Camera2D>() is not null ? "◉"
            : Entity.GetComponent<SpriteRenderer>() is not null ? "▣"
            : "◇";

    public IBrush NameBrush =>
        string.IsNullOrWhiteSpace(Entity.EditorColor)
            ? NeutralBrush
            : new SolidColorBrush(Color.Parse(Entity.EditorColor));

    public string EditorIcon
    {
        get => Entity.EditorIcon ?? string.Empty;
        set
        {
            if ((Entity.EditorIcon ?? string.Empty) == value)
            {
                return;
            }

            Entity.EditorIcon = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            OnPropertyChanged(nameof(Icon));
            _owner.MarkDirty();
        }
    }

    public string EditorColorHex
    {
        get => Entity.EditorColor ?? string.Empty;
        set
        {
            if ((Entity.EditorColor ?? string.Empty) == value)
            {
                return;
            }

            Entity.EditorColor = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            OnPropertyChanged(nameof(NameBrush));
            _owner.MarkDirty();
        }
    }

    public string Name
    {
        get => Entity.Name;
        set
        {
            if (Entity.Name == value)
            {
                return;
            }

            Entity.Name = string.IsNullOrWhiteSpace(value) ? "Entidad" : value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(DisplayName));
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string DisplayName => $"{Entity.Name}";

    public decimal X
    {
        get => (decimal)Entity.Transform.Position.X;
        set
        {
            if (Nearly(Entity.Transform.Position.X, (float)value))
            {
                return;
            }

            SetPosition((float)value, Entity.Transform.Position.Y);
        }
    }

    public decimal Y
    {
        get => (decimal)Entity.Transform.Position.Y;
        set
        {
            if (Nearly(Entity.Transform.Position.Y, (float)value))
            {
                return;
            }

            SetPosition(Entity.Transform.Position.X, (float)value);
        }
    }

    public decimal RotationDegrees
    {
        get => (decimal)(Entity.Transform.Rotation * 180.0 / Math.PI);
        set
        {
            if (Nearly(Entity.Transform.Rotation, (float)((double)value * Math.PI / 180.0)))
            {
                return;
            }

            Entity.Transform.Rotation = (float)((double)value * Math.PI / 180.0);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal ScaleX
    {
        get => (decimal)Entity.Transform.Scale.X;
        set
        {
            if (Nearly(Entity.Transform.Scale.X, (float)value))
            {
                return;
            }

            Entity.Transform.Scale = new Vector2((float)value, Entity.Transform.Scale.Y);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal ScaleY
    {
        get => (decimal)Entity.Transform.Scale.Y;
        set
        {
            if (Nearly(Entity.Transform.Scale.Y, (float)value))
            {
                return;
            }

            Entity.Transform.Scale = new Vector2(Entity.Transform.Scale.X, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool HasSprite => Entity.GetComponent<SpriteRenderer>() is not null;

    public bool HasCamera => Entity.GetComponent<Camera2D>() is not null;

    public bool HasScript => Entity.GetComponent<ScriptComponent>() is not null;

    private ScriptComponent? _activeScript;

    public IReadOnlyList<ScriptComponent> ScriptComponents => Entity.Components.OfType<ScriptComponent>().ToList();

    public ScriptComponent? ActiveScript
    {
        get => _activeScript ??= Entity.Components.OfType<ScriptComponent>().FirstOrDefault();
        set
        {
            if (ReferenceEquals(_activeScript, value)) return;
            _activeScript = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(ScriptPath));
            OnPropertyChanged(nameof(ScriptClass));
        }
    }

    public int ScriptCount => ScriptComponents.Count;

    private IReadOnlyList<ScriptField>? _scriptFields;

    public IReadOnlyList<ScriptField> ScriptFields
    {
        get
        {
            if (_scriptFields is not null) return _scriptFields;
            var component = Script;
            _scriptFields = _owner.GetScriptFields(component?.ClassName ?? string.Empty)
                .Select(definition => new ScriptField(
                    definition.Name,
                    definition.TypeName,
                    component?.Values?.TryGetValue(definition.Name, out var value) == true ? value : definition.DefaultValue,
                    newValue =>
                    {
                        if (component is null) return;
                        component.Values[definition.Name] = newValue;
                        _owner.MarkDirty();
                    }))
                .ToList();
            return _scriptFields;
        }
    }

    public IReadOnlyList<ScriptFieldDefinition> GetScriptFields(string className) => _owner.GetScriptFields(className);

    public void NotifyEdit() => _owner.MarkDirty();

    public void OpenScriptFor(ScriptComponent component) => _owner.OpenScript(Path.Combine(_owner.ProjectRoot, component.ScriptPath ?? string.Empty));

    private IReadOnlyList<ScriptComponentEditor>? _scriptEditors;

    public IReadOnlyList<ScriptComponentEditor> ScriptEditors
    {
        get
        {
            if (_scriptEditors is not null) return _scriptEditors;
            _scriptEditors = ScriptComponents.Select(component => new ScriptComponentEditor(
                this,
                component,
                () =>
                {
                    Entity.RemoveComponent(component);
                    RefreshAll();
                    _owner.NotifyComponentsChanged();
                },
                () => _owner.OpenScript(Path.Combine(_owner.ProjectRoot, component.ScriptPath ?? string.Empty)))).ToList();
            return _scriptEditors;
        }
    }

    public bool HasCharacterBody => Entity.GetComponent<CharacterBody2D>() is not null;

    public CharacterBody2D? CharacterBody => Entity.GetComponent<CharacterBody2D>();

    public decimal CharacterMoveSpeed
    {
        get => (decimal)(CharacterBody?.MoveSpeed ?? 6f);
        set
        {
            if (CharacterBody is null || MathF.Abs(CharacterBody.MoveSpeed - (float)value) < 0.001f) return;
            CharacterBody.MoveSpeed = MathF.Max(0f, (float)value);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal CharacterJumpSpeed
    {
        get => (decimal)(CharacterBody?.JumpSpeed ?? 12f);
        set
        {
            if (CharacterBody is null || MathF.Abs(CharacterBody.JumpSpeed - (float)value) < 0.001f) return;
            CharacterBody.JumpSpeed = MathF.Max(0f, (float)value);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal CharacterGravity
    {
        get => (decimal)(CharacterBody?.Gravity ?? 30f);
        set
        {
            if (CharacterBody is null || MathF.Abs(CharacterBody.Gravity - (float)value) < 0.001f) return;
            CharacterBody.Gravity = MathF.Max(0f, (float)value);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal CharacterMaxSlopeAngle
    {
        get => (decimal)(CharacterBody?.MaxSlopeAngle ?? 50f);
        set { if (CharacterBody is null || MathF.Abs(CharacterBody.MaxSlopeAngle - (float)value) < 0.001f) return; CharacterBody.MaxSlopeAngle = System.Math.Clamp((float)value, 0f, 89f); OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public decimal CharacterFloorSnapLength
    {
        get => (decimal)(CharacterBody?.FloorSnapLength ?? 0.18f);
        set { if (CharacterBody is null || MathF.Abs(CharacterBody.FloorSnapLength - (float)value) < 0.001f) return; CharacterBody.FloorSnapLength = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public decimal CharacterAcceleration
    {
        get => (decimal)(CharacterBody?.Acceleration ?? 60f);
        set
        {
            if (CharacterBody is null || MathF.Abs(CharacterBody.Acceleration - (float)value) < 0.001f) return;
            CharacterBody.Acceleration = MathF.Max(0.01f, (float)value);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }


    public bool HasSkeleton => Entity.GetComponent<Skeleton2D>() is not null;
    public bool SkeletonUseIK { get => Entity.GetComponent<Skeleton2D>()?.UseIK ?? false; set { if (Entity.GetComponent<Skeleton2D>() is { } s) { s.UseIK = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public decimal SkeletonIKX { get => (decimal)(Entity.GetComponent<Skeleton2D>()?.IKTarget.X ?? 0f); set { if (Entity.GetComponent<Skeleton2D>() is { } s) { s.IKTarget = new Vector2((float)value, s.IKTarget.Y); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal SkeletonIKY { get => (decimal)(Entity.GetComponent<Skeleton2D>()?.IKTarget.Y ?? 0f); set { if (Entity.GetComponent<Skeleton2D>() is { } s) { s.IKTarget = new Vector2(s.IKTarget.X, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal SkeletonBend { get => Entity.GetComponent<Skeleton2D>()?.BendDirection ?? 1; set { if (Entity.GetComponent<Skeleton2D>() is { } s) { s.BendDirection = value < 0 ? -1 : 1; OnPropertyChanged(); _owner.MarkDirty(); } } }
    public string SkeletonInfo => Entity.GetComponent<Skeleton2D>() is { } s ? $"{s.Bones.Count} huesos" : "-";


    public decimal ColliderBounciness { get => (decimal)(Collider?.Bounciness ?? 0f); set { if (Collider is null || MathF.Abs(Collider.Bounciness - (float)value) < 0.001f) return; Collider.Bounciness = System.Math.Clamp((float)value, 0f, 1f); OnPropertyChanged(); _owner.MarkDirty(); } }
    public decimal ColliderFriction { get => (decimal)(Collider?.Friction ?? 0.2f); set { if (Collider is null || MathF.Abs(Collider.Friction - (float)value) < 0.001f) return; Collider.Friction = System.Math.Clamp((float)value, 0f, 1f); OnPropertyChanged(); _owner.MarkDirty(); } }
    public bool RigidbodyContinuous { get => Entity.GetComponent<Rigidbody2D>()?.ContinuousCollision ?? true; set { if (Entity.GetComponent<Rigidbody2D>() is { } b) { b.ContinuousCollision = value; OnPropertyChanged(); _owner.MarkDirty(); } } }

    public string ColliderIgnoreTags { get => string.Join(", ", Collider?.IgnoreTags ?? new List<string>()); set { if (Collider is null) return; Collider.IgnoreTags = value.Split(',', StringSplitOptions.RemoveEmptyEntries).Select(tag => tag.Trim()).Where(tag => tag.Length > 0).ToList(); OnPropertyChanged(); _owner.MarkDirty(); } }

    public bool HasNetworkIdentity => Entity.GetComponent<NetworkIdentity>() is not null;
    public bool HasNetworkTransform => Entity.GetComponent<NetworkTransform>() is not null;
    public decimal NetworkInterpolation { get => (decimal)(Entity.GetComponent<NetworkTransform>()?.Interpolation ?? 12f); set { if (Entity.GetComponent<NetworkTransform>() is { } n) { n.Interpolation = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public bool NetworkIsLocal { get => Entity.GetComponent<NetworkIdentity>()?.IsLocal ?? true; set { if (Entity.GetComponent<NetworkIdentity>() is { } n) { n.IsLocal = value; OnPropertyChanged(); _owner.MarkDirty(); } } }

    public bool HasMesh2D => Entity.GetComponent<Mesh2D>() is not null;
    public bool HasMultiMesh2D => Entity.GetComponent<MultiMesh2D>() is not null;

    public bool HasSkinnedMesh => Entity.GetComponent<SkinnedMesh2D>() is not null;
    public bool HasLine2D => Entity.GetComponent<Line2D>() is not null;
    public bool HasPolygon2D => Entity.GetComponent<Polygon2D>() is not null;

    public bool HasNavigationPolygon => Entity.GetComponent<NavigationPolygon2D>() is not null;
    public bool HasNavigationAgent => Entity.GetComponent<NavigationAgent2D>() is not null;
    public decimal NavigationSpeed { get => (decimal)(Entity.GetComponent<NavigationAgent2D>()?.Speed ?? 3f); set { if (Entity.GetComponent<NavigationAgent2D>() is { } a) { a.Speed = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal NavigationAvoidance { get => (decimal)(Entity.GetComponent<NavigationAgent2D>()?.AvoidanceRadius ?? 1f); set { if (Entity.GetComponent<NavigationAgent2D>() is { } a) { a.AvoidanceRadius = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal NavigationSeparation { get => (decimal)(Entity.GetComponent<NavigationAgent2D>()?.Separation ?? 1.2f); set { if (Entity.GetComponent<NavigationAgent2D>() is { } a) { a.Separation = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal NavigationTargetX { get => (decimal)(Entity.GetComponent<NavigationAgent2D>()?.Target.X ?? 0f); set { if (Entity.GetComponent<NavigationAgent2D>() is { } a) a.Target = new Vector2((float)value, a.Target.Y); OnPropertyChanged(); _owner.MarkDirty(); } }
    public decimal NavigationTargetY { get => (decimal)(Entity.GetComponent<NavigationAgent2D>()?.Target.Y ?? 0f); set { if (Entity.GetComponent<NavigationAgent2D>() is { } a) a.Target = new Vector2(a.Target.X, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } }

    public bool HasAudioSource => Entity.GetComponent<AudioSource2D>() is not null;
    public bool HasAudioListener => Entity.GetComponent<AudioListener2D>() is not null;
    public string SpatialAudioClip { get => Entity.GetComponent<AudioSource2D>()?.Clip ?? string.Empty; set { if (Entity.GetComponent<AudioSource2D>() is { } a) { a.Clip = value ?? string.Empty; OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal SpatialAudioVolume { get => (decimal)(Entity.GetComponent<AudioSource2D>()?.Volume ?? 1f); set { if (Entity.GetComponent<AudioSource2D>() is { } a) { a.Volume = System.Math.Clamp((float)value, 0f, 1f); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal SpatialAudioMaxDistance { get => (decimal)(Entity.GetComponent<AudioSource2D>()?.MaxDistance ?? 12f); set { if (Entity.GetComponent<AudioSource2D>() is { } a) { a.MaxDistance = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }

    public bool HasUiCanvas => Entity.GetComponent<UiCanvas>() is not null;
    public string UiCanvasSummary => Entity.GetComponent<UiCanvas>() is { } canvas ? $"{canvas.Elements.Count} elementos" : "-";

    public bool HasSpringJoint => Entity.GetComponent<SpringJoint2D>() is not null;
    public bool HasPinJoint => Entity.GetComponent<PinJoint2D>() is not null;
    public string JointTargetTag { get => Entity.GetComponent<Joint2D>()?.TargetTag ?? string.Empty; set { if (Entity.GetComponent<Joint2D>() is { } j) { j.TargetTag = value ?? string.Empty; OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal JointRestLength { get => (decimal)((Entity.GetComponent<SpringJoint2D>()?.RestLength) ?? (Entity.GetComponent<PinJoint2D>()?.Distance ?? 1f)); set { if (Entity.GetComponent<SpringJoint2D>() is { } s) s.RestLength = MathF.Max(0.01f, (float)value); else if (Entity.GetComponent<PinJoint2D>() is { } p) p.Distance = MathF.Max(0.01f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } }
    public decimal JointStiffness { get => (decimal)(Entity.GetComponent<SpringJoint2D>()?.Stiffness ?? 20f); set { if (Entity.GetComponent<SpringJoint2D>() is { } s) { s.Stiffness = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }

    public bool HasAnimationBlendTree => Entity.GetComponent<AnimationBlendTree2D>() is not null;
    public decimal AnimationBlendTime { get => (decimal)(Entity.GetComponent<AnimationBlendTree2D>()?.Time ?? 0f); set { if (Entity.GetComponent<AnimationBlendTree2D>() is { } b) { b.Duration = MathF.Max(0.01f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }

    public bool HasAnimationPlayer => Entity.GetComponent<AnimationPlayer2D>() is not null;
    public string AnimationPlayerClip { get => Entity.GetComponent<AnimationPlayer2D>()?.Current ?? "-"; }
    public bool AnimationPlayerPlaying { get => Entity.GetComponent<AnimationPlayer2D>()?.Playing ?? false; }
    public decimal AnimationPlayerSpeed { get => (decimal)(Entity.GetComponent<AnimationPlayer2D>()?.Speed ?? 1f); set { if (Entity.GetComponent<AnimationPlayer2D>() is { } a) { a.Speed = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }

    public bool HasCanvasMaterial2D => Entity.GetComponent<CanvasMaterial2D>() is not null;
    public string CanvasMaterialParams { get => string.Join("; ", Entity.GetComponent<CanvasMaterial2D>()?.Parameters.Select(pair => $"{pair.Key}={pair.Value.X},{pair.Value.Y},{pair.Value.Z},{pair.Value.W}") ?? Array.Empty<string>()); set { if (Entity.GetComponent<CanvasMaterial2D>() is not { } m) return; m.Parameters.Clear(); foreach (var part in (value ?? string.Empty).Split(';', StringSplitOptions.RemoveEmptyEntries)) { var pieces = part.Split('=', 2); if (pieces.Length != 2) continue; var numbers = pieces[1].Split(','); if (numbers.Length < 4 || !float.TryParse(numbers[0], out var x) || !float.TryParse(numbers[1], out var y) || !float.TryParse(numbers[2], out var z) || !float.TryParse(numbers[3], out var w)) continue; m.Parameters[pieces[0].Trim()] = new Vector4(x, y, z, w); } OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } }
    public string CanvasMaterialShader { get => Entity.GetComponent<CanvasMaterial2D>()?.FragmentSource ?? string.Empty; set { if (Entity.GetComponent<CanvasMaterial2D>() is { } m) { m.FragmentSource = value ?? string.Empty; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }

    public bool HasSubViewport2D => Entity.GetComponent<SubViewport2D>() is not null;
    public string SubViewportCamera { get => Entity.GetComponent<SubViewport2D>()?.CameraTag ?? "main"; set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.CameraTag = value ?? string.Empty; OnPropertyChanged(); _owner.MarkDirty(); } } }
    public bool SubViewportDebugBorder { get => Entity.GetComponent<SubViewport2D>()?.DebugBorder ?? true; set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.DebugBorder = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public bool SubViewportUseTexture { get => Entity.GetComponent<SubViewport2D>()?.UseTexture ?? true; set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.UseTexture = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public decimal SubViewportX { get => (decimal)(Entity.GetComponent<SubViewport2D>()?.Rect.X ?? 0.65f); set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.Rect = new Vector4((float)value, v.Rect.Y, v.Rect.Z, v.Rect.W); OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public decimal SubViewportY { get => (decimal)(Entity.GetComponent<SubViewport2D>()?.Rect.Y ?? 0.65f); set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.Rect = new Vector4(v.Rect.X, (float)value, v.Rect.Z, v.Rect.W); OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public decimal SubViewportW { get => (decimal)(Entity.GetComponent<SubViewport2D>()?.Rect.Z ?? 0.3f); set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.Rect = new Vector4(v.Rect.X, v.Rect.Y, Math.Clamp((float)value, 0.05f, 1f), v.Rect.W); OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public decimal SubViewportH { get => (decimal)(Entity.GetComponent<SubViewport2D>()?.Rect.W ?? 0.3f); set { if (Entity.GetComponent<SubViewport2D>() is { } v) { v.Rect = new Vector4(v.Rect.X, v.Rect.Y, v.Rect.Z, Math.Clamp((float)value, 0.05f, 1f)); OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }

    public bool HasCanvasItem2D => Entity.GetComponent<CanvasItem2D>() is not null;
    public bool CanvasItemVisible { get => Entity.GetComponent<CanvasItem2D>()?.Visible ?? true; set { if (Entity.GetComponent<CanvasItem2D>() is { } c) { c.Visible = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public bool CanvasItemYSort { get => Entity.GetComponent<CanvasItem2D>()?.YSort ?? false; set { if (Entity.GetComponent<CanvasItem2D>() is { } c) { c.YSort = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public decimal CanvasItemZ { get => Entity.GetComponent<CanvasItem2D>()?.ZIndex ?? 0; set { if (Entity.GetComponent<CanvasItem2D>() is { } c) { c.ZIndex = (int)value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }

    public bool HasCanvasLayer => Entity.GetComponent<CanvasLayer>() is not null;
    public decimal CanvasOrder { get => Entity.GetComponent<CanvasLayer>()?.Order ?? 0; set { if (Entity.GetComponent<CanvasLayer>() is { } l) { l.Order = (int)value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public bool CanvasYSort { get => Entity.GetComponent<CanvasLayer>()?.YSort ?? false; set { if (Entity.GetComponent<CanvasLayer>() is { } l) { l.YSort = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }
    public bool CanvasVisible { get => Entity.GetComponent<CanvasLayer>()?.Visible ?? true; set { if (Entity.GetComponent<CanvasLayer>() is { } l) { l.Visible = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } } }

    public bool HasAnimationFSM => Entity.GetComponent<AnimationStateMachine>() is not null;
    public string AnimationDefaultState { get => Entity.GetComponent<AnimationStateMachine>()?.DefaultState ?? "idle"; set { if (Entity.GetComponent<AnimationStateMachine>() is { } f) { f.DefaultState = value ?? "idle"; OnPropertyChanged(); _owner.MarkDirty(); } } }
    public string AnimationCurrentState => Entity.GetComponent<AnimationStateMachine>()?.CurrentState ?? "-";
    public bool HasTransition2D => Entity.GetComponent<ScreenTransition2D>() is not null;
    public decimal TransitionDuration { get => (decimal)(Entity.GetComponent<ScreenTransition2D>()?.Duration ?? 0.6f); set { if (Entity.GetComponent<ScreenTransition2D>() is { } t) { t.Duration = MathF.Max(0.05f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal TransitionDelay { get => (decimal)(Entity.GetComponent<ScreenTransition2D>()?.Delay ?? 0f); set { if (Entity.GetComponent<ScreenTransition2D>() is { } t) { t.Delay = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public bool TilemapAutotile { get => Tilemap?.Autotile ?? false; set { if (Tilemap is null || Tilemap.Autotile == value) return; Tilemap.Autotile = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } }
    public bool TilemapYSort { get => Tilemap?.YSort ?? false; set { if (Tilemap is null || Tilemap.YSort == value) return; Tilemap.YSort = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } }
    public decimal TilemapAutotileBase { get => Tilemap?.AutotileBase ?? 0; set { if (Tilemap is null || Tilemap.AutotileBase == (int)value) return; Tilemap.AutotileBase = (int)value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } }

    public bool HasMovingPlatform => Entity.GetComponent<MovingPlatform2D>() is not null;
    public decimal MovingTravelX { get => (decimal)(Entity.GetComponent<MovingPlatform2D>()?.Travel.X ?? 3f); set { if (Entity.GetComponent<MovingPlatform2D>() is { } m) { m.Travel = new Vector2((float)value, m.Travel.Y); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal MovingTravelY { get => (decimal)(Entity.GetComponent<MovingPlatform2D>()?.Travel.Y ?? 0f); set { if (Entity.GetComponent<MovingPlatform2D>() is { } m) { m.Travel = new Vector2(m.Travel.X, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal MovingSpeed { get => (decimal)(Entity.GetComponent<MovingPlatform2D>()?.Speed ?? 1f); set { if (Entity.GetComponent<MovingPlatform2D>() is { } m) { m.Speed = MathF.Max(0.01f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public bool HasParticles => Entity.GetComponent<ParticleEmitter2D>() is not null;
    public decimal ParticleRate { get => (decimal)(Entity.GetComponent<ParticleEmitter2D>()?.Rate ?? 20f); set { if (Entity.GetComponent<ParticleEmitter2D>() is { } p) { p.Rate = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public decimal ParticleLifetime { get => (decimal)(Entity.GetComponent<ParticleEmitter2D>()?.Lifetime ?? 0.8f); set { if (Entity.GetComponent<ParticleEmitter2D>() is { } p) { p.Lifetime = MathF.Max(0.05f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }
    public bool HasTrail => Entity.GetComponent<Trail2D>() is not null;
    public decimal TrailLifetime { get => (decimal)(Entity.GetComponent<Trail2D>()?.Lifetime ?? 0.35f); set { if (Entity.GetComponent<Trail2D>() is { } t) { t.Lifetime = MathF.Max(0.02f, (float)value); OnPropertyChanged(); _owner.MarkDirty(); } } }

    public bool HasRotator => Entity.GetComponent<RotatorComponent>() is not null;

    public bool HasOscillator => Entity.GetComponent<OscillatorComponent>() is not null;

    public RotatorComponent? Rotator => Entity.GetComponent<RotatorComponent>();

    public ScriptComponent? Script => ActiveScript;

    public string ScriptPath
    {
        get => Script?.ScriptPath ?? string.Empty;
        set
        {
            if (Script is null)
            {
                return;
            }

            Script.ScriptPath = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public string ScriptClass
    {
        get => Script?.ClassName ?? string.Empty;
        set
        {
            if (Script is null)
            {
                return;
            }

            Script.ClassName = value ?? string.Empty;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public OscillatorComponent? Oscillator => Entity.GetComponent<OscillatorComponent>();

    public decimal RotationSpeed
    {
        get => (decimal)(Rotator?.DegreesPerSecond ?? 0f);
        set
        {
            if (Rotator is null)
            {
                return;
            }

            Rotator.DegreesPerSecond = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal OscillationAmplitude
    {
        get => (decimal)(Oscillator?.Amplitude ?? 0f);
        set
        {
            if (Oscillator is null)
            {
                return;
            }

            Oscillator.Amplitude = MathF.Max(0f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal OscillationFrequency
    {
        get => (decimal)(Oscillator?.Frequency ?? 0f);
        set
        {
            if (Oscillator is null)
            {
                return;
            }

            Oscillator.Frequency = MathF.Max(0.01f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public SpriteRenderer? Sprite => Entity.GetComponent<SpriteRenderer>();

    public Camera2D? Camera => Entity.GetComponent<Camera2D>();

    public string TexturePath
    {
        get => Sprite?.TexturePath ?? string.Empty;
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.TexturePath = string.IsNullOrWhiteSpace(value) ? null : value;
            _texturePreview = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TexturePreview));
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string ColorHex
    {
        get => ColorUtil.ToHex(Sprite?.Tint ?? Vector4.One);
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.Tint = ColorUtil.FromHex(value, Sprite.Tint);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ColorBrush));
        OnPropertyChanged(nameof(TexturePreview));
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    /// <summary>Miniatura de la textura asignada (se carga bajo demanda y se cachea).</summary>
    public IImage? TexturePreview
    {
        get
        {
            _texturePreview ??= LoadTexturePreview();
            return _texturePreview;
        }
    }

    private IImage? LoadTexturePreview()
    {
        var path = Sprite?.TexturePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var fullPath = Path.IsPathRooted(path) ? path : Path.Combine(_owner.ProjectRoot, path);
        if (!File.Exists(fullPath))
        {
            return null;
        }

        try
        {
            using var stream = File.OpenRead(fullPath);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>Asigna una textura del proyecto y ajusta el ancho según su proporción.</summary>
    public void AssignTexture(AssetItem asset)
    {
        if (Sprite is null)
        {
            return;
        }

        Sprite.TexturePath = asset.RelativePath;
        var height = Sprite.Size.Y > 0.01f ? Sprite.Size.Y : 1f;
        Sprite.Size = new Vector2(height * asset.AspectRatio, height);
        _texturePreview = null;

        OnPropertyChanged(nameof(TexturePath));
        OnPropertyChanged(nameof(TexturePreview));
        OnPropertyChanged(nameof(SpriteWidth));
        OnPropertyChanged(nameof(SpriteHeight));
        _owner.MarkDirty();
        _owner.RequestRedraw();
    }

    // ---------- Animación ----------

    public bool HasAnimator => Entity.GetComponent<SpriteAnimator>() is not null;

    public SpriteAnimator? Animator => Entity.GetComponent<SpriteAnimator>();

    public string AnimationTexture
    {
        get => Animator?.TexturePath ?? string.Empty;
        set
        {
            if (Animator is null || Animator.TexturePath == value)
            {
                return;
            }

            Animator.TexturePath = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal FrameWidth
    {
        get => Animator?.FrameWidth ?? 32;
        set
        {
            if (Animator is null || Animator.FrameWidth == (int)value)
            {
                return;
            }

            Animator.FrameWidth = Math.Max(1, (int)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal FrameHeight
    {
        get => Animator?.FrameHeight ?? 32;
        set
        {
            if (Animator is null || Animator.FrameHeight == (int)value)
            {
                return;
            }

            Animator.FrameHeight = Math.Max(1, (int)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal FrameCount
    {
        get => Animator?.FrameCount ?? 4;
        set
        {
            if (Animator is null || Animator.FrameCount == (int)value)
            {
                return;
            }

            Animator.FrameCount = Math.Max(1, (int)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal AnimationFps
    {
        get => (decimal)(Animator?.Fps ?? 8f);
        set
        {
            if (Animator is null || MathF.Abs(Animator.Fps - (float)value) < 0.001f)
            {
                return;
            }

            Animator.Fps = Math.Max(0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool AnimationLoop
    {
        get => Animator?.Loop ?? true;
        set
        {
            if (Animator is null || Animator.Loop == value)
            {
                return;
            }

            Animator.Loop = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    // ---------- Tilemap ----------

    public bool HasTilemap => Entity.GetComponents<TileMapLayer>().Any();

    private IReadOnlyList<TilePaletteItem>? _tilePalette;

    public IReadOnlyList<TilePaletteItem> TilePalette => _tilePalette ??= LoadTilePalette();

    public bool HasTilePalette => TilePalette.Count > 0;

    private int _activeTilemapLayer;

    public TileMapLayer? Tilemap => Entity.GetComponents<TileMapLayer>().ElementAtOrDefault(System.Math.Clamp(_activeTilemapLayer, 0, System.Math.Max(0, Entity.GetComponents<TileMapLayer>().Count() - 1)));

    public IReadOnlyList<string> TilemapLayerNames => Entity.GetComponents<TileMapLayer>()
        .Select((layer, index) => $"{index + 1}. {layer.SortingOrder} ({(layer.Visible ? "visible" : "oculta")})").ToList();

    public void AddTilemapLayer()
    {
        var current = Tilemap;
        var layer = new TileMapLayer
        {
            Width = current?.Width ?? 16,
            Height = current?.Height ?? 8,
            TileSize = current?.TileSize ?? 1f,
            TexturePath = current?.TexturePath,
            TilesetColumns = current?.TilesetColumns ?? 4,
            SortingOrder = (current?.SortingOrder ?? 0) + 1,
        };
        Entity.AddComponent(layer);
        _activeTilemapLayer = Entity.GetComponents<TileMapLayer>().Count() - 1;
        OnPropertyChanged(nameof(Tilemap));
        OnPropertyChanged(nameof(TilemapLayerNames));
        OnPropertyChanged(nameof(ActiveTilemapLayer));
        OnPropertyChanged(nameof(HasTilemap));
        _owner.MarkDirty();
        _owner.RequestRedraw();
    }

    public void RemoveActiveTilemapLayer()
    {
        var layers = Entity.GetComponents<TileMapLayer>().ToList();
        if (layers.Count <= 1) return;
        Entity.RemoveComponent(layers[System.Math.Clamp(_activeTilemapLayer, 0, layers.Count - 1)]);
        _activeTilemapLayer = System.Math.Max(0, _activeTilemapLayer - 1);
        OnPropertyChanged(nameof(Tilemap));
        OnPropertyChanged(nameof(TilemapLayerNames));
        OnPropertyChanged(nameof(ActiveTilemapLayer));
        OnPropertyChanged(nameof(HasTilemap));
        _owner.MarkDirty();
        _owner.RequestRedraw();
    }

    public int ActiveTilemapLayer
    {
        get => _activeTilemapLayer;
        set
        {
            var max = System.Math.Max(0, Entity.GetComponents<TileMapLayer>().Count() - 1);
            _activeTilemapLayer = System.Math.Clamp(value, 0, max);
            OnPropertyChanged();
            OnPropertyChanged(nameof(Tilemap));
            OnPropertyChanged(nameof(TilemapLayerNames));
            OnPropertyChanged(nameof(HasTilemap));
            OnPropertyChanged(nameof(TilemapTexture));
            OnPropertyChanged(nameof(TileSetTexture));
            _owner.RequestRedraw();
        }
    }

    public bool HasTileSet => Tilemap is not null;

    public string TileSetTexture
    {
        get => Tilemap?.TileSet?.TexturePath ?? string.Empty;
        set
        {
            if (Tilemap is null) return;
            Tilemap.TileSet ??= new TileSet();
            Tilemap.TileSet.TexturePath = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public decimal TileSetColumns
    {
        get => Tilemap?.TileSet?.Columns ?? 4;
        set
        {
            if (Tilemap is null) return;
            Tilemap.TileSet ??= new TileSet();
            Tilemap.TileSet.Columns = System.Math.Clamp((int)value, 1, 64);
            OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public decimal TileSetTileWidth
    {
        get => Tilemap?.TileSet?.TileWidth ?? 32;
        set
        {
            if (Tilemap is null) return;
            Tilemap.TileSet ??= new TileSet();
            Tilemap.TileSet.TileWidth = System.Math.Max(1, (int)value);
            OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public decimal TileSetTileHeight
    {
        get => Tilemap?.TileSet?.TileHeight ?? 32;
        set
        {
            if (Tilemap is null) return;
            Tilemap.TileSet ??= new TileSet();
            Tilemap.TileSet.TileHeight = System.Math.Max(1, (int)value);
            OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public string TileSetSummary => Tilemap?.TileSet is { } set ? $"{set.Tiles.Count} tiles definidos" : "Sin TileSet";

    public string TilemapTexture
    {
        get => Tilemap?.TexturePath ?? string.Empty;
        set
        {
            if (Tilemap is null || Tilemap.TexturePath == value)
            {
                return;
            }

            Tilemap.TexturePath = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal TilemapColumns
    {
        get => Tilemap?.TilesetColumns ?? 4;
        set
        {
            if (Tilemap is null || Tilemap.TilesetColumns == (int)value) return;
            Tilemap.TilesetColumns = Math.Clamp((int)value, 1, 64);
            _tilePalette = null;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TilePalette));
            OnPropertyChanged(nameof(HasTilePalette));
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    private IReadOnlyList<TilePaletteItem> LoadTilePalette()
    {
        var map = Tilemap;
        if (map is null || string.IsNullOrWhiteSpace(map.TexturePath)) return Array.Empty<TilePaletteItem>();

        var fullPath = Path.IsPathRooted(map.TexturePath) ? map.TexturePath : Path.Combine(_owner.ProjectRoot, map.TexturePath);
        if (!File.Exists(fullPath)) return Array.Empty<TilePaletteItem>();

        try
        {
            using var stream = File.OpenRead(fullPath);
            var bitmap = new Bitmap(stream);
            var columns = Math.Clamp(map.TilesetColumns, 1, 64);
            var tileWidth = Math.Max(1, bitmap.PixelSize.Width / columns);
            var rows = Math.Max(1, bitmap.PixelSize.Height / tileWidth);
            var items = new List<TilePaletteItem>();

            for (var index = 0; index < columns * rows; index++)
            {
                var x = (index % columns) * tileWidth;
                var y = (index / columns) * tileWidth;
                if (x + tileWidth > bitmap.PixelSize.Width || y + tileWidth > bitmap.PixelSize.Height) break;
                items.Add(new TilePaletteItem(index, new CroppedBitmap(bitmap, new PixelRect(x, y, tileWidth, tileWidth))));
            }

            return items;
        }
        catch
        {
            return Array.Empty<TilePaletteItem>();
        }
    }

    public decimal TilemapWidth
    {
        get => Tilemap?.Width ?? 16;
        set
        {
            if (Tilemap is null || Tilemap.Width == (int)value)
            {
                return;
            }

            Tilemap.Resize((int)value, Tilemap.Height);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal TilemapHeight
    {
        get => Tilemap?.Height ?? 8;
        set
        {
            if (Tilemap is null || Tilemap.Height == (int)value)
            {
                return;
            }

            Tilemap.Resize(Tilemap.Width, (int)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal TileSize
    {
        get => (decimal)(Tilemap?.TileSize ?? 1f);
        set
        {
            if (Tilemap is null || MathF.Abs(Tilemap.TileSize - (float)value) < 0.001f)
            {
                return;
            }

            Tilemap.TileSize = Math.Max(0.05f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    // ---------- Física ----------

    public bool HasCollider => Entity.GetComponent<Collider2D>() is not null;

    public bool HasCircleCollider => Entity.GetComponent<CircleCollider2D>() is not null;

    public bool HasEdgeCollider => Entity.GetComponent<EdgeCollider2D>() is not null;

    public bool HasArea2D => Entity.GetComponent<Area2D>() is not null;

    public Collider2D? Collider => Entity.GetComponent<Collider2D>();

    private Vector2 ColliderSize => Collider switch
    {
        BoxCollider box => box.Size,
        CapsuleCollider2D capsule => capsule.Size,
        Area2D area => area.Size,
        CircleCollider2D circle => new Vector2(circle.Radius * 2f),
        EdgeCollider2D edge => EdgeSize(edge),
        PolygonCollider2D polygon => PolygonSize(polygon),
        _ => Vector2.One,
    };

    private static Vector2 EdgeSize(EdgeCollider2D edge)
    {
        if (edge.Points.Count == 0) return Vector2.One;
        var min = edge.Points[0]; var max = edge.Points[0];
        foreach (var point in edge.Points) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
        return Vector2.Max(new Vector2(0.01f), max - min);
    }

    private static Vector2 PolygonSize(PolygonCollider2D polygon)
    {
        if (polygon.Points.Count == 0) return Vector2.One;
        var min = polygon.Points[0]; var max = polygon.Points[0];
        foreach (var point in polygon.Points) { min = Vector2.Min(min, point); max = Vector2.Max(max, point); }
        return Vector2.Max(new Vector2(0.01f), max - min);
    }

    public decimal ColliderWidth
    {
        get => (decimal)ColliderSize.X;
        set
        {
            if (Collider is null || MathF.Abs(ColliderSize.X - (float)value) < 0.001f)
            {
                return;
            }

            SetColliderSize(new Vector2(Math.Max(0.01f, (float)value), ColliderSize.Y));
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal ColliderHeight
    {
        get => (decimal)ColliderSize.Y;
        set
        {
            if (Collider is null || MathF.Abs(ColliderSize.Y - (float)value) < 0.001f)
            {
                return;
            }

            SetColliderSize(new Vector2(ColliderSize.X, Math.Max(0.01f, (float)value)));
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    private void SetColliderSize(Vector2 size)
    {
        switch (Collider)
        {
            case BoxCollider box: box.Size = size; break;
            case CapsuleCollider2D capsule: capsule.Size = size; break;
            case Area2D area: area.Size = size; break;
            case CircleCollider2D circle: circle.Radius = MathF.Max(0.01f, MathF.Max(size.X, size.Y) * 0.5f); break;
        }
    }

    public decimal ColliderRadius
    {
        get => (decimal)(Collider is CircleCollider2D circle ? circle.Radius : 0.5f);
        set
        {
            if (Collider is not CircleCollider2D circle || MathF.Abs(circle.Radius - (float)value) < 0.001f) return;
            circle.Radius = MathF.Max(0.01f, (float)value);
            OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public bool ColliderOneWay
    {
        get => Collider?.OneWay ?? false;
        set { if (Collider is null || Collider.OneWay == value) return; Collider.OneWay = value; OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public int ColliderLayer
    {
        get => Collider?.Layer ?? 0;
        set
        {
            if (Collider is null || Collider.Layer == Math.Clamp(value, 0, 31)) return;
            Collider.Layer = Math.Clamp(value, 0, 31);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public int ColliderMask
    {
        get => Collider?.Mask ?? ~0;
        set
        {
            if (Collider is null || Collider.Mask == value) return;
            Collider.Mask = value;
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool IsTrigger
    {
        get => Collider?.IsTrigger ?? false;
        set
        {
            if (Collider is null || Collider.IsTrigger == value)
            {
                return;
            }

            Collider.IsTrigger = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasRigidbody => Entity.GetComponent<Rigidbody2D>() is not null;

    public Rigidbody2D? Body => Entity.GetComponent<Rigidbody2D>();

    public decimal VelocityX
    {
        get => (decimal)(Body?.Velocity.X ?? 0f);
        set
        {
            if (Body is null || MathF.Abs(Body.Velocity.X - (float)value) < 0.001f)
            {
                return;
            }

            Body.Velocity = new Vector2((float)value, Body.Velocity.Y);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal VelocityY
    {
        get => (decimal)(Body?.Velocity.Y ?? 0f);
        set
        {
            if (Body is null || MathF.Abs(Body.Velocity.Y - (float)value) < 0.001f)
            {
                return;
            }

            Body.Velocity = new Vector2(Body.Velocity.X, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool UseGravity
    {
        get => Body?.AffectedByGravity ?? false;
        set
        {
            if (Body is null || Body.AffectedByGravity == value)
            {
                return;
            }

            Body.AffectedByGravity = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool IsStaticBody
    {
        get => Body?.IsStatic ?? false;
        set
        {
            if (Body is null || Body.IsStatic == value)
            {
                return;
            }

            Body.IsStatic = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasAudio => Entity.GetComponent<AudioSource>() is not null;

    public AudioSource? Source => Entity.GetComponent<AudioSource>();

    public string AudioClip
    {
        get => Source?.Clip ?? string.Empty;
        set
        {
            if (Source is null || Source.Clip == value)
            {
                return;
            }

            Source.Clip = string.IsNullOrWhiteSpace(value) ? null : value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal AudioVolume
    {
        get => (decimal)(Source?.Volume ?? 1f);
        set
        {
            if (Source is null || MathF.Abs(Source.Volume - (float)value) < 0.001f)
            {
                return;
            }

            Source.Volume = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool AudioLoop
    {
        get => Source?.Loop ?? false;
        set
        {
            if (Source is null || Source.Loop == value)
            {
                return;
            }

            Source.Loop = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool AudioPlayOnStart
    {
        get => Source?.PlayOnStart ?? false;
        set
        {
            if (Source is null || Source.PlayOnStart == value)
            {
                return;
            }

            Source.PlayOnStart = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool AudioMusic
    {
        get => Source?.Music ?? false;
        set
        {
            if (Source is null || Source.Music == value)
            {
                return;
            }

            Source.Music = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal AudioFadeIn
    {
        get => (decimal)(Source?.FadeIn ?? 0f);
        set
        {
            if (Source is null || MathF.Abs(Source.FadeIn - (float)value) < 0.001f)
            {
                return;
            }

            Source.FadeIn = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool IsKinematicBody
    {
        get => Body?.IsKinematic ?? false;
        set
        {
            if (Body is null || Body.IsKinematic == value)
            {
                return;
            }

            Body.IsKinematic = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool TilemapCollides
    {
        get => Tilemap?.Collides ?? false;
        set
        {
            if (Tilemap is null || Tilemap.Collides == value)
            {
                return;
            }

            Tilemap.Collides = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public IBrush ColorBrush
    {
        get
        {
            var tint = Sprite?.Tint ?? Vector4.One;
            return new SolidColorBrush(Color.FromArgb(
                (byte)(System.Math.Clamp(tint.W, 0f, 1f) * 255),
                (byte)(System.Math.Clamp(tint.X, 0f, 1f) * 255),
                (byte)(System.Math.Clamp(tint.Y, 0f, 1f) * 255),
                (byte)(System.Math.Clamp(tint.Z, 0f, 1f) * 255)));
        }
    }

    public decimal SpriteWidth
    {
        get => (decimal)(Sprite?.Size.X ?? 1f);
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.Size = new Vector2((float)value, Sprite.Size.Y);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal SpriteHeight
    {
        get => (decimal)(Sprite?.Size.Y ?? 1f);
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.Size = new Vector2(Sprite.Size.X, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal SortingOrder
    {
        get => Sprite?.SortingOrder ?? 0;
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.SortingOrder = (int)value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool FlipX
    {
        get => Sprite?.FlipX ?? false;
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.FlipX = value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool FlipY
    {
        get => Sprite?.FlipY ?? false;
        set
        {
            if (Sprite is null)
            {
                return;
            }

            Sprite.FlipY = value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal OrthographicSize
    {
        get => (decimal)(Camera?.OrthographicSize ?? 6f);
        set
        {
            if (Camera is null)
            {
                return;
            }

            Camera.OrthographicSize = MathF.Max(0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal CameraViewportX { get => (decimal)(Camera?.Viewport.X ?? 0f); set { if (Camera is null) return; Camera.Viewport = new Vector4((float)value, Camera.Viewport.Y, Camera.Viewport.Z, Camera.Viewport.W); OnPropertyChanged(); _owner.MarkDirty(); } }
    public decimal CameraViewportY { get => (decimal)(Camera?.Viewport.Y ?? 0f); set { if (Camera is null) return; Camera.Viewport = new Vector4(Camera.Viewport.X, (float)value, Camera.Viewport.Z, Camera.Viewport.W); OnPropertyChanged(); _owner.MarkDirty(); } }
    public decimal CameraViewportW { get => (decimal)(Camera?.Viewport.Z ?? 1f); set { if (Camera is null) return; Camera.Viewport = new Vector4(Camera.Viewport.X, Camera.Viewport.Y, Math.Clamp((float)value, 0.05f, 1f), Camera.Viewport.W); OnPropertyChanged(); _owner.MarkDirty(); } }
    public decimal CameraViewportH { get => (decimal)(Camera?.Viewport.W ?? 1f); set { if (Camera is null) return; Camera.Viewport = new Vector4(Camera.Viewport.X, Camera.Viewport.Y, Camera.Viewport.Z, Math.Clamp((float)value, 0.05f, 1f)); OnPropertyChanged(); _owner.MarkDirty(); } }
    public bool CameraPixelPerfect { get => Camera?.PixelPerfect ?? false; set { if (Camera is null || Camera.PixelPerfect == value) return; Camera.PixelPerfect = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw(); } }
    public decimal CameraZoom
    {
        get => (decimal)(Camera?.Zoom ?? 1f);
        set
        {
            if (Camera is null)
            {
                return;
            }

            Camera.Zoom = Math.Clamp((float)value, 0.05f, 40f);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string ComponentsSummary
    {
        get
        {
            var names = Entity.Components.Select(c => c.GetType().Name).ToList();
            return names.Count == 0 ? "Sin componentes" : string.Join(" · ", names);
        }
    }

    private static bool Nearly(float a, float b) => MathF.Abs(a - b) < 0.00001f;

    private void SetPosition(float x, float y)
    {
        if (Nearly(Entity.Transform.Position.X, x) && Nearly(Entity.Transform.Position.Y, y))
        {
            return;
        }

        Entity.Transform.Position = new Vector2(x, y);
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        _owner.MarkDirty();
        _owner.RequestRedraw();
    }

    /// <summary>Refresca el inspector tras cambios externos (por ejemplo, mover con la cámara).</summary>
    public void RefreshAll()
    {
        OnPropertyChanged(nameof(Name));
        OnPropertyChanged(nameof(X));
        OnPropertyChanged(nameof(Y));
        OnPropertyChanged(nameof(RotationDegrees));
        OnPropertyChanged(nameof(ScaleX));
        OnPropertyChanged(nameof(ScaleY));
        OnPropertyChanged(nameof(TexturePath));
        OnPropertyChanged(nameof(ColorHex));
        OnPropertyChanged(nameof(ColorBrush));
        OnPropertyChanged(nameof(TexturePreview));
        OnPropertyChanged(nameof(SpriteWidth));
        OnPropertyChanged(nameof(SpriteHeight));
        OnPropertyChanged(nameof(SortingOrder));
        OnPropertyChanged(nameof(FlipX));
        OnPropertyChanged(nameof(FlipY));
        OnPropertyChanged(nameof(OrthographicSize));
        OnPropertyChanged(nameof(CameraZoom));
        OnPropertyChanged(nameof(CameraPixelPerfect));
        OnPropertyChanged(nameof(CameraViewportX));
        OnPropertyChanged(nameof(CameraViewportY));
        OnPropertyChanged(nameof(CameraViewportW));
        OnPropertyChanged(nameof(CameraViewportH));
        OnPropertyChanged(nameof(CameraDeadzoneX));
        OnPropertyChanged(nameof(CameraDeadzoneY));
        OnPropertyChanged(nameof(CameraUseBounds));
        OnPropertyChanged(nameof(CameraBoundsMinX));
        OnPropertyChanged(nameof(CameraBoundsMinY));
        OnPropertyChanged(nameof(CameraBoundsMaxX));
        OnPropertyChanged(nameof(CameraBoundsMaxY));
        OnPropertyChanged(nameof(ComponentsSummary));
        OnPropertyChanged(nameof(RotationSpeed));
        OnPropertyChanged(nameof(OscillationAmplitude));
        OnPropertyChanged(nameof(OscillationFrequency));
_scriptFields = null;
        _scriptEditors = null;
        OnPropertyChanged(nameof(ScriptEditors));
        OnPropertyChanged(nameof(ScriptFields));
        OnPropertyChanged(nameof(HasSprite));
        OnPropertyChanged(nameof(HasCollider));
        OnPropertyChanged(nameof(HasCircleCollider));
        OnPropertyChanged(nameof(HasEdgeCollider));
        OnPropertyChanged(nameof(ColliderLayer));
        OnPropertyChanged(nameof(ColliderMask));
        OnPropertyChanged(nameof(HasArea2D));
        OnPropertyChanged(nameof(HasRigidbody));
        OnPropertyChanged(nameof(HasCharacterBody));
        OnPropertyChanged(nameof(HasMovingPlatform));
        OnPropertyChanged(nameof(HasAnimationFSM));
        OnPropertyChanged(nameof(HasCanvasLayer));
        OnPropertyChanged(nameof(HasCanvasItem2D));
        OnPropertyChanged(nameof(HasSubViewport2D));
        OnPropertyChanged(nameof(HasCanvasMaterial2D));
        OnPropertyChanged(nameof(HasAnimationPlayer));
        OnPropertyChanged(nameof(HasAnimationBlendTree));
        OnPropertyChanged(nameof(AnimationBlendTime));
        OnPropertyChanged(nameof(HasSpringJoint));
        OnPropertyChanged(nameof(HasPinJoint));
        OnPropertyChanged(nameof(HasUiCanvas));
        OnPropertyChanged(nameof(HasAudioSource));
        OnPropertyChanged(nameof(HasNavigationPolygon));
        OnPropertyChanged(nameof(HasLine2D));
        OnPropertyChanged(nameof(HasSkinnedMesh));
        OnPropertyChanged(nameof(HasMesh2D));
        OnPropertyChanged(nameof(HasMultiMesh2D));
        OnPropertyChanged(nameof(HasNetworkIdentity));
        OnPropertyChanged(nameof(HasNetworkTransform));
        OnPropertyChanged(nameof(HasPolygon2D));
        OnPropertyChanged(nameof(HasNavigationAgent));
        OnPropertyChanged(nameof(HasAudioListener));
        OnPropertyChanged(nameof(ColliderIgnoreTags));
        OnPropertyChanged(nameof(ColliderBounciness));
        OnPropertyChanged(nameof(ColliderFriction));
        OnPropertyChanged(nameof(RigidbodyContinuous));
        OnPropertyChanged(nameof(UiCanvasSummary));
        OnPropertyChanged(nameof(AnimationPlayerClip));
        OnPropertyChanged(nameof(AnimationPlayerPlaying));
        OnPropertyChanged(nameof(CanvasOrder));
        OnPropertyChanged(nameof(CanvasYSort));
        OnPropertyChanged(nameof(CanvasVisible));
        OnPropertyChanged(nameof(HasSkeleton));
        OnPropertyChanged(nameof(SkeletonUseIK));
        OnPropertyChanged(nameof(SkeletonIKX));
        OnPropertyChanged(nameof(SkeletonIKY));
        OnPropertyChanged(nameof(AnimationCurrentState));
        OnPropertyChanged(nameof(HasTransition2D));
        OnPropertyChanged(nameof(TilemapAutotile));
        OnPropertyChanged(nameof(TilemapYSort));
        OnPropertyChanged(nameof(HasTileSet));
        OnPropertyChanged(nameof(TileSetSummary));
        OnPropertyChanged(nameof(HasParticles));
        OnPropertyChanged(nameof(HasTrail));
        OnPropertyChanged(nameof(ColliderOneWay));
        OnPropertyChanged(nameof(CharacterMoveSpeed));
        OnPropertyChanged(nameof(CharacterJumpSpeed));
        OnPropertyChanged(nameof(CharacterGravity));
        OnPropertyChanged(nameof(CharacterAcceleration));
        OnPropertyChanged(nameof(CharacterMaxSlopeAngle));
        OnPropertyChanged(nameof(CharacterFloorSnapLength));
        OnPropertyChanged(nameof(HasAudio));
        OnPropertyChanged(nameof(HasLight));
        OnPropertyChanged(nameof(HasCamera));
        OnPropertyChanged(nameof(HasTilemap));
        OnPropertyChanged(nameof(HasAnimator));
        OnPropertyChanged(nameof(HasScript));
        OnPropertyChanged(nameof(HasInteractable));
        OnPropertyChanged(nameof(HasRotator));
        OnPropertyChanged(nameof(HasOscillator));
        OnPropertyChanged(nameof(ScriptPath));
        OnPropertyChanged(nameof(ScriptClass));
    }

    public string TagsText
    {
        get => string.Join(", ", Entity.Tags);
        set
        {
            var tags = (value ?? string.Empty)
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(tag => tag.Length > 0)
                .ToList();

            if (tags.SequenceEqual(Entity.Tags))
            {
                return;
            }

            Entity.Tags = tags;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasInteractable => Entity.GetComponent<Interactable>() is not null;

    public Interactable? Interaction => Entity.GetComponent<Interactable>();

    public string InteractionPrompt
    {
        get => Interaction?.Prompt ?? string.Empty;
        set
        {
            if (Interaction is null || Interaction.Prompt == value)
            {
                return;
            }

            Interaction.Prompt = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal InteractionRadius
    {
        get => (decimal)(Interaction?.Radius ?? 1.8f);
        set
        {
            if (Interaction is null || MathF.Abs(Interaction.Radius - (float)value) < 0.001f)
            {
                return;
            }

            Interaction.Radius = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public string DialogueId
    {
        get => Interaction?.DialogueId ?? string.Empty;
        set
        {
            if (Interaction is null || Interaction.DialogueId == value)
            {
                return;
            }

            Interaction.DialogueId = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool InteractOnce
    {
        get => Interaction?.Once ?? true;
        set
        {
            if (Interaction is null || Interaction.Once == value)
            {
                return;
            }

            Interaction.Once = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasLight => Entity.GetComponent<Light2D>() is not null;

    public Light2D? Light => Entity.GetComponent<Light2D>();

    public bool LightIsCone
    {
        get => Light?.Kind == LightKind.Cone;
        set
        {
            if (Light is null)
            {
                return;
            }

            var kind = value ? LightKind.Cone : LightKind.Point;
            if (Light.Kind == kind)
            {
                return;
            }

            Light.Kind = kind;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string LightColorHex
    {
        get => ColorUtil.ToHex(Light?.Color ?? Vector4.One);
        set
        {
            if (Light is null)
            {
                return;
            }

            Light.Color = ColorUtil.FromHex(value, Light.Color);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal LightRadius
    {
        get => (decimal)(Light?.Radius ?? 4.5f);
        set
        {
            if (Light is null || MathF.Abs(Light.Radius - (float)value) < 0.001f)
            {
                return;
            }

            Light.Radius = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal LightIntensity
    {
        get => (decimal)(Light?.Intensity ?? 1f);
        set
        {
            if (Light is null || MathF.Abs(Light.Intensity - (float)value) < 0.001f)
            {
                return;
            }

            Light.Intensity = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal LightConeAngle
    {
        get => (decimal)(Light?.ConeAngle ?? 60f);
        set
        {
            if (Light is null || MathF.Abs(Light.ConeAngle - (float)value) < 0.001f)
            {
                return;
            }

            Light.ConeAngle = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool LightShadows
    {
        get => Light?.CastShadows ?? false;
        set
        {
            if (Light is null || Light.CastShadows == value)
            {
                return;
            }

            Light.CastShadows = value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal LightFlicker
    {
        get => (decimal)(Light?.FlickerAmplitude ?? 0f);
        set
        {
            if (Light is null || MathF.Abs(Light.FlickerAmplitude - (float)value) < 0.001f)
            {
                return;
            }

            Light.FlickerAmplitude = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal Parallax
    {
        get => (decimal)(Sprite?.Parallax ?? 1f);
        set
        {
            if (Sprite is null || MathF.Abs(Sprite.Parallax - (float)value) < 0.001f)
            {
                return;
            }

            Sprite.Parallax = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string AmbientHex
    {
        get => ColorUtil.ToHex(Camera?.Ambient ?? Vector4.One);
        set
        {
            if (Camera is null)
            {
                return;
            }

            Camera.Ambient = ColorUtil.FromHex(value, Camera.Ambient);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string FollowTag
    {
        get => Camera?.FollowTag ?? string.Empty;
        set
        {
            if (Camera is null || (Camera.FollowTag ?? string.Empty) == value)
            {
                return;
            }

            Camera.FollowTag = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal CameraDeadzoneX
    {
        get => (decimal)(Camera?.Deadzone.X ?? 0.75f);
        set
        {
            if (Camera is null || MathF.Abs(Camera.Deadzone.X - (float)value) < 0.001f) return;
            Camera.Deadzone = new Vector2(MathF.Max(0f, (float)value), Camera.Deadzone.Y);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal CameraDeadzoneY
    {
        get => (decimal)(Camera?.Deadzone.Y ?? 0.5f);
        set
        {
            if (Camera is null || MathF.Abs(Camera.Deadzone.Y - (float)value) < 0.001f) return;
            Camera.Deadzone = new Vector2(Camera.Deadzone.X, MathF.Max(0f, (float)value));
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool CameraUseBounds
    {
        get => Camera?.UseBounds ?? false;
        set
        {
            if (Camera is null || Camera.UseBounds == value) return;
            Camera.UseBounds = value;
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal CameraBoundsMinX
    {
        get => (decimal)(Camera?.BoundsMin.X ?? -100f);
        set { if (Camera is null) return; Camera.BoundsMin = new Vector2((float)value, Camera.BoundsMin.Y); OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public decimal CameraBoundsMinY
    {
        get => (decimal)(Camera?.BoundsMin.Y ?? -100f);
        set { if (Camera is null) return; Camera.BoundsMin = new Vector2(Camera.BoundsMin.X, (float)value); OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public decimal CameraBoundsMaxX
    {
        get => (decimal)(Camera?.BoundsMax.X ?? 100f);
        set { if (Camera is null) return; Camera.BoundsMax = new Vector2((float)value, Camera.BoundsMax.Y); OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public decimal CameraBoundsMaxY
    {
        get => (decimal)(Camera?.BoundsMax.Y ?? 100f);
        set { if (Camera is null) return; Camera.BoundsMax = new Vector2(Camera.BoundsMax.X, (float)value); OnPropertyChanged(); _owner.MarkDirty(); }
    }

    public decimal FollowSmoothing
    {
        get => (decimal)(Camera?.FollowSmoothing ?? 6f);
        set
        {
            if (Camera is null || MathF.Abs(Camera.FollowSmoothing - (float)value) < 0.001f)
            {
                return;
            }

            Camera.FollowSmoothing = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

}
