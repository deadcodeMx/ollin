using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Numerics;
using Avalonia.Threading;
using Ollin.Core.Assets;
using Ollin.Core.Diagnostics;
using Ollin.Core.History;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;
using Ollin.Runtime;

namespace Ollin.Editor.ViewModels;

/// <summary>Estado completo del editor: documento de escena, selección, cámara y consola.</summary>
public sealed class MainViewModel : ObservableObject
{
    private readonly Camera2D _fallbackCamera = new();
    private Vector2 _fallbackCameraPosition;
    private Scene _scene = new();
    private EntityNode? _selected;
    private bool _showGrid = true;
    private string _statusMessage = "Listo";
    private string? _scenePath;
    private bool _isDirty;
    private int _fps;
    private int _drawCalls;
    private string _projectRoot = Environment.CurrentDirectory;
    private Vector2 _homeCameraPosition;
    private float _homeZoom = 1f;

    private readonly string _defaultProjectRoot = ProjectLocator.FindProjectRoot() ?? Environment.CurrentDirectory;
    private readonly SceneHistory _history = new();
    private readonly SceneRuntime _runtime = new();
    private string? _lastEditKey;
    private DateTime _lastEditTime;
    private bool _restoring;
    private readonly Stopwatch _playClock = new();
    private DispatcherTimer? _playTimer;
    private Vector2 _cameraBeforePlay;
    private float _zoomBeforePlay;
    private bool _dirtyBeforePlay;
    private Guid? _selectedBeforePlay;
    private AssetItem? _selectedAsset;

    public MainViewModel()
    {
        Log.Written += OnLogWritten;
        foreach (var entry in Log.History)
        {
            Logs.Add(new LogItem(entry));
        }

        LoadScene(DefaultScene.Create(), null);
        RefreshAssets(logToConsole: false);
        Log.Info("Ollin 0.1 — editor 2D listo", "Editor");
        Log.Info("Clic para seleccionar · Rueda para zoom · Alt+arrastrar para mover la cámara", "Editor");
    }

    /// <summary>Se dispara cuando algo visual cambió y el viewport debe redibujarse.</summary>
    public event Action? RedrawRequested;

    public ObservableCollection<EntityNode> Roots { get; } = new();

    public ObservableCollection<LogItem> Logs { get; } = new();

    public ObservableCollection<AssetItem> Assets { get; } = new();

    /// <summary>Se dispara cuando cambian archivos de textura y hay que vaciar la caché de GPU.</summary>
    public event Action? TexturesReloadRequested;

    public AssetItem? SelectedAsset
    {
        get => _selectedAsset;
        set
        {
            if (SetProperty(ref _selectedAsset, value))
            {
                OnPropertyChanged(nameof(HasSelectedAsset));
            }
        }
    }

    public bool HasSelectedAsset => SelectedAsset is not null;

    public bool CanUndo => _history.CanUndo;

    public bool CanRedo => _history.CanRedo;

    public string UndoText => _history.UndoLabel is { } label ? $"Deshacer: {label}" : "Deshacer";

    public string RedoText => _history.RedoLabel is { } label ? $"Rehacer: {label}" : "Rehacer";

    public string AssetCountText => Assets.Count == 1 ? "1 textura" : $"{Assets.Count} texturas";

