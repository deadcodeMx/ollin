using System.Numerics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.OpenGL;
using Avalonia.OpenGL.Controls;
using Avalonia.Threading;
using Ollin.Core.Diagnostics;
using Ollin.Core.Input;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Editor.ViewModels;
using Ollin.Rendering;
using Silk.NET.OpenGLES;

namespace Ollin.Editor.Controls;

/// <summary>
/// Superficie del viewport. Crea su propio contexto OpenGL sobre el control de
/// Avalonia y dibuja la escena con <see cref="Renderer2D"/>.
/// </summary>
public sealed class ViewportControl : OpenGlControlBase
{
    public static readonly DirectProperty<ViewportControl, bool> HasErrorProperty =
        AvaloniaProperty.RegisterDirect<ViewportControl, bool>(nameof(HasError), o => o.HasError);

    public static readonly DirectProperty<ViewportControl, string?> ErrorTextProperty =
        AvaloniaProperty.RegisterDirect<ViewportControl, string?>(nameof(ErrorText), o => o.ErrorText);

    private GL? _gl;
    private Renderer2D? _renderer;
    private MainViewModel? _viewModel;
    private DispatcherTimer? _timer;
    private int _frames;
    private DateTime _statsClock = DateTime.UtcNow;
    private bool _hasError;
    private volatile bool _textureReloadRequested;
    private string? _errorText;

    private GizmoHandle _gizmoHandle = GizmoHandle.None;
    private Vector2 _gizmoStartWorld;
    private Vector2 _gizmoCenter;
    private Vector2 _gizmoStartPosition;
    private Vector2 _gizmoStartScale = Vector2.One;
    private float _gizmoStartRotation;
    private float _gizmoStartAngle;
    private float _gizmoStartDistance = 1f;

    public ViewportControl()
    {
        Focusable = true;
        ClipToBounds = true;
    }

    public bool HasError => _hasError;

    /// <summary>
    /// Pide vaciar la caché de texturas en el próximo cuadro. Debe hacerse en el
    /// hilo de render, que es donde vive el contexto OpenGL.
    /// </summary>
    public void RequestTextureReload()
    {
        _textureReloadRequested = true;
        RequestNextFrameRendering();
    }

    public string? ErrorText => _errorText;

    public MainViewModel? ViewModel
    {
        get => _viewModel;
        set
        {
            if (ReferenceEquals(_viewModel, value))
            {
                return;
            }

            if (_viewModel is not null)
            {
                _viewModel.RedrawRequested -= OnRedrawRequested;
            }

            _viewModel = value;

            if (_viewModel is not null)
            {
                _viewModel.RedrawRequested += OnRedrawRequested;
            }

            RequestNextFrameRendering();
        }
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        _timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        _timer.Tick += (_, _) =>
        {
            if (IsVisible)
            {
                RequestNextFrameRendering();
            }
        };
        _timer.Start();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        _timer?.Stop();
        _timer = null;
        if (_viewModel is not null)
        {
            _viewModel.RedrawRequested -= OnRedrawRequested;
        }

        base.OnDetachedFromVisualTree(e);
    }

    protected override void OnOpenGlInit(GlInterface gl)
    {
        try
        {
            _gl = GL.GetApi(gl.GetProcAddress);
            _renderer = new Renderer2D(_gl)
            {
                ProjectRoot = _viewModel?.ProjectRoot ?? Environment.CurrentDirectory,
            };
            _renderer.Initialize();
            Log.Info($"OpenGL {gl.Version} · {gl.Renderer}", "Viewport");
        }
        catch (Exception exception)
        {
            SetError($"No se pudo inicializar OpenGL: {exception.Message}");
            Log.Error(exception, "Viewport");
        }
    }

    protected override void OnOpenGlDeinit(GlInterface gl)
    {
        _renderer?.Dispose();
        _renderer = null;
        _gl?.Dispose();
        _gl = null;
    }

    protected override void OnOpenGlLost()
    {
        _renderer?.Dispose();
        _renderer = null;
        _gl?.Dispose();
        _gl = null;
        base.OnOpenGlLost();
    }

