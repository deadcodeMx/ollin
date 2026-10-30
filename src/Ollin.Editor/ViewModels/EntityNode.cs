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

    public string Icon => Entity.GetComponent<Camera2D>() is not null ? "◉" : Entity.GetComponent<SpriteRenderer>() is not null ? "▣" : "◇";

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
        OnPropertyChanged(nameof(OscillationAmplitude));
        OnPropertyChanged(nameof(OscillationFrequency));
        OnPropertyChanged(nameof(ScriptPath));
        OnPropertyChanged(nameof(ScriptClass));
    }
}