    public Scene Scene
    {
        get => _scene;
        private set
        {
            _scene = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Title));
        }
    }

    public EntityNode? Selected
    {
        get => _selected;
        set
        {
            if (!SetProperty(ref _selected, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasSelection));
            OnPropertyChanged(nameof(HasNoSelection));
            OnPropertyChanged(nameof(SelectionText));
            RequestRedraw();
        }
    }

    public bool HasSelection => Selected is not null;

    public bool HasNoSelection => Selected is null;

    public bool ShowGrid
    {
        get => _showGrid;
        set
        {
            if (SetProperty(ref _showGrid, value))
            {
                RequestRedraw();
            }
        }
    }

    public string StatusMessage
    {
        get => _statusMessage;
        set => SetProperty(ref _statusMessage, value);
    }

    public string? ScenePath
    {
        get => _scenePath;
        private set
        {
            if (SetProperty(ref _scenePath, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(ScenePathText));
            }
        }
    }

    public bool IsDirty
    {
        get => _isDirty;
        private set
        {
            if (SetProperty(ref _isDirty, value))
            {
                OnPropertyChanged(nameof(Title));
                OnPropertyChanged(nameof(DirtyMarker));
            }
        }
    }

    public string DirtyMarker => IsDirty ? " ●" : string.Empty;

    public string Title => $"Ollin — {(ScenePath is null ? Scene.Name : System.IO.Path.GetFileName(ScenePath))}{DirtyMarker}";

    public string ScenePathText => ScenePath is null ? "Sin guardar" : ScenePath;

    public string SelectionText => Selected is null ? "Nada seleccionado" : Selected.Entity.Name;

    public string EntityCountText => $"{Scene.Entities.Count} entidades";

    public int Fps
    {
        get => _fps;
        private set => SetProperty(ref _fps, value);
    }

    public int DrawCalls
    {
        get => _drawCalls;
        private set => SetProperty(ref _drawCalls, value);
    }

    public string ZoomText => $"{ActiveCamera.Zoom * 100f:0}%";

    public string ProjectRoot
    {
        get => _projectRoot;
        set => SetProperty(ref _projectRoot, value);
    }

    /// <summary>Cámara activa: la de la escena o una de respaldo si no existe.</summary>
    public Camera2D ActiveCamera => Scene.GetMainCamera() ?? _fallbackCamera;

    public Vector2 CameraPosition
    {
        get
        {
            var cameraEntity = GetCameraEntity();
            return cameraEntity is null ? _fallbackCameraPosition : Scene.GetWorldPosition(cameraEntity);
        }
    }

    // ---------- Modo juego (etapa 2) ----------

    public bool IsPlaying => _runtime.IsPlaying;

    public bool IsPaused => _runtime.IsPaused;

    /// <summary>Mientras se reproduce no se edita la escena.</summary>
    public bool CanEdit => !_runtime.IsPlaying;

    public string PlayStateText => !IsPlaying ? "Detenido" : IsPaused ? "Pausado" : "Reproduciendo";

    public string ElapsedText => TimeSpan.FromSeconds(_runtime.Elapsed).ToString(@"mm\:ss\.f");

    /// <summary>Reproduce o detiene según el estado actual (F5).</summary>
    public void TogglePlay()
    {
        if (IsPlaying)
        {
            StopPlay();
        }
        else
        {
            StartPlay();
        }
    }

    public void TogglePause()
    {
        if (!IsPlaying)
        {
            return;
        }

        _runtime.TogglePause();
        StatusMessage = IsPaused ? "Reproducción en pausa" : "Reproduciendo la escena";
        NotifyPlayState();
    }

    private void StartPlay()
    {
        _cameraBeforePlay = CameraPosition;
        _zoomBeforePlay = ActiveCamera.Zoom;
        _dirtyBeforePlay = IsDirty;
        _selectedBeforePlay = Selected?.Id;

        _runtime.Play(Scene);
        _playClock.Restart();
        _playTimer ??= CreatePlayTimer();
        _playTimer.Start();

        StatusMessage = "Reproduciendo la escena";
        NotifyPlayState();
        RequestRedraw();
    }

    private void StopPlay()
    {
        _playTimer?.Stop();
        _playClock.Stop();

        var restored = _runtime.Stop();
        if (restored is null)
        {
            NotifyPlayState();
            return;
        }

        LoadScene(restored, ScenePath);
        IsDirty = _dirtyBeforePlay;
        MoveCamera(_cameraBeforePlay);
        ActiveCamera.Zoom = _zoomBeforePlay;
        _homeZoom = _zoomBeforePlay;

        if (_selectedBeforePlay is { } id)
        {
            Select(Scene.Find(id));
        }

        StatusMessage = "Detenido; escena restaurada";
        OnPropertyChanged(nameof(ZoomText));
        NotifyPlayState();
        RequestRedraw();
    }

    private DispatcherTimer CreatePlayTimer()
    {
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
        timer.Tick += (_, _) =>
        {
            var delta = (float)_playClock.Elapsed.TotalSeconds;
            _playClock.Restart();
            _runtime.Tick(delta);
            OnPropertyChanged(nameof(ElapsedText));
            RequestRedraw();
        };
        return timer;
    }

    private void NotifyPlayState()
    {
        OnPropertyChanged(nameof(IsPlaying));
        OnPropertyChanged(nameof(IsPaused));
        OnPropertyChanged(nameof(CanEdit));
        OnPropertyChanged(nameof(PlayStateText));
        OnPropertyChanged(nameof(ElapsedText));
    }
    public void RequestRedraw() => RedrawRequested?.Invoke();

    public void LoadScene(Scene scene, string? path)
    {
        if (_runtime.IsPlaying)
        {
            _playTimer?.Stop();
            _runtime.Stop();
            NotifyPlayState();
        }

        Scene = scene;
        ScenePath = path;
        ProjectRoot = path is null ? _defaultProjectRoot : System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? _defaultProjectRoot;
        IsDirty = false;
        RebuildTree();
        Selected = null;
        _homeCameraPosition = CameraPosition;
        _homeZoom = ActiveCamera.Zoom;
        ResetCamera();
        if (!_restoring)
        {
            _history.Initialize(scene);
            NotifyHistory();
        }

        OnPropertyChanged(nameof(EntityCountText));
        OnPropertyChanged(nameof(Title));
        RefreshAssets(logToConsole: false);
        RequestRedraw();
    }

    // ---------- Historial (deshacer / rehacer) ----------

    /// <summary>Marca la escena como editada sin crear un paso de historial (arrastres en curso).</summary>
    public void NotifySceneEdited()
    {
        IsDirty = true;
        Selected?.RefreshAll();
        RequestRedraw();
    }

    /// <summary>Registra un paso de deshacer con el estado actual de la escena.</summary>
    public void CommitEdit(string label)
    {
        if (_restoring)
        {
            return;
        }

        var json = SceneSerializer.Serialize(Scene);
        if (json == _history.Current?.Json)
        {
            return;
        }

        _lastEditKey = null;
        _history.Record(json, label);
        NotifyHistory();
    }

    /// <summary>Agrupa ediciones continuas del mismo campo en un solo paso.</summary>
    private void CommitEditDebounced(string label, string key)
    {
        if (_restoring)
        {
            return;
        }

        var json = SceneSerializer.Serialize(Scene);
        if (json == _history.Current?.Json)
        {
            return;
        }

        var now = DateTime.UtcNow;
        if (key == _lastEditKey && (now - _lastEditTime).TotalMilliseconds < 800)
        {
            _history.ReplaceCurrent(json);
        }
        else
        {
            _history.Record(json, label);
        }

        _lastEditKey = key;
        _lastEditTime = now;
        NotifyHistory();
    }

    public void Undo()
    {
        if (_restoring || IsPlaying)
        {
            return;
        }

        var action = _history.Current?.Label ?? "edición";
        var entry = _history.Undo();
        if (entry is null)
        {
            return;
        }

        RestoreHistory(entry, $"Deshecho: {action}");
    }

    public void Redo()
    {
        if (_restoring || IsPlaying)
        {
            return;
        }

        var action = _history.RedoLabel;
        var entry = _history.Redo();
        if (entry is null)
        {
            return;
        }

        RestoreHistory(entry, $"Rehecho: {action}");
    }

    private void RestoreHistory(HistoryEntry entry, string verb)
    {
        _restoring = true;
        try
        {
            var camera = CameraPosition;
            var zoom = ActiveCamera.Zoom;
            var selection = Selected?.Id;
            var path = ScenePath;

            var scene = SceneSerializer.Deserialize(entry.Json);
            LoadScene(scene, path);
            MoveCamera(camera);
            ActiveCamera.Zoom = zoom;
            if (selection is { } id)
            {
                Select(Scene.Find(id));
            }

            IsDirty = true;
            OnPropertyChanged(nameof(ZoomText));
            StatusMessage = verb;

        }
        finally
        {
            _restoring = false;
        }

        NotifyHistory();
        RequestRedraw();
    }

    private void NotifyHistory()
    {
        OnPropertyChanged(nameof(CanUndo));
        OnPropertyChanged(nameof(CanRedo));
        OnPropertyChanged(nameof(UndoText));
        OnPropertyChanged(nameof(RedoText));
    }

    // ---------- Assets ----------

    /// <summary>Reindexa las texturas del proyecto.</summary>
    public void RefreshAssets(bool logToConsole = true)
    {
        Assets.Clear();
        foreach (var info in AssetDatabase.ScanTextures(ProjectRoot))
        {
            Assets.Add(new AssetItem(info));
        }

        if (SelectedAsset is not null && !Assets.Contains(SelectedAsset))
        {
            SelectedAsset = null;
        }

        OnPropertyChanged(nameof(AssetCountText));

        if (logToConsole)
        {
            Log.Info($"Assets actualizados: {Assets.Count} texturas", "Assets");
        }
    }

    /// <summary>Copia una imagen al proyecto y la deja lista para asignar.</summary>
    public void ImportTexture(string sourcePath)
    {
        try
        {
            var relative = AssetDatabase.ImportTexture(ProjectRoot, sourcePath);
            RefreshAssets(logToConsole: false);
            SelectedAsset = Assets.FirstOrDefault(a => a.RelativePath == relative) ?? Assets.FirstOrDefault(a => a.Name == System.IO.Path.GetFileName(relative));
            Log.Info($"Textura importada: {relative}", "Assets");
            TexturesReloadRequested?.Invoke();
            StatusMessage = $"Textura importada: {System.IO.Path.GetFileName(relative)}";
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo importar la textura: {exception.Message}", "Assets");
        }
    }

    /// <summary>Asigna la textura seleccionada al sprite de la entidad seleccionada.</summary>
    public void AssignAssetToSelected()
    {
        if (IsPlaying)
        {
            return;
        }

        if (Selected is null)
        {
            Log.Warn("Selecciona una entidad para asignarle la textura", "Assets");
            return;
        }

        if (SelectedAsset is null)
        {
            Log.Warn("Selecciona una textura en el panel de assets", "Assets");
            return;
        }

        if (Selected.Sprite is null)
        {
            Log.Warn($"'{Selected.Name}' no tiene un componente SpriteRenderer", "Assets");
            return;
        }

        Selected.AssignTexture(SelectedAsset);
        TexturesReloadRequested?.Invoke();
        StatusMessage = $"Textura asignada a {Selected.Name}";
        RequestRedraw();
    }

    public void RequestTextureReload()
    {
        TexturesReloadRequested?.Invoke();
        RequestRedraw();
    }

    public void NewScene()
    {
        var scene = new Scene { Name = "Escena nueva" };
        var camera = scene.Create("Cámara principal", Vector2.Zero);
        camera.AddComponent(new Camera2D());
        LoadScene(scene, null);
        Log.Info("Escena nueva creada", "Editor");
    }

    public void OpenScene(string path)
    {
        try
        {
            var scene = SceneSerializer.Load(path);
            LoadScene(scene, path);
            Log.Info($"Escena cargada: {path}", "Editor");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo abrir la escena: {exception.Message}", "Editor");
            StatusMessage = "Error al abrir la escena";
        }
    }

    public void SaveScene(string path)
    {
        try
        {
            SceneSerializer.Save(Scene, path);
            ScenePath = path;
            IsDirty = false;
            ProjectRoot = System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? ProjectRoot;
            Log.Info($"Escena guardada: {path}", "Editor");
            StatusMessage = "Escena guardada";
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar la escena: {exception.Message}", "Editor");
            StatusMessage = "Error al guardar";
        }
    }

    public void AddEntity(bool withSprite)
    {
        if (IsPlaying)
        {
            return;
        }

        var entity = Scene.Create("Entidad", CameraPosition);
        if (withSprite)
        {
            entity.AddComponent(new SpriteRenderer
            {
                Size = new Vector2(1f, 1f),
                Tint = new Vector4(0.75f, 0.78f, 0.85f, 1f),
            });
        }

        RebuildTree();
        Select(entity);
        IsDirty = true;
        CommitEdit("Crear entidad");
        OnPropertyChanged(nameof(EntityCountText));
        Log.Info($"Entidad creada: {entity.Name}", "Editor");
        RequestRedraw();
    }

    public void DeleteSelected()
    {
        if (IsPlaying || Selected is null)
        {
            return;
        }

        if (Selected.HasCamera && Scene.Entities.Count(e => e.GetComponent<Camera2D>() is not null) == 1)
        {
            Log.Warn("No se puede eliminar la única cámara de la escena", "Editor");
            return;
        }

        var name = Selected.Entity.Name;
        Scene.Remove(Selected.Entity.Id);
        Selected = null;
        RebuildTree();
        IsDirty = true;
        CommitEdit("Eliminar entidad");
        OnPropertyChanged(nameof(EntityCountText));
        Log.Info($"Entidad eliminada: {name}", "Editor");
        RequestRedraw();
    }

    public void DuplicateSelected()
    {
        if (IsPlaying || Selected is null)
        {
            return;
        }

        var original = Selected.Entity;
        var copy = original.Clone();
        copy.Name = $"{original.Name} (copia)";
        copy.Transform.Position += new Vector2(0.4f, -0.4f);
        Scene.Add(copy);
        RebuildTree();
        Select(copy);
        IsDirty = true;
        CommitEdit("Duplicar entidad");
        OnPropertyChanged(nameof(EntityCountText));
        Log.Info($"Entidad duplicada: {copy.Name}", "Editor");
        RequestRedraw();
    }

    public void Select(Entity? entity)
    {
        if (entity is null)
        {
            Selected = null;
            return;
        }

        Selected = FindNode(entity.Id);
    }

    public void ResetCamera()
    {
        var camera = ActiveCamera;
        MoveCamera(_homeCameraPosition);
        camera.Zoom = _homeZoom;
        OnPropertyChanged(nameof(ZoomText));
        RequestRedraw();
    }

    public void FrameSelected()
    {
        if (Selected is null)
        {
            ResetCamera();
            return;
        }

        var target = Scene.GetWorldPosition(Selected.Entity);
        MoveCamera(target);
        StatusMessage = $"Centrado en {Selected.Entity.Name}";
        RequestRedraw();
    }

    public void PanCamera(Vector2 screenDelta, int viewportHeight)
    {
        var camera = ActiveCamera;
        var ppu = CameraMath.PixelsPerUnit(camera.OrthographicSize, camera.Zoom, viewportHeight);
        var worldDelta = new Vector2(-screenDelta.X, screenDelta.Y) / MathF.Max(1f, ppu);
        MoveCamera(CameraPosition + worldDelta);
    }

    /// <summary>Zoom manteniendo fijo el punto de mundo bajo el cursor.</summary>
    public void ZoomAt(Vector2 screenPoint, float factor, int width, int height)
    {
        var camera = ActiveCamera;
        var before = CameraMath.ScreenToWorld(CameraPosition, camera.OrthographicSize, camera.Zoom, width, height, screenPoint);
        camera.Zoom = Math.Clamp(camera.Zoom * factor, 0.05f, 40f);
        var after = CameraMath.ScreenToWorld(CameraPosition, camera.OrthographicSize, camera.Zoom, width, height, screenPoint);
        MoveCamera(CameraPosition + (before - after));
        OnPropertyChanged(nameof(ZoomText));
        RequestRedraw();
    }

    public void SetStats(int fps, int drawCalls)
    {
        Fps = fps;
        DrawCalls = drawCalls;
    }

    public void MarkDirty([CallerMemberName] string? editKey = null)
    {
        IsDirty = true;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(EntityCountText));

        if (!string.IsNullOrEmpty(editKey))
        {
            CommitEditDebounced($"Editar {editKey}", editKey);
        }
    }

    public Entity? GetCameraEntity() =>
        Scene.Entities.FirstOrDefault(e => e.GetComponent<Camera2D>() is { IsMain: true });

    private void MoveCamera(Vector2 worldPosition)
    {
        var cameraEntity = GetCameraEntity();
        if (cameraEntity is not null)
        {
            cameraEntity.Transform.Position = worldPosition;
        }
        else
        {
            _fallbackCameraPosition = worldPosition;
        }
    }

    private void RebuildTree()
    {
        Roots.Clear();
        foreach (var root in Scene.RootEntities.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            Roots.Add(CreateNode(root));
        }
    }

    private EntityNode CreateNode(Entity entity)
    {
        var node = new EntityNode(entity, this);
        foreach (var child in Scene.ChildrenOf(entity.Id).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            node.Children.Add(CreateNode(child));
        }

        return node;
    }

    private EntityNode? FindNode(Guid id, IEnumerable<EntityNode>? nodes = null)
    {
        foreach (var node in nodes ?? Roots)
        {
            if (node.Id == id)
            {
                return node;
            }

            var match = FindNode(id, node.Children);
            if (match is not null)
            {
                return match;
            }
        }

        return null;
    }

    private void OnLogWritten(LogEntry entry)
    {
        if (!Dispatcher.UIThread.CheckAccess())
        {
            Dispatcher.UIThread.Post(() => AppendLog(entry));
            return;
        }

        AppendLog(entry);
    }

    private void AppendLog(LogEntry entry)
    {
        Logs.Add(new LogItem(entry));
        while (Logs.Count > 500)
        {
            Logs.RemoveAt(0);
        }
    }
}