    protected override void OnOpenGlRender(GlInterface gl, int fb)
    {
        if (_renderer is null || _viewModel is null)
        {
            return;
        }

        var (width, height) = GetPixelSize();
        if (width <= 1 || height <= 1)
        {
            return;
        }

        try
        {
            if (_textureReloadRequested)
            {
                _renderer.ClearTextureCache();
                _textureReloadRequested = false;
            }

            _renderer.ProjectRoot = _viewModel.ProjectRoot;

            if (_viewModel.ActiveCamera3D is { } camera3D)
            {
                _renderer.RenderFrame3D(_viewModel.Scene, camera3D, width, height);
            }
            else
            {
                _renderer.RenderFrame(
                    _viewModel.Scene,
                    _viewModel.CameraPosition,
                    _viewModel.ActiveCamera,
                    width,
                    height,
                    _viewModel.Selected?.Id,
                    _viewModel.ShowGrid);
            }

            if (_hasError)
            {
                SetError(null);
            }

            UpdateStats(width, height);
        }
        catch (Exception exception)
        {
            SetError($"Error de renderizado: {exception.Message}");
            Log.Error(exception, "Viewport");
        }
    }

    private void UpdateStats(int width, int height)
    {
        _frames++;
        var now = DateTime.UtcNow;
        var elapsed = (now - _statsClock).TotalSeconds;
        if (elapsed < 0.5)
        {
            return;
        }

        var fps = (int)(_frames / elapsed);
        var drawCalls = _renderer?.LastDrawCalls ?? 0;
        _frames = 0;
        _statsClock = now;

        if (Environment.GetEnvironmentVariable("OLLIN_DIAG") == "1" && _viewModel is not null)
        {
            var cam = _viewModel.ActiveCamera;
            try
            {
                File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "diag.txt"),
                    $"{DateTime.Now:HH:mm:ss.fff} vp={width}x{height} bounds={Bounds.Width:0}x{Bounds.Height:0} scaling={TopLevel.GetTopLevel(this)?.RenderScaling:0.##} cam=({_viewModel.CameraPosition.X:0.##},{_viewModel.CameraPosition.Y:0.##}) ortho={cam.OrthographicSize:0.##} zoom={cam.Zoom:0.##} quads={_renderer?.LastQuadCount.ToString() ?? "?"} entities={_viewModel.Scene.Entities.Count}{Environment.NewLine}");
            }
            catch
            {
                // diagnóstico best-effort
            }
        }

        Dispatcher.UIThread.Post(() => _viewModel?.SetStats(fps, drawCalls));
    }

    private void SetError(string? message)
    {
        _hasError = message is not null;
        _errorText = message;
        RaisePropertyChanged(HasErrorProperty, !_hasError, _hasError);
        RaisePropertyChanged(ErrorTextProperty, null, message);
    }

    private (int Width, int Height) GetPixelSize()
    {
        var scaling = TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0;
        return ((int)Math.Max(1, Bounds.Width * scaling), (int)Math.Max(1, Bounds.Height * scaling));
    }

    private (int Width, int Height, float Scaling) GetPixelSizeWithScaling()
    {
        var scaling = (float)(TopLevel.GetTopLevel(this)?.RenderScaling ?? 1.0);
        return ((int)Math.Max(1, Bounds.Width * scaling), (int)Math.Max(1, Bounds.Height * scaling), scaling);
    }

    /// <summary>Selecciona la entidad bajo un punto del viewport (coordenadas locales).</summary>
    public void SelectAt(Point position)
    {
        if (_viewModel is null)
        {
            return;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var camera = _viewModel.ActiveCamera;
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));
        var world = CameraMath.ScreenToWorld(_viewModel.CameraPosition, camera.OrthographicSize, camera.Zoom, width, height, screen);
        Diag($"press local=({position.X:0.#},{position.Y:0.#}) screen=({screen.X:0.#},{screen.Y:0.#}) vp={width}x{height} world=({world.X:0.##},{world.Y:0.##})");

        Entity? hit = null;
        var bestOrder = int.MinValue;
        foreach (var entity in _viewModel.Scene.Entities)
        {
            if (!entity.Active)
            {
                continue;
            }

            var sprite = entity.GetComponent<SpriteRenderer>();
            if (sprite is not { Visible: true } || sprite.SortingOrder < bestOrder)
            {
                continue;
            }

            if (Renderer2D.HitTest(_viewModel.Scene, entity, world))
            {
                hit = entity;
                bestOrder = sprite.SortingOrder;
            }
        }

        Diag($"hit={(hit?.Name ?? "nada")} order={bestOrder}");
        _viewModel.Select(hit);
        RequestNextFrameRendering();
    }

    /// <summary>Pinta el mosaico activo en la celda bajo el puntero.</summary>
    public void PaintAt(Point position) => _viewModel?.PaintTileAtWorld(ToWorld(position));

    /// <summary>Alimenta la posición del ratón para los scripts (solo durante la reproducción).</summary>
    public void UpdatePointerInput(Point position)
    {
        if (_viewModel?.IsPlaying != true)
        {
            return;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var camera = _viewModel.ActiveCamera;
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));
        var world = CameraMath.ScreenToWorld(_viewModel.CameraPosition, camera.OrthographicSize, camera.Zoom, width, height, screen);
        InputState.SetMousePosition(screen, world);
    }

    /// <summary>Alimenta el estado de un botón del ratón para los scripts.</summary>
    public void UpdateMouseButton(int button, bool down)
    {
        if (_viewModel?.IsPlaying == true)
        {
            InputState.SetMouseButton(button, down);
        }
    }

    /// <summary>Detecta qué parte del gizmo hay bajo el puntero (mover, rotar o escalar).</summary>
    public GizmoHandle HitGizmo(Point position)
    {
        if (_viewModel?.Selected is null || _viewModel.IsPlaying)
        {
            return GizmoHandle.None;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var camera = _viewModel.ActiveCamera;
        var cameraPosition = _viewModel.CameraPosition;
        var entity = _viewModel.Selected.Entity;
        var sprite = entity.GetComponent<SpriteRenderer>();
        var size = sprite?.Size ?? Vector2.One;
        var origin = sprite?.Origin ?? new Vector2(0.5f, 0.5f);
        var corners = Renderer2D.GetWorldCorners(_viewModel.Scene.GetWorldMatrix(entity), size, origin);

        var pointer = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));
        var screenCorners = new Vector2[4];
        for (var i = 0; i < 4; i++)
        {
            screenCorners[i] = CameraMath.WorldToScreen(cameraPosition, camera.OrthographicSize, camera.Zoom, width, height, corners[i]);
        }

        var topCenter = (corners[0] + corners[1]) * 0.5f;
        var bottomCenter = (corners[3] + corners[2]) * 0.5f;
        var up = topCenter - bottomCenter;
        up = up.LengthSquared() < 1e-6f ? Vector2.UnitY : Vector2.Normalize(up);
        var ppu = CameraMath.PixelsPerUnit(camera.OrthographicSize, camera.Zoom, height);
        var rotateWorld = topCenter + up * (26f / ppu);
        var rotateScreen = CameraMath.WorldToScreen(cameraPosition, camera.OrthographicSize, camera.Zoom, width, height, rotateWorld);

        if (Vector2.Distance(pointer, rotateScreen) <= 13f)
        {
            return GizmoHandle.Rotate;
        }

        var handles = new[]
        {
            GizmoHandle.ScaleTopLeft,
            GizmoHandle.ScaleTopRight,
            GizmoHandle.ScaleBottomRight,
            GizmoHandle.ScaleBottomLeft,
        };

        for (var i = 0; i < 4; i++)
        {
            if (Vector2.Distance(pointer, screenCorners[i]) <= 11f)
            {
                return handles[i];
            }
        }

        return Renderer2D.PointInQuad(pointer, screenCorners) ? GizmoHandle.Move : GizmoHandle.None;
    }

    /// <summary>Comienza a arrastrar el gizmo desde el puntero indicado.</summary>
    public bool BeginGizmo(GizmoHandle handle, Point position)
    {
        if (handle == GizmoHandle.None || _viewModel?.Selected is null)
        {
            return false;
        }

        var entity = _viewModel.Selected.Entity;
        _gizmoHandle = handle;
        _gizmoStartWorld = ToWorld(position);
        _gizmoCenter = _viewModel.Scene.GetWorldPosition(entity);
        _gizmoStartPosition = entity.Transform.Position;
        _gizmoStartScale = entity.Transform.Scale;
        _gizmoStartRotation = entity.Transform.Rotation;

        var offset = _gizmoStartWorld - _gizmoCenter;
        _gizmoStartAngle = MathF.Atan2(offset.Y, offset.X);
        _gizmoStartDistance = MathF.Max(0.0001f, offset.Length());
        return true;
    }

    /// <summary>Aplica el arrastre del gizmo (con ajuste opcional).</summary>
    public void UpdateGizmo(Point position, bool snap)
    {
        if (_gizmoHandle == GizmoHandle.None || _viewModel?.Selected is null)
        {
            return;
        }

        var entity = _viewModel.Selected.Entity;
        var world = ToWorld(position);

        switch (_gizmoHandle)
        {
            case GizmoHandle.Move:
            {
                var position2 = _gizmoStartPosition + (world - _gizmoStartWorld);
                if (snap)
                {
                    position2 = new Vector2(MathF.Round(position2.X * 4f) / 4f, MathF.Round(position2.Y * 4f) / 4f);
                }

                entity.Transform.Position = position2;
                break;
            }

            case GizmoHandle.Rotate:
            {
                var angle = MathF.Atan2(world.Y - _gizmoCenter.Y, world.X - _gizmoCenter.X);
                var rotation = _gizmoStartRotation + (angle - _gizmoStartAngle);
                if (snap)
                {
                    const float step = MathF.PI / 12f; // 15°
                    rotation = MathF.Round(rotation / step) * step;
                }

                entity.Transform.Rotation = rotation;
                break;
            }

            default:
            {
                var distance = Vector2.Distance(world, _gizmoCenter);
                var factor = distance / _gizmoStartDistance;
                if (snap)
                {
                    factor = MathF.Round(factor * 10f) / 10f;
                }

                factor = Math.Clamp(factor, 0.05f, 50f);
                entity.Transform.Scale = _gizmoStartScale * factor;
                break;
            }
        }

        _viewModel.NotifySceneEdited();
    }

    /// <summary>Termina el arrastre y registra un paso de deshacer.</summary>
    public void EndGizmo()
    {
        if (_gizmoHandle == GizmoHandle.None)
        {
            return;
        }

        var label = _gizmoHandle switch
        {
            GizmoHandle.Move => "Mover entidad",
            GizmoHandle.Rotate => "Rotar entidad",
            _ => "Escalar entidad",
        };

        _gizmoHandle = GizmoHandle.None;
        _viewModel?.CommitEdit(label);
    }

    /// <summary>Convierte un punto del viewport a coordenadas de mundo.</summary>
    public Vector2 ToWorld(Point position)
    {
        var (width, height, scaling) = GetPixelSizeWithScaling();
        var camera = _viewModel!.ActiveCamera;
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));
        return CameraMath.ScreenToWorld(_viewModel.CameraPosition, camera.OrthographicSize, camera.Zoom, width, height, screen);
    }

    /// <summary>Mueve la cámara con un desplazamiento en píxeles de pantalla.</summary>
    public void PanByDelta(double dx, double dy)
    {
        if (_viewModel is null)
        {
            return;
        }

        var (_, height) = GetPixelSize();
        _viewModel.PanCamera(new Vector2((float)dx, (float)dy), height);
        RequestNextFrameRendering();
    }

    /// <summary>Zoom anclado al punto indicado (coordenadas locales del viewport).</summary>
    public void ZoomAtPoint(Point position, double wheelDelta)
    {
        if (_viewModel is null || Math.Abs(wheelDelta) < 0.0001)
        {
            return;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));
        var factor = wheelDelta > 0 ? 1.15f : 1f / 1.15f;
        _viewModel.ZoomAt(screen, factor, width, height);
        RequestNextFrameRendering();
    }
    private void Diag(string message)
    {
        if (Environment.GetEnvironmentVariable("OLLIN_DIAG") != "1")
        {
            return;
        }

        try
        {
            File.AppendAllText(Path.Combine(AppContext.BaseDirectory, "diag.txt"), $"{DateTime.Now:HH:mm:ss.fff} {message}{Environment.NewLine}");
        }
        catch
        {
            // diagnóstico best-effort
        }
    }

    private void OnRedrawRequested() => RequestNextFrameRendering();
}
