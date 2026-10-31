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
    private bool _gizmo3DDragging;
    private Vector3 _gizmo3DStartPlane;
    private Vector2 _gizmo3DStartPosition;
    private Vector2 _gizmo3DStartWorld;
    private int _gizmo3DAxis;
    private float _gizmo3DStartYaw;
    private float _gizmo3DStartRotation;
    private float _gizmo3DStartAngle;
    private Vector2 _gizmo3DStartScreen;
    private float _gizmo3DStartDistance;
    private float _gizmo3DStartHeight;
    private Vector3 _gizmo3DStartWallSize;
    private Vector2 _gizmo3DStartSpriteSize;

    public Gizmo3DMode Gizmo3DMode { get; set; } = Gizmo3DMode.Move;

    private Brush3DMode _brush3DMode;

    public Brush3DMode Brush3DMode
    {
        get => _brush3DMode;
        set
        {
            _brush3DMode = value;
            BrushPreview = null;
            RequestNextFrameRendering();
        }
    }

    public BrushPreview3D? BrushPreview { get; set; }

    /// <summary>Actualiza la caja fantasma que sigue al puntero con el pincel activo.</summary>
    public void UpdateBrushPreview(Avalonia.Point position)
    {
        if (_viewModel is null || _viewModel.IsPlaying || _viewModel.ActiveCamera3D is not { } camera ||
            Brush3DMode == Brush3DMode.None)
        {
            BrushPreview = null;
            RequestNextFrameRendering();
            return;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));

        if (!RayPlane(camera.Eye, camera.RayDirection(screen, width, height), 0f, out var point))
        {
            BrushPreview = null;
            RequestNextFrameRendering();
            return;
        }

        var step = MathF.Max(0.1f, SnapStep);
        var x = MathF.Round(point.X / step) * step;
        var z = MathF.Round(point.Z / step) * step;

        if (Brush3DMode == Brush3DMode.Floor)
        {
            var size = MathF.Max(0.5f, _viewModel.BrushFloorSize);
            BrushPreview = new BrushPreview3D(new Vector3(x, 0.04f, z), new Vector3(size, 0.08f, size));
        }
        else
        {
            var wallWidth = MathF.Max(0.2f, _viewModel.BrushWidth);
            var wallHeight = MathF.Max(0.2f, _viewModel.BrushHeight);
            var wallDepth = MathF.Max(0.1f, _viewModel.BrushDepth);
            BrushPreview = new BrushPreview3D(
                new Vector3(x, wallHeight * 0.5f, z),
                new Vector3(wallWidth, wallHeight, wallDepth));
        }

        RequestNextFrameRendering();
    }

    public bool Snap3D { get; set; }

    public float SnapStep { get; set; } = 0.5f;

    /// <summary>Coloca una pared o suelo en el punto del plano del suelo bajo el puntero.</summary>
    public void PlaceBrush3D(Avalonia.Point position)
    {
        if (_viewModel is null || _viewModel.IsPlaying || _viewModel.ActiveCamera3D is not { } camera ||
            Brush3DMode == Brush3DMode.None)
        {
            return;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));

        if (!RayPlane(camera.Eye, camera.RayDirection(screen, width, height), 0f, out var point))
        {
            return;
        }

        var step = MathF.Max(0.1f, SnapStep);
        var snapped = new Vector2(
            MathF.Round(point.X / step) * step,
            MathF.Round(point.Z / step) * step);

        _viewModel.AddBrush3D(snapped, Brush3DMode == Brush3DMode.Floor);
    }

    /// <summary>Cambia el modo del gizmo 3D y redibuja.</summary>
    public void SetGizmo3DMode(Gizmo3DMode mode)
    {
        Gizmo3DMode = mode;
        RequestNextFrameRendering();
    }

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

    private void UpdateMouseLock()
    {
        if (TopLevel.GetTopLevel(this) is not Window window)
        {
            WindowsMouseLock.Disable();
            return;
        }

        var wantsLock = _viewModel is { IsPlaying: true, IsPaused: false, IsGameOverVisible: false } &&
                        window.IsActive && !WindowsMouseLock.Suspended;

        if (wantsLock)
        {
            WindowsMouseLock.Enable(window);
            WindowsMouseLock.Update(window);
        }
        else
        {
            WindowsMouseLock.Disable();
        }
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
                _renderer.RenderFrame3D(_viewModel.Scene, camera3D, width, height, _viewModel.IsPlaying ? null : _viewModel.Selected?.Id, Gizmo3DMode, _viewModel.IsPlaying ? null : BrushPreview);
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

    public bool IsGizmo3DDragging => _gizmo3DDragging;

    /// <summary>Selecciona una entidad del modo 2.5D lanzando un rayo desde la cámara.</summary>
    public void Select3DAt(Point position)
    {
        if (_viewModel?.ActiveCamera3D is not { } camera)
        {
            return;
        }

        var (width, height, scaling) = GetPixelSizeWithScaling();
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));
        var origin = camera.Eye;
        var direction = camera.RayDirection(screen, width, height);

        Entity? hit = null;
        var best = float.MaxValue;

        foreach (var entity in _viewModel.Scene.Entities)
        {
            if (!entity.Active)
            {
                continue;
            }

            if (entity.GetComponent<Wall3D>() is { Enabled: true, Visible: true } wall &&
                RayBox(origin, direction, wall.Bounds.Min, wall.Bounds.Max, out var t) && t < best)
            {
                hit = entity;
                best = t;
                continue;
            }

            if (entity.GetComponent<SpriteRenderer>() is { Enabled: true, Visible: true } sprite)
            {
                var spriteWorld = entity.Scene?.GetWorldPosition(entity) ?? entity.Transform.Position;
        var center = new Vector3(spriteWorld.X, sprite.Size.Y * 0.5f, spriteWorld.Y);

                if (!camera.Project(center, width, height, out var projected, out var depth))
                {
                    continue;
                }

                if (Vector2.Distance(screen, projected) <= 30f && depth < best)
                {
                    hit = entity;
                    best = depth;
                }
            }
        }

        _viewModel.Select(hit);
        RequestNextFrameRendering();
    }

    /// <summary>Comienza a arrastrar la entidad seleccionada sobre el plano 3D.</summary>
    public bool Begin3DDrag(Point position)
    {
        if (_viewModel?.Selected is null || _viewModel.ActiveCamera3D is not { } camera)
        {
            return false;
        }

        var entity = _viewModel.Selected.Entity;
        var (width, height, scaling) = GetPixelSizeWithScaling();
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));

        if (!TryScreenCenter(entity, camera, width, height, out var centerScreen))
        {
            return false;
        }

        _gizmo3DStartWallSize = entity.GetComponent<Wall3D>()?.Size ?? Vector3.One;
        _gizmo3DStartSpriteSize = entity.GetComponent<SpriteRenderer>()?.Size ?? Vector2.One;
        _gizmo3DStartHeight = entity.GetComponent<Camera3D>()?.Height ?? 1.7f;
        _gizmo3DStartYaw = entity.GetComponent<Wall3D>()?.Yaw ?? entity.GetComponent<Camera3D>()?.Yaw ?? 0f;
        _gizmo3DStartRotation = entity.Transform.Rotation;
        _gizmo3DStartDistance = MathF.Max(20f, Vector2.Distance(screen, centerScreen));

        switch (Gizmo3DMode)
        {
            case Gizmo3DMode.Rotate:


                _gizmo3DStartAngle = MathF.Atan2(screen.Y - centerScreen.Y, screen.X - centerScreen.X);
                break;

            case Gizmo3DMode.Scale:
            {
                var handle = HitAxisHandle(entity, camera, width, height, screen, includeY: true);

                _gizmo3DAxis = handle ?? -1;
                _gizmo3DStartDistance = MathF.Max(160f, Vector2.Distance(screen, centerScreen));
                break;
            }

            default:
            {
                var handle = HitAxisHandle(entity, camera, width, height, screen, includeY: false);
                _gizmo3DAxis = handle switch { 0 => 1, 2 => 2, _ => 0 };
                var planeY = entity.GetComponent<Wall3D>()?.BaseY ?? 0f;

                if (!RayPlane(camera.Eye, camera.RayDirection(screen, width, height), planeY, out var point))
                {
                    return false;
                }

                _gizmo3DStartPlane = point;
                break;
            }
        }

        _gizmo3DStartPosition = entity.Transform.Position;
        _gizmo3DStartWorld = _viewModel.Scene.GetWorldPosition(entity);
        _gizmo3DStartScreen = screen;
        _gizmo3DDragging = true;
        return true;
    }

    public void Update3DDrag(Point position, bool snap)
    {
        if (!_gizmo3DDragging || _viewModel?.Selected is null || _viewModel.ActiveCamera3D is not { } camera)
        {
            return;
        }

        var entity = _viewModel.Selected.Entity;
        var (width, height, scaling) = GetPixelSizeWithScaling();
        var screen = new Vector2((float)(position.X * scaling), (float)(position.Y * scaling));

        if (!TryScreenCenter(entity, camera, width, height, out var centerScreen))
        {
            return;
        }

        switch (Gizmo3DMode)
        {
            case Gizmo3DMode.Rotate:
            {
                // Arrastre horizontal: predecible incluso si el centro queda lejos.
                var delta = (screen.X - _gizmo3DStartScreen.X) * 0.006f;
                var rotation = _gizmo3DStartYaw + delta;

                if (snap || Snap3D)
                {
                    const float step = MathF.PI / 12f;
                    rotation = MathF.Round(rotation / step) * step;
                }

                if (entity.GetComponent<Wall3D>() is { } wall)
                {
                    wall.Yaw = rotation;
                }
                else if (entity.GetComponent<Camera3D>() is { } cam)
                {
                    cam.Yaw = rotation;
                }
                else
                {
                    entity.Transform.Rotation = _gizmo3DStartRotation + delta;
                }

                break;
            }

            case Gizmo3DMode.Scale:
            {
                var factor = 1f + (screen.X - _gizmo3DStartScreen.X) / 200f;

                if (snap || Snap3D)
                {
                    factor = MathF.Round(factor * 10f) / 10f;
                }

                factor = Math.Clamp(factor, 0.05f, 20f);

                if (entity.GetComponent<Wall3D>() is { } wall)
                {
                    var size = _gizmo3DStartWallSize;

                    if (_gizmo3DAxis == 0) size.X *= factor;
                    else if (_gizmo3DAxis == 1) size.Y *= factor;
                    else if (_gizmo3DAxis == 2) size.Z *= factor;
                    else size *= factor;

                    wall.Size = size;
                }
                else if (entity.GetComponent<SpriteRenderer>() is { } sprite)
                {
                    sprite.Size = _gizmo3DStartSpriteSize * factor;
                }
                else if (entity.GetComponent<Camera3D>() is { } cam)
                {
                    cam.Height = _gizmo3DStartHeight * factor;
                }

                break;
            }

            default:
            {
                var planeY = entity.GetComponent<Wall3D>()?.BaseY ?? 0f;

                if (!RayPlane(camera.Eye, camera.RayDirection(screen, width, height), planeY, out var point))
                {
                    return;
                }

                var delta = new Vector2(point.X - _gizmo3DStartPlane.X, point.Z - _gizmo3DStartPlane.Z);
                var next = _gizmo3DStartWorld + delta;

                if (snap || Snap3D)
                {
                    var step = MathF.Max(0.1f, SnapStep);
                    next = new Vector2(MathF.Round(next.X / step) * step, MathF.Round(next.Y / step) * step);
                }

                if (_gizmo3DAxis == 1)
                {
                    next.Y = _gizmo3DStartWorld.Y;
                }
                else if (_gizmo3DAxis == 2)
                {
                    next.X = _gizmo3DStartWorld.X;
                }

                _viewModel.Scene.SetWorldPosition(entity, next);
                break;
            }
        }

        _viewModel.NotifySceneEdited();
        RequestNextFrameRendering();
    }

    public void End3DDrag()
    {
        if (!_gizmo3DDragging)
        {
            return;
        }

        _gizmo3DDragging = false;
        var label = Gizmo3DMode switch
        {
            Gizmo3DMode.Rotate => "Rotar entidad 3D",
            Gizmo3DMode.Scale => "Escalar entidad 3D",
            _ => "Mover entidad 3D",
        };

        _viewModel?.CommitEdit(label);
    }

    private static float NormalizeAngle(float angle)
    {
        while (angle > MathF.PI) angle -= MathF.Tau;
        while (angle < -MathF.PI) angle += MathF.Tau;
        return angle;
    }

    private static bool TryScreenCenter(Entity entity, Camera3D camera, int width, int height, out Vector2 screen)
    {
        var center = EntityCenter3D(entity);
        return camera.Project(center, width, height, out screen, out _);
    }

    private static Vector3 EntityCenter3D(Entity entity)
    {
        var world = entity.Scene?.GetWorldPosition(entity) ?? entity.Transform.Position;

        return entity.GetComponent<Wall3D>() is { } wall
            ? wall.Center
            : entity.GetComponent<SpriteRenderer>() is { } sprite
                ? new Vector3(world.X, sprite.Size.Y * 0.5f, world.Y)
                : entity.GetComponent<Camera3D>() is { } cam
                    ? cam.Eye
                    : new Vector3(world.X, 0f, world.Y);
    }

    private static bool IsNearRing(Entity entity, Camera3D camera, int width, int height, Vector2 screen, out float distance)
    {
        distance = float.MaxValue;
        var center = EntityCenter3D(entity);
        var previous = Vector2.Zero;
        var hasPrevious = false;

        for (var i = 0; i <= 48; i++)
        {
            var angle = i / 48f * MathF.Tau;
            var point = center + new Vector3(MathF.Cos(angle) * 1.3f, 0f, MathF.Sin(angle) * 1.3f);

            if (!camera.Project(point, width, height, out var projected, out _))
            {
                hasPrevious = false;
                continue;
            }

            if (hasPrevious)
            {
                distance = MathF.Min(distance, DistanceToSegment(screen, previous, projected));
            }

            previous = projected;
            hasPrevious = true;
        }

        return distance <= 12f;
    }

    private static int? HitAxisHandle(Entity entity, Camera3D camera, int width, int height, Vector2 screen, bool includeY)
    {
        if (!TryScreenCenter(entity, camera, width, height, out var centerScreen))
        {
            return null;
        }

        if (Vector2.Distance(screen, centerScreen) <= 16f)
        {
            return -1;
        }

        var center = EntityCenter3D(entity);
        var axis = 1.2f;

        for (var i = 0; i < 3; i++)
        {
            if (i == 1 && !includeY)
            {
                continue;
            }

            var direction = i switch
            {
                0 => new Vector3(axis, 0f, 0f),
                1 => new Vector3(0f, axis, 0f),
                _ => new Vector3(0f, 0f, axis),
            };

            if (!camera.Project(center + direction, width, height, out var tip, out _))
            {
                continue;
            }

            if (DistanceToSegment(screen, centerScreen, tip) <= 10f)
            {
                return i;
            }
        }

        return null;
    }

    private static float DistanceToSegment(Vector2 point, Vector2 a, Vector2 b)
    {
        var ab = b - a;
        var lengthSquared = ab.LengthSquared();

        if (lengthSquared < 0.0001f)
        {
            return Vector2.Distance(point, a);
        }

        var t = Math.Clamp(Vector2.Dot(point - a, ab) / lengthSquared, 0f, 1f);
        return Vector2.Distance(point, a + ab * t);
    }
    private static bool RayPlane(Vector3 origin, Vector3 direction, float planeY, out Vector3 point)
    {
        point = Vector3.Zero;

        if (MathF.Abs(direction.Y) < 0.0001f)
        {
            return false;
        }

        var t = (planeY - origin.Y) / direction.Y;

        if (t < 0f)
        {
            return false;
        }

        point = origin + direction * t;
        return true;
    }

    private static bool RayBox(Vector3 origin, Vector3 direction, Vector3 min, Vector3 max, out float distance)
    {
        distance = 0f;
        var near = 0f;
        var far = float.PositiveInfinity;

        for (var axis = 0; axis < 3; axis++)
        {
            var o = axis == 0 ? origin.X : axis == 1 ? origin.Y : origin.Z;
            var d = axis == 0 ? direction.X : axis == 1 ? direction.Y : direction.Z;
            var lo = axis == 0 ? min.X : axis == 1 ? min.Y : min.Z;
            var hi = axis == 0 ? max.X : axis == 1 ? max.Y : max.Z;

            if (MathF.Abs(d) < 0.000001f)
            {
                if (o < lo || o > hi)
                {
                    return false;
                }

                continue;
            }

            var t0 = (lo - o) / d;
            var t1 = (hi - o) / d;

            if (t0 > t1)
            {
                (t0, t1) = (t1, t0);
            }

            near = MathF.Max(near, t0);
            far = MathF.Min(far, t1);

            if (near > far)
            {
                return false;
            }
        }

        distance = near;
        return true;
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
