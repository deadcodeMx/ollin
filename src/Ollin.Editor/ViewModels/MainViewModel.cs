using System.Collections.ObjectModel;
using Ollin.Core.Interaction;
using System.Reflection;
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
using Ollin.Scripting;

namespace Ollin.Editor.ViewModels;

/// <summary>Estado completo del editor: documento de escena, selección, cámara y consola.</summary>
public sealed partial class MainViewModel : ObservableObject
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
    private SceneHistory _history = new();
    private ScriptWorkspace _scripts;
    private readonly SceneRuntime _runtime = new();
    private string? _lastEditKey;
    private DateTime _lastEditTime;
    private bool _restoring;
    private readonly Stopwatch _playClock = new();
    private DispatcherTimer? _playTimer;
    private Vector2 _cameraBeforePlay;
    private float _zoomBeforePlay;
    private bool _dirtyBeforePlay;
    private string _scriptStatus = "Scripts: —";
    private bool _isPaintingTiles;
    private decimal _paintTileIndex;
    private FileSystemWatcher? _scriptWatcher;
    private bool _usesPrecompiledScripts;
    private DispatcherTimer? _scriptReloadTimer;
    private Guid? _selectedBeforePlay;
    private AssetItem? _selectedAsset;

    public MainViewModel(bool initialize = true)
    {
        _scripts = new ScriptWorkspace(_defaultProjectRoot);
        Log.Written += OnLogWritten;
        foreach (var entry in Log.History)
        {
            Logs.Add(new LogItem(entry));
        }

        LoadScene(DefaultScene.Create(), null);
        RefreshAssets(logToConsole: false);

        if (!initialize)
        {
            return;
        }

        ReloadScripts();
        StartScriptWatcher();
        InitializeProject();
        var version = typeof(MainViewModel).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?
            .Split('+')[0] ?? "0.0.0";
        Log.Info($"Ollin {version} — editor listo", "Editor");
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

    public bool IsPaintingTiles
    {
        get => _isPaintingTiles;
        set
        {
            if (SetProperty(ref _isPaintingTiles, value))
            {
                StatusMessage = value
                    ? (Selected?.Tilemap is null
                        ? "Selecciona una entidad con tilemap para pintar"
                        : "Modo pintar: clic o arrastra en el viewport para colocar mosaicos")
                    : "Listo";
                RequestRedraw();
            }
        }
    }

    public decimal PaintTileIndex
    {
        get => _paintTileIndex;
        set => SetProperty(ref _paintTileIndex, value);
    }

    /// <summary>Pinta el mosaico seleccionado en la celda bajo un punto de mundo.</summary>
    public void PaintTileAtWorld(Vector2 world)
    {
        if (!IsPaintingTiles || IsPlaying || Selected?.Tilemap is not { } map)
        {
            return;
        }

        var worldMatrix = Scene.GetWorldMatrix(Selected.Entity);
        if (!Matrix4x4.Invert(worldMatrix, out var inverse))
        {
            return;
        }

        var local = Vector2.Transform(world, inverse);
        var (x, y) = map.WorldToCell(local);
        if (x < 0 || y < 0 || x >= map.Width || y >= map.Height)
        {
            return;
        }

        var tile = (int)_paintTileIndex;
        if (map.TileAt(x, y) == tile)
        {
            return;
        }

        map.SetTile(x, y, tile);
        MarkDirty();
        RequestRedraw();
    }

    public string ScriptStatusText
    {
        get => _scriptStatus;
        private set => SetProperty(ref _scriptStatus, value);
    }

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
                if (_currentSceneDoc is not null)
                {
                    _currentSceneDoc.Path = value;
                }

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
                if (_currentSceneDoc is not null)
                {
                    _currentSceneDoc.IsDirty = value;
                }

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

    public float BrushWidth { get; set; } = 4f;

    public float BrushHeight { get; set; } = 3f;

    public float BrushDepth { get; set; } = 0.4f;

    public float BrushFloorSize { get; set; } = 6f;

    public int SeriesCount { get; set; } = 4;

    public float SeriesSpacing { get; set; } = 0.2f;

    public string BrushTexture { get; set; } = "assets/textures/wall.png";

    public string BrushFloorTexture { get; set; } = "assets/textures/floor.png";

    public IReadOnlyList<string> BrushTextures { get; } = new[]
    {
        "assets/textures/wall.png",
        "assets/textures/floor.png",
        "assets/textures/ceiling.png",
    };

    public string ProjectRoot
    {
        get => _projectRoot;
        set
        {
            if (SetProperty(ref _projectRoot, value))
            {
                Core.Audio.Audio.Root = value;
            }
        }
    }

    /// <summary>Cámara activa: la de la escena o una de respaldo si no existe.</summary>
    public Camera2D ActiveCamera => Scene.GetMainCamera() ?? _fallbackCamera;

    /// <summary>Cámara 3D principal (modo 2.5D), si existe.</summary>
    public Camera3D? ActiveCamera3D => Scene.GetMainCamera3D();

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

    private bool _isDialogueActive;
    private string _dialogueSpeaker = string.Empty;
    private string _dialogueText = string.Empty;
    private string _interactionPrompt = string.Empty;

    public bool IsDialogueActive
    {
        get => _isDialogueActive;
        private set
        {
            if (_isDialogueActive == value) return;
            _isDialogueActive = value;
            OnPropertyChanged();
        }
    }

    public string DialogueSpeaker
    {
        get => _dialogueSpeaker;
        private set
        {
            if (_dialogueSpeaker == value) return;
            _dialogueSpeaker = value;
            OnPropertyChanged();
        }
    }

    public string DialogueText
    {
        get => _dialogueText;
        private set
        {
            if (_dialogueText == value) return;
            _dialogueText = value;
            OnPropertyChanged();
        }
    }

    public string InteractionPrompt
    {
        get => _interactionPrompt;
        private set
        {
            if (_interactionPrompt == value) return;
            _interactionPrompt = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(HasInteractionPrompt));
        }
    }

    public bool HasInteractionPrompt => !string.IsNullOrWhiteSpace(InteractionPrompt);

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

    /// <summary>Inicia la reproducción desde la línea de comandos (`--play`).</summary>
    public void StartPlayback()
    {
        if (!IsPlaying)
        {
            StartPlay();
        }
    }

    private void StartPlay(bool keepState = false)
    {
        if (!keepState)
        {
            _runtime.GameState.Clear();
        }

        _cameraBeforePlay = CameraPosition;
        _zoomBeforePlay = ActiveCamera.Zoom;
        _dirtyBeforePlay = IsDirty;
        _selectedBeforePlay = Selected?.Id;

        if (!_usesPrecompiledScripts)
        {
            ReloadScripts();
        }

        ResetHudState();
        _runtime.Dialogues = new Ollin.Core.Dialogue.DialogueDatabase(ProjectRoot);
        _runtime.Play(Scene, _scripts);
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
        IsDialogueActive = false;
        DialogueSpeaker = string.Empty;
        DialogueText = string.Empty;
        InteractionPrompt = string.Empty;
        ResetHudState();

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
            UpdateRuntimeOverlay();
            RefreshHud();
            OnPropertyChanged(nameof(ElapsedText));
            RequestRedraw();
        };
        return timer;
    }

    /// <summary>Refleja en la interfaz el diálogo y el aviso de interacción.</summary>
    private void UpdateRuntimeOverlay()
    {
        var dialogue = _runtime.Dialogue;
        IsDialogueActive = dialogue.IsActive;
        DialogueSpeaker = dialogue.Speaker;
        DialogueText = dialogue.Text;
        InteractionPrompt = InteractionState.Prompt ?? string.Empty;
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
        if (_currentSceneDoc is not null)
        {
            _currentSceneDoc.Scene = scene;
        }

        ProjectRoot = Project?.RootPath
            ?? (path is null
                ? _defaultProjectRoot
                : System.IO.Path.GetDirectoryName(System.IO.Path.GetFullPath(path)) ?? _defaultProjectRoot);
        if (!string.Equals(_scripts.ProjectRoot, ProjectRoot, StringComparison.OrdinalIgnoreCase))
        {
            _scripts.Dispose();
            _scripts = new ScriptWorkspace(ProjectRoot);
            ReloadScripts();
            StartScriptWatcher();
        }
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

    // ---------- Scripts (etapa 3) ----------

    /// <summary>Recompila los scripts del proyecto y refleja el resultado en consola y barra de estado.</summary>
    public void ReloadScripts()
    {
        if (IsPlaying)
        {
            var hot = _runtime.HotReload(_scripts);
            ScriptStatusText = hot > 0 ? $"Scripts: {hot} en caliente" : "Scripts: recarga fallida";
            StatusMessage = hot > 0
                ? $"Recarga en caliente: {hot} script(s) con estado conservado"
                : "No se pudieron recargar los scripts (ver consola)";
            SyncOpenScripts();
            return;
        }

        var result = _scripts.Reload();
        var errors = result.Diagnostics.Count(d => d.IsError);

        if (errors > 0)
        {
            ScriptStatusText = $"Scripts: {errors} error(es)";
            StatusMessage = "Errores de compilación en scripts (ver consola)";
            return;
        }

        ScriptStatusText = _scripts.HasScripts
            ? $"Scripts: {string.Join(", ", result.Types.Select(t => t.Name))}"
            : "Scripts: sin scripts";

        SyncOpenScripts();
    }

    /// <summary>Refresca las pestañas de script que no tienen cambios sin guardar.</summary>
    private void SyncOpenScripts()
    {
        foreach (var document in Documents.Where(d => d.IsScript && d.FullPath is not null))
        {
            if (document.IsDirty || !File.Exists(document.FullPath))
            {
                continue;
            }

            try
            {
                var text = File.ReadAllText(document.FullPath!);
                if (string.Equals(text, document.Text, StringComparison.Ordinal))
                {
                    continue;
                }

                document.LoadText(text);

                if (ReferenceEquals(ActiveDocument, document))
                {
                    OnPropertyChanged(nameof(ActiveScript));
                }
            }
            catch (Exception exception)
            {
                Log.Warn($"No se pudo sincronizar {document.Title}: {exception.Message}", "Editor");
            }
        }
    }

    /// <summary>Crea (o reutiliza) un script para la entidad seleccionada y lo enlaza.</summary>
    public void CreateScriptForSelected()
    {
        if (IsPlaying || Selected is null)
        {
            return;
        }

        var entity = Selected.Entity;
        var className = SanitizeIdentifier(entity.Name) + "Script";
        var folder = _scripts.ScriptsFolder;
        Directory.CreateDirectory(folder);
        var fullPath = Path.Combine(folder, className + ".cs");

        if (!File.Exists(fullPath))
        {
            File.WriteAllText(fullPath, ScriptTemplate(className, entity.Name));
        }

        var component = entity.GetComponent<ScriptComponent>() ?? entity.AddComponent(new ScriptComponent());
        component.ScriptPath = $"scripts/{className}.cs";
        component.ClassName = className;

        ReloadScripts();
        Selected.RefreshAll();
        MarkDirty();
        StatusMessage = $"Script {className} enlazado a {entity.Name}";
        Log.Info($"Script creado: scripts/{className}.cs", "Scripts");
    }

    private static string SanitizeIdentifier(string name)
    {
        var builder = new System.Text.StringBuilder();
        foreach (var character in name)
        {
            if (char.IsLetterOrDigit(character))
            {
                builder.Append(character);
            }
        }

        var result = builder.ToString();
        if (result.Length == 0)
        {
            return "Entity";
        }

        return char.IsDigit(result[0]) ? "E" + result : result;
    }

    private static string ScriptTemplate(string className, string entityName) => $$"""
        using System;
        using System.Numerics;
        using Ollin.Core.Input;
        using Ollin.Scripting;

        /// <summary>Script de {{entityName}}.</summary>
        public sealed class {{className}} : OllinScript
        {
            private Vector2 _origin;

            public override void OnStart()
            {
                _origin = Entity.Transform.Position;
                Debug("{{className}} iniciado en " + Entity.Name);
            }

            public override void OnUpdate(float deltaTime)
            {
                // Movimiento con WASD; cambia o borra este ejemplo.
                var direction = Vector2.Zero;
                if (Input.GetKey(KeyCode.A)) direction.X -= 1f;
                if (Input.GetKey(KeyCode.D)) direction.X += 1f;
                if (Input.GetKey(KeyCode.W)) direction.Y += 1f;
                if (Input.GetKey(KeyCode.S)) direction.Y -= 1f;

                if (direction != Vector2.Zero)
                {
                    Entity.Transform.Position += Vector2.Normalize(direction) * 3f * Time.DeltaTime;
                }
            }
        }
        """;

    /// <summary>
    /// Vigila la carpeta de scripts: al guardar un `.cs` recompila y, si se está
    /// reproduciendo, aplica recarga en caliente conservando el estado.
    /// </summary>
    private void StartScriptWatcher()
    {
        _scriptWatcher?.Dispose();
        _scriptWatcher = null;

        try
        {
            var folder = _scripts.ScriptsFolder;
            Directory.CreateDirectory(folder);

            _scriptWatcher = new FileSystemWatcher(folder, "*.cs")
            {
                IncludeSubdirectories = true,
                NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.FileName | NotifyFilters.Size,
                EnableRaisingEvents = true,
            };

            _scriptWatcher.Changed += OnScriptFileChanged;
            _scriptWatcher.Created += OnScriptFileChanged;
            _scriptWatcher.Deleted += OnScriptFileChanged;
            _scriptWatcher.Renamed += OnScriptFileChanged;

            _scriptReloadTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(600) };
            _scriptReloadTimer.Tick += (_, _) =>
            {
                _scriptReloadTimer?.Stop();
                ReloadScripts();
            };
        }
        catch (Exception exception)
        {
            Log.Warn($"No se pudo vigilar la carpeta de scripts: {exception.Message}", "Scripts");
        }
    }

    private void OnScriptFileChanged(object sender, FileSystemEventArgs e)
    {
        // El evento llega en un hilo del sistema de archivos; se agrupa y se pasa a la UI.
        Dispatcher.UIThread.Post(() =>
        {
            if (_scriptReloadTimer is null)
            {
                return;
            }

            _scriptReloadTimer.Stop();
            _scriptReloadTimer.Start();
        });
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
        var number = Documents.Count(d => d.IsScene) + 1;
        var scene = new Scene { Name = $"Escena {number}" };
        var camera = scene.Create("Cámara principal", Vector2.Zero);
        camera.AddComponent(new Camera2D());
        AddSceneDocument(scene, null);
        Log.Info("Escena nueva creada", "Editor");
    }

    public void OpenScene(string path)
    {
        try
        {
            var fullPath = Path.GetFullPath(path);
            var alreadyOpen = Documents.FirstOrDefault(d =>
                d is { IsScene: true, SceneDocument: not null } &&
                string.Equals(d.SceneDocument.Path, fullPath, StringComparison.OrdinalIgnoreCase));

            if (alreadyOpen is not null)
            {
                ActiveDocument = alreadyOpen;
                return;
            }

            var scene = SceneSerializer.Load(fullPath);
            AddSceneDocument(scene, fullPath);
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
            RefreshSceneTabTitle();
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

    /// <summary>Crea una pared o un suelo 3D en la posición indicada por el pincel.</summary>
    public void AddBrush3D(Vector2 position, bool floor)
    {
        if (IsPlaying)
        {
            return;
        }

        var entity = Scene.Create(floor ? "Suelo 3D" : "Pared 3D", position);
        entity.AddComponent(new Wall3D
        {
            Size = floor
                ? new Vector3(MathF.Max(0.5f, BrushFloorSize), 0.2f, MathF.Max(0.5f, BrushFloorSize))
                : new Vector3(MathF.Max(0.2f, BrushWidth), MathF.Max(0.2f, BrushHeight), MathF.Max(0.1f, BrushDepth)),
            Color = floor ? new Vector3(0.3f, 0.27f, 0.22f) : new Vector3(0.58f, 0.55f, 0.5f),
            Collides = !floor,
            BaseY = 0f,
            TexturePath = floor ? BrushFloorTexture : BrushTexture,
            TextureScale = 2f,
        });

        RebuildTree();
        Select(entity);
        IsDirty = true;
        CommitEdit(floor ? "Pincel: suelo 3D" : "Pincel: pared 3D");
        OnPropertyChanged(nameof(EntityCountText));
        RequestRedraw();
    }

    /// <summary>Duplica la selección varias veces en fila (una sola entrada de deshacer).</summary>
    public void DuplicateSelectedSeries()
    {
        var count = Math.Clamp(SeriesCount, 1, 64);
        DuplicateSelectedSeries(count, SeriesSpacing);
    }

    private void DuplicateSelectedSeries(int count, float spacing)
    {
        if (IsPlaying || Selected is null || count <= 0)
        {
            return;
        }

        var original = Selected.Entity;
        var step = original.GetComponent<Wall3D>() is { } wall
            ? MathF.Max(0.5f, wall.Size.X + MathF.Max(0f, spacing))
            : MathF.Max(0.5f, 1.5f + spacing);
        Entity? last = null;

        for (var i = 1; i <= count; i++)
        {
            var copy = original.Clone();
            copy.Name = $"{original.Name} {i + 1}";
            copy.Transform.Position = original.Transform.Position + new Vector2(step * i, 0f);
            Scene.Add(copy);
            last = copy;
        }

        RebuildTree();
        if (last is not null) Select(last);
        IsDirty = true;
        CommitEdit($"Duplicar serie x{count}");
        OnPropertyChanged(nameof(EntityCountText));
        RequestRedraw();
    }

    /// <summary>Añade otra arma a la entidad seleccionada, en la siguiente ranura libre.</summary>
    public void AddWeaponToSelected()
    {
        if (IsPlaying || Selected is null)
        {
            return;
        }

        var entity = Selected.Entity;
        var weapons = entity.Components.OfType<Weapon>().ToList();
        var slot = Enumerable.Range(1, 3).FirstOrDefault(value => weapons.All(weapon => weapon.Slot != value));

        if (slot == 0)
        {
            StatusMessage = "Ya hay tres armas en esta entidad";
            return;
        }

        var weapon = new Weapon
        {
            Name = slot == 1 ? "Pistola" : slot == 2 ? "Escopeta" : "Rifle",
            Slot = slot,
            Ammo = 12,
            MagazineSize = 12,
            ReserveAmmo = 48,
        };
        entity.AddComponent(weapon);
        RebuildTree();
        Selected = FindNode(entity.Id);
        Selected!.WeaponSlotIndex = slot;
        IsDirty = true;
        CommitEdit("Añadir arma");
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
        var filter = string.IsNullOrWhiteSpace(SearchText) ? null : SearchText.Trim();

        foreach (var root in Scene.RootEntities.OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            var node = CreateNode(root, filter);
            if (node is not null)
            {
                Roots.Add(node);
            }
        }
    }

    private EntityNode? CreateNode(Entity entity, string? filter)
    {
        var node = new EntityNode(entity, this);
        foreach (var child in Scene.ChildrenOf(entity.Id).OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase))
        {
            var childNode = CreateNode(child, filter);
            if (childNode is not null)
            {
                node.Children.Add(childNode);
            }
        }

        var matches = filter is null || entity.Name.Contains(filter, StringComparison.OrdinalIgnoreCase);
        return matches || node.Children.Count > 0 ? node : null;
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
