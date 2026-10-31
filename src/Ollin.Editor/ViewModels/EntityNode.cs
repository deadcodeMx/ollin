using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.ViewModels;

/// <summary>
/// Nodo del árbol de escena. Envuelve una <see cref="Entity"/> y expone las
/// propiedades que edita el inspector (con conversión a unidades amigables).
/// </summary>
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

    public bool HasRotator => Entity.GetComponent<RotatorComponent>() is not null;

    public bool HasOscillator => Entity.GetComponent<OscillatorComponent>() is not null;

    public RotatorComponent? Rotator => Entity.GetComponent<RotatorComponent>();

    public ScriptComponent? Script => Entity.GetComponent<ScriptComponent>();

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

    public bool HasTilemap => Entity.GetComponent<Tilemap>() is not null;

    public Tilemap? Tilemap => Entity.GetComponent<Tilemap>();

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

    public bool HasCollider => Entity.GetComponent<BoxCollider>() is not null;

    public BoxCollider? Collider => Entity.GetComponent<BoxCollider>();

    public decimal ColliderWidth
    {
        get => (decimal)(Collider?.Size.X ?? 1f);
        set
        {
            if (Collider is null || MathF.Abs(Collider.Size.X - (float)value) < 0.001f)
            {
                return;
            }

            Collider.Size = new Vector2(Math.Max(0.01f, (float)value), Collider.Size.Y);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal ColliderHeight
    {
        get => (decimal)(Collider?.Size.Y ?? 1f);
        set
        {
            if (Collider is null || MathF.Abs(Collider.Size.Y - (float)value) < 0.001f)
            {
                return;
            }

            Collider.Size = new Vector2(Collider.Size.X, Math.Max(0.01f, (float)value));
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
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
        OnPropertyChanged(nameof(OrthographicSize));
        OnPropertyChanged(nameof(CameraZoom));
        OnPropertyChanged(nameof(ComponentsSummary));
        OnPropertyChanged(nameof(RotationSpeed));
        OnPropertyChanged(nameof(WallWidthInput));
        OnPropertyChanged(nameof(WallHeight));
        OnPropertyChanged(nameof(WallDepth));
        OnPropertyChanged(nameof(WallYaw));
        OnPropertyChanged(nameof(WallBaseY));
        OnPropertyChanged(nameof(WallTextureScale));
        OnPropertyChanged(nameof(Camera3DFov));
        OnPropertyChanged(nameof(Camera3DHeight));
        OnPropertyChanged(nameof(Camera3DYaw));
        OnPropertyChanged(nameof(Camera3DPitch));
        OnPropertyChanged(nameof(Camera3DFogStart));
        OnPropertyChanged(nameof(Camera3DFogEnd));
        OnPropertyChanged(nameof(FpsSpeed));
        OnPropertyChanged(nameof(FpsRun));
        OnPropertyChanged(nameof(FpsSensitivity));
        OnPropertyChanged(nameof(FpsRadius));
        OnPropertyChanged(nameof(Ai3DTargetTag));
        OnPropertyChanged(nameof(Ai3DSpeed));
        OnPropertyChanged(nameof(Ai3DVision));
        OnPropertyChanged(nameof(Ai3DPatrolRadius));
        OnPropertyChanged(nameof(Ai3DMemory));
        OnPropertyChanged(nameof(Ai3DRadius));
        OnPropertyChanged(nameof(Ai3DStatus));
        OnPropertyChanged(nameof(OscillationAmplitude));
        OnPropertyChanged(nameof(OscillationFrequency));
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

    public bool HasInventory => Entity.GetComponent<Inventory>() is not null;

    public Inventory? Inventory => Entity.GetComponent<Inventory>();

    public decimal InventoryCapacity
    {
        get => Inventory?.Capacity ?? 12;
        set
        {
            if (Inventory is null || Inventory.Capacity == (int)value)
            {
                return;
            }

            Inventory.Capacity = System.Math.Max(1, (int)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public string InventorySummary =>
        Inventory is null || Inventory.Slots.Count == 0
            ? "(vacío)"
            : string.Join(", ", Inventory.Slots.Select(slot => $"{slot.ItemId} x{slot.Quantity}"));

    /// <summary>Texto temporal del inspector para añadir o quitar objetos.</summary>
    public string NewItemText { get; set; } = string.Empty;

    public void AddItemFromText()
    {
        if (Inventory is null || !TryParseItem(NewItemText, out var id, out var quantity))
        {
            return;
        }

        Inventory.Add(id, quantity);
        NewItemText = string.Empty;
        OnPropertyChanged(nameof(NewItemText));
        OnPropertyChanged(nameof(InventorySummary));
        _owner.MarkDirty();
    }

    public void RemoveItemFromText()
    {
        if (Inventory is null || !TryParseItem(NewItemText, out var id, out var quantity))
        {
            return;
        }

        Inventory.Remove(id, quantity);
        NewItemText = string.Empty;
        OnPropertyChanged(nameof(NewItemText));
        OnPropertyChanged(nameof(InventorySummary));
        _owner.MarkDirty();
    }

    public void ClearInventory()
    {
        if (Inventory is null || Inventory.Slots.Count == 0)
        {
            return;
        }

        Inventory.Clear();
        OnPropertyChanged(nameof(InventorySummary));
        _owner.MarkDirty();
    }

    private static bool TryParseItem(string? text, out string id, out int quantity)
    {
        id = string.Empty;
        quantity = 1;

        var parts = (text ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0)
        {
            return false;
        }

        id = parts[0];

        if (parts.Length > 1)
        {
            var amount = parts[1].TrimStart('x', 'X');
            int.TryParse(amount, out quantity);
        }

        quantity = System.Math.Max(1, quantity);
        return id.Length > 0;
    }

    public bool HasStats => Entity.GetComponent<Stats>() is not null;

    public Stats? Stats => Entity.GetComponent<Stats>();

    public string StatsSummary =>
        Stats is null || Stats.Entries.Count == 0
            ? "(vacío)"
            : string.Join(", ", Stats.Entries.Select(entry =>
                $"{entry.Name} {System.Math.Round(entry.Value)}/{System.Math.Round(entry.Max)}"));

    public string NewStatText { get; set; } = string.Empty;

    public void ApplyStatFromText()
    {
        if (Stats is null || string.IsNullOrWhiteSpace(NewStatText))
        {
            return;
        }

        var text = NewStatText.Trim();
        var name = text;
        var mode = '=';
        var value = 0f;
        var index = text.IndexOfAny(new[] { '=', '+', '-' });

        if (index > 0)
        {
            name = text[..index].Trim();
            mode = text[index];
            float.TryParse(text[(index + 1)..].Replace(',', '.'), System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out value);
        }

        switch (mode)
        {
            case '+':
                Stats.Add(name, value);
                break;
            case '-':
                Stats.Add(name, -value);
                break;
            case '=':
                Stats.Set(name, value);
                break;
            default:
                Stats.Ensure(name);
                break;
        }

        NewStatText = string.Empty;
        OnPropertyChanged(nameof(NewStatText));
        OnPropertyChanged(nameof(StatsSummary));
        _owner.MarkDirty();
    }

    public void ClearStats()
    {
        if (Stats is null || Stats.Entries.Count == 0)
        {
            return;
        }

        Stats.Entries.Clear();
        OnPropertyChanged(nameof(StatsSummary));
        _owner.MarkDirty();
    }

    public bool HasDayNight => Entity.GetComponent<DayNightCycle>() is not null;

    public DayNightCycle? DayNight => Entity.GetComponent<DayNightCycle>();

    public decimal DayLength
    {
        get => (decimal)(DayNight?.DayLengthSeconds ?? 120f);
        set
        {
            if (DayNight is null || MathF.Abs(DayNight.DayLengthSeconds - (float)value) < 0.01f)
            {
                return;
            }

            DayNight.DayLengthSeconds = System.Math.Max(1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal TimeOfDay
    {
        get => (decimal)(DayNight?.TimeOfDay ?? 0f);
        set
        {
            if (DayNight is null || MathF.Abs(DayNight.TimeOfDay - (float)value) < 0.001f)
            {
                return;
            }

            DayNight.TimeOfDay = System.Math.Clamp((float)value, 0f, 0.999f);
            OnPropertyChanged();
            OnPropertyChanged(nameof(ClockText));
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string ClockText
    {
        get
        {
            if (DayNight is null)
            {
                return string.Empty;
            }

            var totalMinutes = (int)(DayNight.TimeOfDay * 24f * 60f);
            return $"{totalMinutes / 60:00}:{totalMinutes % 60:00} · {(DayNight.DayFactor > 0.5f ? "día" : "noche")}";
        }
    }

    public string DayAmbientHex
    {
        get => ColorUtil.ToHex(DayNight?.DayAmbient ?? Vector4.One);
        set
        {
            if (DayNight is null)
            {
                return;
            }

            DayNight.DayAmbient = ColorUtil.FromHex(value, DayNight.DayAmbient);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string NightAmbientHex
    {
        get => ColorUtil.ToHex(DayNight?.NightAmbient ?? Vector4.One);
        set
        {
            if (DayNight is null)
            {
                return;
            }

            DayNight.NightAmbient = ColorUtil.FromHex(value, DayNight.NightAmbient);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool DayPaused
    {
        get => DayNight?.Paused ?? false;
        set
        {
            if (DayNight is null || DayNight.Paused == value)
            {
                return;
            }

            DayNight.Paused = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasAi => Entity.GetComponent<AiAgent>() is not null;

    public AiAgent? Ai => Entity.GetComponent<AiAgent>();

    public string AiTargetTag
    {
        get => Ai?.TargetTag ?? string.Empty;
        set
        {
            if (Ai is null || Ai.TargetTag == value)
            {
                return;
            }

            Ai.TargetTag = string.IsNullOrWhiteSpace(value) ? "player" : value.Trim();
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal AiSpeed
    {
        get => (decimal)(Ai?.Speed ?? 2.5f);
        set
        {
            if (Ai is null || MathF.Abs(Ai.Speed - (float)value) < 0.001f)
            {
                return;
            }

            Ai.Speed = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal AiVision
    {
        get => (decimal)(Ai?.VisionRadius ?? 6f);
        set
        {
            if (Ai is null || MathF.Abs(Ai.VisionRadius - (float)value) < 0.001f)
            {
                return;
            }

            Ai.VisionRadius = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal AiPatrolRadius
    {
        get => (decimal)(Ai?.PatrolRadius ?? 3f);
        set
        {
            if (Ai is null || MathF.Abs(Ai.PatrolRadius - (float)value) < 0.001f)
            {
                return;
            }

            Ai.PatrolRadius = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal AiRefresh
    {
        get => (decimal)(Ai?.PathRefreshSeconds ?? 0.45f);
        set
        {
            if (Ai is null || MathF.Abs(Ai.PathRefreshSeconds - (float)value) < 0.001f)
            {
                return;
            }

            Ai.PathRefreshSeconds = (float)value;
            OnPropertyChanged();
        }
    }

    public decimal AiMemory
    {
        get => (decimal)(Ai?.MemorySeconds ?? 3f);
        set
        {
            if (Ai is null || MathF.Abs(Ai.MemorySeconds - (float)value) < 0.01f)
            {
                return;
            }

            Ai.MemorySeconds = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public string AiStatus => Ai is null ? string.Empty : Ai.IsChasing ? "Persiguiendo" : "Patrullando";

    public void RefreshAiStatus() => OnPropertyChanged(nameof(AiStatus));

    public bool HasCamera3D => Entity.GetComponent<Camera3D>() is not null;

    public Camera3D? Camera3D => Entity.GetComponent<Camera3D>();

    public decimal Camera3DFov
    {
        get => (decimal)(Camera3D?.FovDegrees ?? 70f);
        set
        {
            if (Camera3D is null || MathF.Abs(Camera3D.FovDegrees - (float)value) < 0.01f) return;
            Camera3D.FovDegrees = Math.Clamp((float)value, 20f, 140f);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal Camera3DHeight
    {
        get => (decimal)(Camera3D?.Height ?? 1.7f);
        set
        {
            if (Camera3D is null || MathF.Abs(Camera3D.Height - (float)value) < 0.001f) return;
            Camera3D.Height = MathF.Max(0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal Camera3DYaw
    {
        get => (decimal)((Camera3D?.Yaw ?? 0f) * 180f / MathF.PI);
        set
        {
            if (Camera3D is null) return;
            Camera3D.Yaw = (float)value * MathF.PI / 180f;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal Camera3DPitch
    {
        get => (decimal)((Camera3D?.Pitch ?? 0f) * 180f / MathF.PI);
        set
        {
            if (Camera3D is null) return;
            Camera3D.Pitch = Math.Clamp((float)value * MathF.PI / 180f, -1.4f, 1.4f);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal Camera3DFogStart
    {
        get => (decimal)(Camera3D?.FogStart ?? 6f);
        set
        {
            if (Camera3D is null || MathF.Abs(Camera3D.FogStart - (float)value) < 0.01f) return;
            Camera3D.FogStart = MathF.Max(0f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal Camera3DFogEnd
    {
        get => (decimal)(Camera3D?.FogEnd ?? 28f);
        set
        {
            if (Camera3D is null || MathF.Abs(Camera3D.FogEnd - (float)value) < 0.01f) return;
            Camera3D.FogEnd = MathF.Max(Camera3D.FogStart + 0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool Camera3DFogEnabled
    {
        get => Camera3D?.FogEnabled ?? false;
        set
        {
            if (Camera3D is null || Camera3D.FogEnabled == value) return;
            Camera3D.FogEnabled = value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string Camera3DBackgroundHex
    {
        get => ColorUtil.ToHex(Camera3D?.Background ?? Vector4.One);
        set
        {
            if (Camera3D is null) return;
            Camera3D.Background = ColorUtil.FromHex(value, Camera3D.Background);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string Camera3DFogHex
    {
        get => ColorUtil.ToHex(Camera3D?.FogColor ?? Vector4.One);
        set
        {
            if (Camera3D is null) return;
            Camera3D.FogColor = ColorUtil.FromHex(value, Camera3D.FogColor);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool HasWall3D => Entity.GetComponent<Wall3D>() is not null;

    public Wall3D? Wall3D => Entity.GetComponent<Wall3D>();

    public decimal WallWidth => (decimal)(Wall3D?.Size.X ?? 1f);

    public decimal WallHeight
    {
        get => (decimal)(Wall3D?.Size.Y ?? 1f);
        set
        {
            if (Wall3D is null || MathF.Abs(Wall3D.Size.Y - (float)value) < 0.001f) return;
            Wall3D.Size = new Vector3(Wall3D.Size.X, MathF.Max(0.05f, (float)value), Wall3D.Size.Z);
            OnPropertyChanged(); OnPropertyChanged(nameof(WallHeight)); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public decimal WallDepth
    {
        get => (decimal)(Wall3D?.Size.Z ?? 1f);
        set
        {
            if (Wall3D is null || MathF.Abs(Wall3D.Size.Z - (float)value) < 0.001f) return;
            Wall3D.Size = new Vector3(Wall3D.Size.X, Wall3D.Size.Y, MathF.Max(0.05f, (float)value));
            OnPropertyChanged(); OnPropertyChanged(nameof(WallDepth)); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public decimal WallWidthInput
    {
        get => (decimal)(Wall3D?.Size.X ?? 1f);
        set
        {
            if (Wall3D is null || MathF.Abs(Wall3D.Size.X - (float)value) < 0.001f) return;
            Wall3D.Size = new Vector3(MathF.Max(0.05f, (float)value), Wall3D.Size.Y, Wall3D.Size.Z);
            OnPropertyChanged(); OnPropertyChanged(nameof(WallWidthInput)); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public decimal WallBaseY
    {
        get => (decimal)(Wall3D?.BaseY ?? 0f);
        set
        {
            if (Wall3D is null || MathF.Abs(Wall3D.BaseY - (float)value) < 0.001f) return;
            Wall3D.BaseY = (float)value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal WallYaw
    {
        get => (decimal)((Wall3D?.Yaw ?? 0f) * 180f / MathF.PI);
        set
        {
            if (Wall3D is null) return;
            Wall3D.Yaw = (float)value * MathF.PI / 180f;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string WallColorHex
    {
        get => ColorUtil.ToHex(Wall3D is null ? Vector4.One : new Vector4(Wall3D.Color, 1f));
        set
        {
            if (Wall3D is null) return;
            var color = ColorUtil.FromHex(value, new Vector4(Wall3D.Color, 1f));
            Wall3D.Color = new Vector3(color.X, color.Y, color.Z);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public string WallTexture
    {
        get => Wall3D?.TexturePath ?? string.Empty;
        set
        {
            if (Wall3D is null || (Wall3D.TexturePath ?? string.Empty) == value) return;
            Wall3D.TexturePath = string.IsNullOrWhiteSpace(value) ? null : value.Trim();
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public decimal WallTextureScale
    {
        get => (decimal)(Wall3D?.TextureScale ?? 1f);
        set
        {
            if (Wall3D is null || MathF.Abs(Wall3D.TextureScale - (float)value) < 0.001f) return;
            Wall3D.TextureScale = MathF.Max(0.05f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool WallVisible
    {
        get => Wall3D?.Visible ?? true;
        set
        {
            if (Wall3D is null || Wall3D.Visible == value) return;
            Wall3D.Visible = value;
            OnPropertyChanged();
            _owner.MarkDirty();
            _owner.RequestRedraw();
        }
    }

    public bool WallCollides
    {
        get => Wall3D?.Collides ?? true;
        set
        {
            if (Wall3D is null || Wall3D.Collides == value) return;
            Wall3D.Collides = value;
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasFps => Entity.GetComponent<FirstPersonController>() is not null;

    public FirstPersonController? Fps => Entity.GetComponent<FirstPersonController>();

    public decimal FpsSpeed
    {
        get => (decimal)(Fps?.Speed ?? 4f);
        set
        {
            if (Fps is null || MathF.Abs(Fps.Speed - (float)value) < 0.01f) return;
            Fps.Speed = MathF.Max(0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal FpsRun
    {
        get => (decimal)(Fps?.RunMultiplier ?? 1.75f);
        set
        {
            if (Fps is null || MathF.Abs(Fps.RunMultiplier - (float)value) < 0.01f) return;
            Fps.RunMultiplier = MathF.Max(1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal FpsSensitivity
    {
        get => (decimal)((Fps?.MouseSensitivity ?? 0.0022f) * 1000f);
        set
        {
            if (Fps is null) return;
            Fps.MouseSensitivity = MathF.Max(0.0001f, (float)value / 1000f);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal FpsRadius
    {
        get => (decimal)(Fps?.CollisionRadius ?? 0.35f);
        set
        {
            if (Fps is null || MathF.Abs(Fps.CollisionRadius - (float)value) < 0.001f) return;
            Fps.CollisionRadius = MathF.Max(0.05f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public bool HasAi3D => Entity.GetComponent<AiAgent3D>() is not null;

    public AiAgent3D? Ai3D => Entity.GetComponent<AiAgent3D>();

    public string Ai3DTargetTag
    {
        get => Ai3D?.TargetTag ?? string.Empty;
        set
        {
            if (Ai3D is null || Ai3D.TargetTag == value) return;
            Ai3D.TargetTag = string.IsNullOrWhiteSpace(value) ? "player" : value.Trim();
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal Ai3DSpeed
    {
        get => (decimal)(Ai3D?.Speed ?? 1.6f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.Speed - (float)value) < 0.001f) return;
            Ai3D.Speed = MathF.Max(0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal Ai3DVision
    {
        get => (decimal)(Ai3D?.VisionRadius ?? 8f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.VisionRadius - (float)value) < 0.001f) return;
            Ai3D.VisionRadius = MathF.Max(0.1f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal Ai3DPatrolRadius
    {
        get => (decimal)(Ai3D?.PatrolRadius ?? 3f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.PatrolRadius - (float)value) < 0.001f) return;
            Ai3D.PatrolRadius = MathF.Max(0f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal Ai3DMemory
    {
        get => (decimal)(Ai3D?.MemorySeconds ?? 3f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.MemorySeconds - (float)value) < 0.01f) return;
            Ai3D.MemorySeconds = MathF.Max(0f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public decimal Ai3DRadius
    {
        get => (decimal)(Ai3D?.CollisionRadius ?? 0.4f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.CollisionRadius - (float)value) < 0.001f) return;
            Ai3D.CollisionRadius = MathF.Max(0.05f, (float)value);
            OnPropertyChanged();
            _owner.MarkDirty();
        }
    }

    public string Ai3DStatus => Ai3D is null ? string.Empty : Ai3D.IsChasing ? "Persiguiendo" : "Patrullando";

    public void RefreshAi3DStatus() => OnPropertyChanged(nameof(Ai3DStatus));

    public bool HasHealth => Entity.GetComponent<Health>() is not null;

    public Health? HealthState => Entity.GetComponent<Health>();

    public decimal HealthMax
    {
        get => (decimal)(HealthState?.Max ?? 100f);
        set
        {
            if (HealthState is null || MathF.Abs(HealthState.Max - (float)value) < 0.01f) return;
            HealthState.Max = MathF.Max(1f, (float)value);
            HealthState.Current = MathF.Min(HealthState.Current, HealthState.Max);
            OnPropertyChanged(); OnPropertyChanged(nameof(HealthCurrent)); _owner.MarkDirty();
        }
    }

    public decimal HealthCurrent
    {
        get => (decimal)(HealthState?.Current ?? 100f);
        set
        {
            if (HealthState is null || MathF.Abs(HealthState.Current - (float)value) < 0.01f) return;
            HealthState.Current = Math.Clamp((float)value, 0f, HealthState.Max);
            OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public bool HealthDeactivate
    {
        get => HealthState?.DeactivateOnDeath ?? true;
        set
        {
            if (HealthState is null || HealthState.DeactivateOnDeath == value) return;
            HealthState.DeactivateOnDeath = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HealthShowHud
    {
        get => HealthState?.ShowOnHud ?? true;
        set
        {
            if (HealthState is null || HealthState.ShowOnHud == value) return;
            HealthState.ShowOnHud = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string HealthLabelText
    {
        get => HealthState?.HudLabel ?? "Vida";
        set
        {
            if (HealthState is null || HealthState.HudLabel == value) return;
            HealthState.HudLabel = string.IsNullOrWhiteSpace(value) ? "Vida" : value.Trim();
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasWeapon => Entity.GetComponent<Weapon>() is not null;

    public Weapon? WeaponState => Entity.GetComponent<Weapon>();

    public decimal WeaponDamage
    {
        get => (decimal)(WeaponState?.Damage ?? 34f);
        set
        {
            if (WeaponState is null || MathF.Abs(WeaponState.Damage - (float)value) < 0.01f) return;
            WeaponState.Damage = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponRange
    {
        get => (decimal)(WeaponState?.Range ?? 30f);
        set
        {
            if (WeaponState is null || MathF.Abs(WeaponState.Range - (float)value) < 0.01f) return;
            WeaponState.Range = MathF.Max(0.5f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponCooldown
    {
        get => (decimal)(WeaponState?.Cooldown ?? 0.3f);
        set
        {
            if (WeaponState is null || MathF.Abs(WeaponState.Cooldown - (float)value) < 0.001f) return;
            WeaponState.Cooldown = MathF.Max(0.05f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponAmmo
    {
        get => WeaponState?.Ammo ?? -1;
        set
        {
            if (WeaponState is null || WeaponState.Ammo == (int)value) return;
            WeaponState.Ammo = (int)value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool WeaponAutomatic
    {
        get => WeaponState?.Automatic ?? true;
        set
        {
            if (WeaponState is null || WeaponState.Automatic == value) return;
            WeaponState.Automatic = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasPickup => Entity.GetComponent<Pickup>() is not null;

    public Pickup? PickupState => Entity.GetComponent<Pickup>();

    public int PickupKindIndex
    {
        get => PickupState?.Kind == PickupKind.Health ? 1 : 0;
        set
        {
            if (PickupState is null || (int)PickupState.Kind == value) return;
            PickupState.Kind = value == 1 ? PickupKind.Health : PickupKind.Item;
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string PickupItemId
    {
        get => PickupState?.ItemId ?? string.Empty;
        set
        {
            if (PickupState is null || PickupState.ItemId == value) return;
            PickupState.ItemId = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal PickupQuantity
    {
        get => PickupState?.Quantity ?? 1;
        set
        {
            if (PickupState is null || PickupState.Quantity == (int)value) return;
            PickupState.Quantity = Math.Max(1, (int)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal PickupHealthAmount
    {
        get => (decimal)(PickupState?.HealthAmount ?? 25f);
        set
        {
            if (PickupState is null || MathF.Abs(PickupState.HealthAmount - (float)value) < 0.01f) return;
            PickupState.HealthAmount = MathF.Max(1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal PickupRadius
    {
        get => (decimal)(PickupState?.Radius ?? 1.2f);
        set
        {
            if (PickupState is null || MathF.Abs(PickupState.Radius - (float)value) < 0.01f) return;
            PickupState.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string PickupMessage
    {
        get => PickupState?.Message ?? string.Empty;
        set
        {
            if (PickupState is null || PickupState.Message == value) return;
            PickupState.Message = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasDoor => Entity.GetComponent<Door>() is not null;

    public Door? DoorState => Entity.GetComponent<Door>();

    public string DoorKeyItemId
    {
        get => DoorState?.KeyItemId ?? string.Empty;
        set
        {
            if (DoorState is null || DoorState.KeyItemId == value) return;
            DoorState.KeyItemId = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal DoorRadius
    {
        get => (decimal)(DoorState?.InteractionRadius ?? 2.8f);
        set
        {
            if (DoorState is null || MathF.Abs(DoorState.InteractionRadius - (float)value) < 0.01f) return;
            DoorState.InteractionRadius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal DoorOpenHeight
    {
        get => (decimal)(DoorState?.OpenHeight ?? 3f);
        set
        {
            if (DoorState is null || MathF.Abs(DoorState.OpenHeight - (float)value) < 0.01f) return;
            DoorState.OpenHeight = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal DoorSpeed
    {
        get => (decimal)(DoorState?.Speed ?? 2f);
        set
        {
            if (DoorState is null || MathF.Abs(DoorState.Speed - (float)value) < 0.01f) return;
            DoorState.Speed = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool DoorOpen
    {
        get => DoorState?.Open ?? false;
        set
        {
            if (DoorState is null || DoorState.Open == value) return;
            DoorState.Open = value; OnPropertyChanged(); _owner.MarkDirty(); _owner.RequestRedraw();
        }
    }

    public bool HasRules => Entity.GetComponent<WinLoseRules>() is not null;

    public WinLoseRules? RulesState => Entity.GetComponent<WinLoseRules>();

    public string RulesPlayerTag
    {
        get => RulesState?.PlayerTag ?? "player";
        set
        {
            if (RulesState is null || RulesState.PlayerTag == value) return;
            RulesState.PlayerTag = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string RulesEnemiesTag
    {
        get => RulesState?.EnemiesTag ?? "enemy";
        set
        {
            if (RulesState is null || RulesState.EnemiesTag == value) return;
            RulesState.EnemiesTag = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool RulesWin
    {
        get => RulesState?.WinWhenEnemiesDead ?? true;
        set
        {
            if (RulesState is null || RulesState.WinWhenEnemiesDead == value) return;
            RulesState.WinWhenEnemiesDead = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool RulesLose
    {
        get => RulesState?.LoseWhenPlayerDead ?? true;
        set
        {
            if (RulesState is null || RulesState.LoseWhenPlayerDead == value) return;
            RulesState.LoseWhenPlayerDead = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string RulesWinMessage
    {
        get => RulesState?.WinMessage ?? "¡Has ganado!";
        set
        {
            if (RulesState is null || RulesState.WinMessage == value) return;
            RulesState.WinMessage = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string RulesLoseMessage
    {
        get => RulesState?.LoseMessage ?? "Has caído";
        set
        {
            if (RulesState is null || RulesState.LoseMessage == value) return;
            RulesState.LoseMessage = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasIntro => Entity.GetComponent<IntroMessage>() is not null;

    public IntroMessage? IntroState => Entity.GetComponent<IntroMessage>();

    public string IntroText
    {
        get => IntroState?.Text ?? string.Empty;
        set
        {
            if (IntroState is null || IntroState.Text == value) return;
            IntroState.Text = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal IntroDuration
    {
        get => (decimal)(IntroState?.Duration ?? 5f);
        set
        {
            if (IntroState is null || MathF.Abs(IntroState.Duration - (float)value) < 0.01f) return;
            IntroState.Duration = MathF.Max(0.5f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal Ai3DAttackDamage
    {
        get => (decimal)(Ai3D?.AttackDamage ?? 0f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.AttackDamage - (float)value) < 0.01f) return;
            Ai3D.AttackDamage = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal Ai3DAttackRange
    {
        get => (decimal)(Ai3D?.AttackRange ?? 1.5f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.AttackRange - (float)value) < 0.01f) return;
            Ai3D.AttackRange = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal Ai3DAttackCooldown
    {
        get => (decimal)(Ai3D?.AttackCooldown ?? 1.2f);
        set
        {
            if (Ai3D is null || MathF.Abs(Ai3D.AttackCooldown - (float)value) < 0.01f) return;
            Ai3D.AttackCooldown = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string WeaponName
    {
        get => WeaponState?.Name ?? "Pistola";
        set
        {
            if (WeaponState is null || WeaponState.Name == value) return;
            WeaponState.Name = string.IsNullOrWhiteSpace(value) ? "Arma" : value.Trim();
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponSlot
    {
        get => WeaponState?.Slot ?? 1;
        set
        {
            if (WeaponState is null || WeaponState.Slot == (int)value) return;
            WeaponState.Slot = Math.Clamp((int)value, 1, 3); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponMagazine
    {
        get => WeaponState?.MagazineSize ?? 0;
        set
        {
            if (WeaponState is null || WeaponState.MagazineSize == (int)value) return;
            WeaponState.MagazineSize = Math.Max(0, (int)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponReserve
    {
        get => WeaponState?.ReserveAmmo ?? -1;
        set
        {
            if (WeaponState is null || WeaponState.ReserveAmmo == (int)value) return;
            WeaponState.ReserveAmmo = (int)value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponReload
    {
        get => (decimal)(WeaponState?.ReloadSeconds ?? 1.2f);
        set
        {
            if (WeaponState is null || MathF.Abs(WeaponState.ReloadSeconds - (float)value) < 0.01f) return;
            WeaponState.ReloadSeconds = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool WeaponProjectile
    {
        get => WeaponState?.IsProjectile ?? false;
        set
        {
            if (WeaponState is null || WeaponState.IsProjectile == value) return;
            WeaponState.IsProjectile = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal WeaponProjectileSpeed
    {
        get => (decimal)(WeaponState?.ProjectileSpeed ?? 18f);
        set
        {
            if (WeaponState is null || MathF.Abs(WeaponState.ProjectileSpeed - (float)value) < 0.01f) return;
            WeaponState.ProjectileSpeed = MathF.Max(1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasRespawn => Entity.GetComponent<Respawn>() is not null;

    public Respawn? RespawnState => Entity.GetComponent<Respawn>();

    public decimal RespawnLives
    {
        get => RespawnState?.Lives ?? 3;
        set
        {
            if (RespawnState is null || RespawnState.Lives == (int)value) return;
            RespawnState.Lives = Math.Max(1, (int)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal RespawnSeconds
    {
        get => (decimal)(RespawnState?.RespawnSeconds ?? 1.5f);
        set
        {
            if (RespawnState is null || MathF.Abs(RespawnState.RespawnSeconds - (float)value) < 0.01f) return;
            RespawnState.RespawnSeconds = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasButton => Entity.GetComponent<Button3D>() is not null;

    public Button3D? ButtonState => Entity.GetComponent<Button3D>();

    public string ButtonTarget
    {
        get => ButtonState?.TargetName ?? string.Empty;
        set
        {
            if (ButtonState is null || ButtonState.TargetName == value) return;
            ButtonState.TargetName = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public int ButtonActionIndex
    {
        get => (int)(ButtonState?.Action ?? ActionKind.Message);
        set
        {
            if (ButtonState is null || (int)ButtonState.Action == value) return;
            ButtonState.Action = (ActionKind)Math.Clamp(value, 0, 6); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal ButtonRadius
    {
        get => (decimal)(ButtonState?.Radius ?? 2.5f);
        set
        {
            if (ButtonState is null || MathF.Abs(ButtonState.Radius - (float)value) < 0.01f) return;
            ButtonState.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string ButtonMessage
    {
        get => ButtonState?.Message ?? string.Empty;
        set
        {
            if (ButtonState is null || ButtonState.Message == value) return;
            ButtonState.Message = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasTrigger => Entity.GetComponent<Trigger3D>() is not null;

    public Trigger3D? TriggerState => Entity.GetComponent<Trigger3D>();

    public string TriggerTarget
    {
        get => TriggerState?.TargetName ?? string.Empty;
        set
        {
            if (TriggerState is null || TriggerState.TargetName == value) return;
            TriggerState.TargetName = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public int TriggerActionIndex
    {
        get => (int)(TriggerState?.Action ?? ActionKind.Message);
        set
        {
            if (TriggerState is null || (int)TriggerState.Action == value) return;
            TriggerState.Action = (ActionKind)Math.Clamp(value, 0, 6); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal TriggerRadius
    {
        get => (decimal)(TriggerState?.Radius ?? 2f);
        set
        {
            if (TriggerState is null || MathF.Abs(TriggerState.Radius - (float)value) < 0.01f) return;
            TriggerState.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string TriggerMessage
    {
        get => TriggerState?.Message ?? string.Empty;
        set
        {
            if (TriggerState is null || TriggerState.Message == value) return;
            TriggerState.Message = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool TriggerOnce
    {
        get => TriggerState?.Once ?? true;
        set
        {
            if (TriggerState is null || TriggerState.Once == value) return;
            TriggerState.Once = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasSpawner => Entity.GetComponent<Spawner3D>() is not null;

    public Spawner3D? SpawnerState => Entity.GetComponent<Spawner3D>();

    public string SpawnerTemplate
    {
        get => SpawnerState?.TemplateName ?? string.Empty;
        set
        {
            if (SpawnerState is null || SpawnerState.TemplateName == value) return;
            SpawnerState.TemplateName = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal SpawnerInterval
    {
        get => (decimal)(SpawnerState?.Interval ?? 4f);
        set
        {
            if (SpawnerState is null || MathF.Abs(SpawnerState.Interval - (float)value) < 0.01f) return;
            SpawnerState.Interval = MathF.Max(0.5f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal SpawnerMax
    {
        get => SpawnerState?.MaxAlive ?? 3;
        set
        {
            if (SpawnerState is null || SpawnerState.MaxAlive == (int)value) return;
            SpawnerState.MaxAlive = Math.Max(0, (int)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool SpawnerAuto
    {
        get => SpawnerState?.AutoStart ?? true;
        set
        {
            if (SpawnerState is null || SpawnerState.AutoStart == value) return;
            SpawnerState.AutoStart = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasCheckpoint => Entity.GetComponent<Checkpoint3D>() is not null;

    public Checkpoint3D? CheckpointState => Entity.GetComponent<Checkpoint3D>();

    public decimal CheckpointRadius
    {
        get => (decimal)(CheckpointState?.Radius ?? 2f);
        set
        {
            if (CheckpointState is null || MathF.Abs(CheckpointState.Radius - (float)value) < 0.01f) return;
            CheckpointState.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string CheckpointMessage
    {
        get => CheckpointState?.Message ?? string.Empty;
        set
        {
            if (CheckpointState is null || CheckpointState.Message == value) return;
            CheckpointState.Message = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool CheckpointHeal
    {
        get => CheckpointState?.Heal ?? true;
        set
        {
            if (CheckpointState is null || CheckpointState.Heal == value) return;
            CheckpointState.Heal = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    private int _visualRuleIndex;

    public bool HasVisualScript => Entity.GetComponent<VisualScript>() is not null;

    public VisualScript? VisualScriptState => Entity.GetComponent<VisualScript>();

    public ScriptRule? VisualRule => VisualScriptState?.GetRule(_visualRuleIndex);

    public int VisualRuleIndex
    {
        get => _visualRuleIndex;
        set
        {
            _visualRuleIndex = Math.Clamp(value, 0, 3);
            OnPropertyChanged();
            OnPropertyChanged(nameof(VisualEventIndex));
            OnPropertyChanged(nameof(VisualConditionIndex));
            OnPropertyChanged(nameof(VisualActionIndex));
            OnPropertyChanged(nameof(VisualTarget));
            OnPropertyChanged(nameof(VisualMessage));
            OnPropertyChanged(nameof(VisualRadius));
            OnPropertyChanged(nameof(VisualInterval));
            OnPropertyChanged(nameof(VisualOnce));
            OnPropertyChanged(nameof(VisualAmount));
            OnPropertyChanged(nameof(VisualConditionItem));
            OnPropertyChanged(nameof(VisualThreshold));
        }
    }

    public int VisualEventIndex
    {
        get => (int)(VisualRule?.Event ?? ScriptEvent.OnStart);
        set
        {
            if (VisualRule is null || (int)VisualRule.Event == value) return;
            VisualRule.Event = (ScriptEvent)Math.Clamp(value, 0, 4);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public int VisualConditionIndex
    {
        get => (int)(VisualRule?.Condition ?? ScriptCondition.None);
        set
        {
            if (VisualRule is null || (int)VisualRule.Condition == value) return;
            VisualRule.Condition = (ScriptCondition)Math.Clamp(value, 0, 2);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public int VisualActionIndex
    {
        get => (int)(VisualRule?.Action ?? ActionKind.Message);
        set
        {
            if (VisualRule is null || (int)VisualRule.Action == value) return;
            VisualRule.Action = (ActionKind)Math.Clamp(value, 0, 9);
            OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string VisualTarget
    {
        get => VisualRule?.TargetName ?? string.Empty;
        set
        {
            if (VisualRule is null || VisualRule.TargetName == value) return;
            VisualRule.TargetName = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string VisualMessage
    {
        get => VisualRule?.Message ?? string.Empty;
        set
        {
            if (VisualRule is null || VisualRule.Message == value) return;
            VisualRule.Message = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal VisualRadius
    {
        get => (decimal)(VisualRule?.Radius ?? 2.5f);
        set
        {
            if (VisualRule is null || MathF.Abs(VisualRule.Radius - (float)value) < 0.01f) return;
            VisualRule.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal VisualInterval
    {
        get => (decimal)(VisualRule?.Interval ?? 3f);
        set
        {
            if (VisualRule is null || MathF.Abs(VisualRule.Interval - (float)value) < 0.01f) return;
            VisualRule.Interval = MathF.Max(0.25f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool VisualOnce
    {
        get => VisualRule?.Once ?? true;
        set
        {
            if (VisualRule is null || VisualRule.Once == value) return;
            VisualRule.Once = value; OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal VisualAmount
    {
        get => (decimal)(VisualRule?.Amount ?? 20f);
        set
        {
            if (VisualRule is null || MathF.Abs(VisualRule.Amount - (float)value) < 0.01f) return;
            VisualRule.Amount = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public string VisualConditionItem
    {
        get => VisualRule?.ConditionItem ?? "llave";
        set
        {
            if (VisualRule is null || VisualRule.ConditionItem == value) return;
            VisualRule.ConditionItem = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal VisualThreshold
    {
        get => (decimal)(VisualRule?.Threshold ?? 50f);
        set
        {
            if (VisualRule is null || MathF.Abs(VisualRule.Threshold - (float)value) < 0.01f) return;
            VisualRule.Threshold = MathF.Max(0f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasHazard => Entity.GetComponent<Hazard3D>() is not null;

    public Hazard3D? HazardState => Entity.GetComponent<Hazard3D>();

    public decimal HazardRadius
    {
        get => (decimal)(HazardState?.Radius ?? 1.5f);
        set
        {
            if (HazardState is null || MathF.Abs(HazardState.Radius - (float)value) < 0.01f) return;
            HazardState.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal HazardDamage
    {
        get => (decimal)(HazardState?.DamagePerSecond ?? 20f);
        set
        {
            if (HazardState is null || MathF.Abs(HazardState.DamagePerSecond - (float)value) < 0.01f) return;
            HazardState.DamagePerSecond = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasTeleporter => Entity.GetComponent<Teleporter3D>() is not null;

    public Teleporter3D? TeleporterState => Entity.GetComponent<Teleporter3D>();

    public string TeleporterTarget
    {
        get => TeleporterState?.TargetName ?? string.Empty;
        set
        {
            if (TeleporterState is null || TeleporterState.TargetName == value) return;
            TeleporterState.TargetName = value.Trim(); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal TeleporterRadius
    {
        get => (decimal)(TeleporterState?.Radius ?? 1.5f);
        set
        {
            if (TeleporterState is null || MathF.Abs(TeleporterState.Radius - (float)value) < 0.01f) return;
            TeleporterState.Radius = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public bool HasFollower => Entity.GetComponent<Follower3D>() is not null;

    public Follower3D? FollowerState => Entity.GetComponent<Follower3D>();

    public decimal FollowerSpeed
    {
        get => (decimal)(FollowerState?.Speed ?? 2.5f);
        set
        {
            if (FollowerState is null || MathF.Abs(FollowerState.Speed - (float)value) < 0.01f) return;
            FollowerState.Speed = MathF.Max(0.1f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }

    public decimal FollowerDistance
    {
        get => (decimal)(FollowerState?.FollowDistance ?? 2f);
        set
        {
            if (FollowerState is null || MathF.Abs(FollowerState.FollowDistance - (float)value) < 0.01f) return;
            FollowerState.FollowDistance = MathF.Max(0.5f, (float)value); OnPropertyChanged(); _owner.MarkDirty();
        }
    }
}
