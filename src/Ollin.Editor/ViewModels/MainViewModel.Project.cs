using Ollin.Core.Input;
using System.Collections.ObjectModel;
using Ollin.Core.Assets;
using Ollin.Core.Diagnostics;
using Ollin.Core.Projects;
using Ollin.Scripting;

namespace Ollin.Editor.ViewModels;

/// <summary>Proyectos, documentos abiertos y explorador de archivos.</summary>
public sealed partial class MainViewModel
{
    private readonly EditorSettings _settings = EditorSettings.Load();

    public EditorSettings Settings => _settings;
    private ProjectInfo? _project;
    private DocumentTab? _activeDocument;

    public ProjectInfo? Project
    {
        get => _project;
        private set
        {
            if (!SetProperty(ref _project, value))
            {
                return;
            }

            OnPropertyChanged(nameof(HasProject));
            OnPropertyChanged(nameof(IsWelcomeVisible));
            OnPropertyChanged(nameof(ProjectName));
            OnPropertyChanged(nameof(ProjectPathText));
            InputMap.Set(value?.InputActions);
            if (value is not null) Ollin.Core.Localization.Localization.Load(value.RootPath);
        }
    }

    public bool HasProject => Project is not null;

    public bool IsWelcomeVisible => Project is null;

    public string ProjectName => Project?.Name ?? "Sin proyecto";

    public string ProjectPathText => Project?.RootPath ?? "Crea o abre un proyecto para empezar";

    public ObservableCollection<DocumentTab> Documents { get; } = new();

    public ObservableCollection<FileNode> ProjectFiles { get; } = new();

    public DocumentTab? ActiveDocument
    {
        get => _activeDocument;
        set
        {
            if (ReferenceEquals(_activeDocument, value))
            {
                return;
            }

            if (value is { IsScene: true, SceneDocument: { } target } &&
                _runtime.IsPlaying &&
                !ReferenceEquals(target, _currentSceneDoc))
            {
                StatusMessage = "Detén la reproducción para cambiar de escena";
                return;
            }

            _activeDocument = value;
            foreach (var document in Documents)
            {
                document.IsActive = ReferenceEquals(document, value);
            }

            if (value is { IsScene: true, SceneDocument: { } sceneDocument })
            {
                ActivateSceneDocument(sceneDocument);
            }

            OnPropertyChanged();
            OnPropertyChanged(nameof(IsSceneActive));
            OnPropertyChanged(nameof(IsScriptActive));
            OnPropertyChanged(nameof(ActiveScript));
            RequestRedraw();
        }
    }

    public bool IsSceneActive => ActiveDocument is null || ActiveDocument.IsScene;

    public bool IsScriptActive => ActiveDocument is { IsScene: false };

    public DocumentTab? ActiveScript => ActiveDocument is { IsScene: false } ? ActiveDocument : null;

    /// <summary>Arranca con el último proyecto o muestra la pantalla de bienvenida.</summary>
    private void InitializeProject()
    {
        var arguments = Environment.GetCommandLineArgs();
        var projectArgument = ValueAfter(arguments, "--project");
        var scriptArgument = ValueAfter(arguments, "--script");

        if (!string.IsNullOrWhiteSpace(projectArgument) && (File.Exists(projectArgument) || Directory.Exists(projectArgument)))
        {
            OpenProjectLocation(projectArgument, logToConsole: false);
            if (!string.IsNullOrWhiteSpace(scriptArgument) && File.Exists(scriptArgument))
            {
                OpenScript(Path.GetFullPath(scriptArgument));
            }

            return;
        }

        var last = _settings.LastProject;
        if (!string.IsNullOrWhiteSpace(last) && (File.Exists(last) || Directory.Exists(last)))
        {
            try
            {
                OpenProjectLocation(last, logToConsole: false);
                return;
            }
            catch (Exception exception)
            {
                Log.Warn($"No se pudo abrir el último proyecto: {exception.Message}", "Proyecto");
            }
        }

        EnsureSceneTab();
        OnPropertyChanged(nameof(IsWelcomeVisible));
    }

    private static string? ValueAfter(string[] arguments, string name)
    {
        for (var i = 0; i < arguments.Length - 1; i++)
        {
            if (string.Equals(arguments[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return arguments[i + 1];
            }
        }

        return null;
    }

    /// <summary>Crea un proyecto nuevo con su propia carpeta y lo abre.</summary>
    public void CreateProject(string parentFolder, string name, ProjectTemplate template = ProjectTemplate.Empty,
        ProjectContentMode contentMode = ProjectContentMode.Full, ProjectGenre genre = ProjectGenre.None)
    {
        try
        {
            var templateAssets = Path.Combine(AppContext.BaseDirectory, "assets");
            if (!Directory.Exists(templateAssets))
            {
                var root = ProjectLocator.FindProjectRoot();
                templateAssets = root is null ? templateAssets : Path.Combine(root, "assets");
            }

            var project = ProjectService.Create(parentFolder, name, Directory.Exists(templateAssets) ? templateAssets : null, template, contentMode, genre);
            ApplyProject(project);
            StatusMessage = $"Proyecto '{project.Name}' creado en {project.RootPath}";
            Log.Info($"Proyecto creado: {project.ProjectFile}", "Proyecto");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo crear el proyecto: {exception.Message}", "Proyecto");
            StatusMessage = "Error al crear el proyecto";
        }
    }

    /// <summary>Abre un proyecto a partir de su archivo, carpeta o una subcarpeta.</summary>
    public void OpenProjectLocation(string path, bool logToConsole = true, bool remember = true)
    {
        try
        {
            var project = ProjectService.Find(path);
            var openedAsFolder = project is null;

            if (project is null)
            {
                var folder = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
                if (folder is null || !Directory.Exists(folder))
                {
                    Log.Warn($"No se encontró un proyecto en {path}", "Proyecto");
                    return;
                }

                project = ProjectService.OpenFolder(folder);
            }

            ApplyProject(project, remember);

            // Una carpeta sin proyecto propio (la de ejemplo) abre la escena de demostración.
            if (openedAsFolder && string.IsNullOrWhiteSpace(project.StartScene))
            {
                LoadScene(Ollin.Core.Scenes.DefaultScene.Create(), null);
                RefreshSceneTabTitle();
                ActiveDocument = Documents.FirstOrDefault();
            }

            if (logToConsole)
            {
                StatusMessage = $"Proyecto abierto: {project.Name}";
                Log.Info($"Proyecto abierto: {project.RootPath}", "Proyecto");
            }
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo abrir el proyecto: {exception.Message}", "Proyecto");
        }
    }

    /// <summary>Carga scripts ya compilados (juego exportado).</summary>
    public void LoadPrecompiledScripts(byte[] image)
    {
        _usesPrecompiledScripts = true;
        _scriptWatcher?.Dispose();
        _scriptWatcher = null;
        _scriptReloadTimer?.Stop();

        var result = _scripts.LoadImage(image);
        StatusMessage = $"Scripts cargados: {result.Types.Count}";
    }

    /// <summary>Empaqueta el proyecto y el reproductor en una carpeta jugable.</summary>
    public string? ExportGame(string destination, bool splash = true, bool watermark = true)
    {
        if (Project is null)
        {
            StatusMessage = "Abre un proyecto para exportarlo";
            return null;
        }

        try
        {
            if (IsPlaying)
            {
                StopPlay();
            }

            if (IsDirty && !string.IsNullOrWhiteSpace(ScenePath) && File.Exists(ScenePath))
            {
                SaveScene(ScenePath);
            }

            var template = GameExporter.FindPlayerTemplate();

            if (template is null)
            {
                StatusMessage = "No se encontró el reproductor (carpeta 'player')";
                Log.Error("Falta la carpeta 'player' junto al editor o en dist/app/player", "Exportar");
                return null;
            }

            if (Directory.Exists(Project.ScriptsFolder) &&
                Directory.EnumerateFiles(Project.ScriptsFolder, "*.cs", SearchOption.AllDirectories).Any())
            {
                var compilation = Ollin.Scripting.ScriptCompiler.CompileDirectory(Project.ScriptsFolder);

                if (!compilation.Success || compilation.Image is null)
                {
                    foreach (var diagnostic in compilation.Diagnostics.Where(d => d.IsError))
                    {
                        Log.Error(diagnostic.Text, "Exportar");
                    }

                    StatusMessage = "No se pudo exportar: hay errores de script";
                    return null;
                }

                var pathWithScripts = GameExporter.Export(Project, destination, template, compilation.Image, splash, watermark);
                StatusMessage = $"Juego exportado: {pathWithScripts}";
                Log.Info($"Juego exportado: {pathWithScripts}", "Exportar");
                return pathWithScripts;
            }

            var path = GameExporter.Export(Project, destination, template, null, splash, watermark);
            StatusMessage = $"Juego exportado: {path}";
            Log.Info($"Juego exportado: {path}", "Exportar");
            return path;
        }
        catch (Exception exception)
        {
            StatusMessage = "Error al exportar el juego";
            Log.Error($"No se pudo exportar el juego: {exception.Message}", "Exportar");
            return null;
        }
    }

    public void CloseProject()
    {
        Project = null;
        ProjectRoot = _defaultProjectRoot;
        _scripts.Dispose();
        _scripts = new ScriptWorkspace(ProjectRoot);
        ReloadScripts();
        StartScriptWatcher();
        Documents.Clear();
        _currentSceneDoc = null;
        ProjectFiles.Clear();
        RefreshAssets(logToConsole: false);
        NewScene();
        _settings.LastProject = null;
        _settings.Save();
        OnPropertyChanged(nameof(IsWelcomeVisible));
    }

    private void ApplyProject(ProjectInfo project, bool remember = true)
    {
        _usesPrecompiledScripts = false;
        Project = project;
        ProjectRoot = project.RootPath;

        _scripts.Dispose();
        _scripts = new ScriptWorkspace(project.RootPath);
        ReloadScripts();
        StartScriptWatcher();
        RefreshAssets(logToConsole: false);
        RefreshProjectFiles();

        if (remember)
        {
            _settings.RememberProject(File.Exists(project.ProjectFile) ? project.ProjectFile : project.RootPath);
            _settings.Save();
        }

        Documents.Clear();
        _currentSceneDoc = null;

        if (!string.IsNullOrWhiteSpace(project.StartScene) && File.Exists(project.StartSceneFullPath))
        {
            OpenScene(project.StartSceneFullPath);
        }
        else
        {
            NewScene();
        }
    }

    private void EnsureSceneTab()
    {
        if (Documents.Any(d => d.IsScene))
        {
            return;
        }

        if (_currentSceneDoc is null)
        {
            _currentSceneDoc = new SceneDocument(Scene, ScenePath, _history)
            {
                IsDirty = IsDirty,
                CameraPosition = CameraPosition,
                Zoom = ActiveCamera.Zoom,
                SelectedId = Selected?.Id,
            };
        }

        var tab = new DocumentTab(_currentSceneDoc.Title, isScene: true, scene: _currentSceneDoc);
        Documents.Insert(0, tab);

        if (ActiveDocument is null)
        {
            ActiveDocument = tab;
        }
    }

    /// <summary>Reconstruye el explorador de archivos del proyecto.</summary>
    public void RefreshProjectFiles()
    {
        ProjectFiles.Clear();
        if (Project is null)
        {
            return;
        }

        try
        {
            foreach (var path in Directory.EnumerateDirectories(Project.RootPath).OrderBy(d => Path.GetFileName(d), StringComparer.OrdinalIgnoreCase))
            {
                ProjectFiles.Add(BuildFileNode(path));
            }
        }
        catch
        {
            // Sin permisos para listar la raíz: se ignora.
        }

        try
        {
            foreach (var file in Directory.EnumerateFiles(Project.RootPath).OrderBy(f => f))
            {
                ProjectFiles.Add(new FileNode(Path.GetFileName(file), file, isFolder: false));
            }
        }
        catch
        {
            // Sin permisos para listar la raíz: se ignora.
        }
    }

    private static FileNode BuildFileNode(string path)
    {
        var node = new FileNode(Path.GetFileName(path), path, isFolder: true);

        try
        {
            foreach (var directory in Directory.EnumerateDirectories(path).OrderBy(d => d))
            {
                node.Children.Add(BuildFileNode(directory));
            }

            foreach (var file in Directory.EnumerateFiles(path).OrderBy(f => f))
            {
                node.Children.Add(new FileNode(Path.GetFileName(file), file, isFolder: false));
            }
        }
        catch
        {
            // Carpeta ilegible: se deja vacía.
        }

        return node;
    }

    /// <summary>Abre un archivo del explorador: script, escena o textura.</summary>
    public void OpenProjectFile(FileNode node)
    {
        if (node.IsFolder)
        {
            return;
        }

        var extension = Path.GetExtension(node.FullPath).ToLowerInvariant();

        switch (extension)
        {
            case ".cs":
                OpenScript(node.FullPath);
                break;

            case ".json" when node.FullPath.Contains("dialogs", StringComparison.OrdinalIgnoreCase):
                OpenScript(node.FullPath);
                break;

            case ".json" when node.FullPath.Contains("scenes", StringComparison.OrdinalIgnoreCase):
                if (IsDirty)
                {
                    StatusMessage = "Guarda la escena actual antes de abrir otra";
                    Log.Warn("Hay cambios sin guardar en la escena", "Proyecto");
                    return;
                }

                OpenScene(node.FullPath);
                ActiveDocument = Documents.FirstOrDefault();
                break;

            case ".png" or ".jpg" or ".jpeg" or ".bmp":
                SelectedAsset = Assets.FirstOrDefault(a =>
                    string.Equals(a.Info.FullPath, node.FullPath, StringComparison.OrdinalIgnoreCase));
                AssignAssetToSelected();
                break;
        }
    }

    public void OpenScript(string fullPath)
    {
        var existing = Documents.FirstOrDefault(d =>
            d.FullPath is not null && string.Equals(d.FullPath, fullPath, StringComparison.OrdinalIgnoreCase));

        if (existing is not null)
        {
            ActiveDocument = existing;
            return;
        }

        try
        {
            var tab = new DocumentTab(Path.GetFileName(fullPath), isScene: false, fullPath);
            tab.LoadText(File.ReadAllText(fullPath));
            Documents.Add(tab);
            ActiveDocument = tab;
            StatusMessage = $"Editando {tab.Title}";
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo abrir el script: {exception.Message}", "Editor");
        }
    }

    public void SaveActiveScript()
    {
        if (ActiveDocument is not { IsScene: false, FullPath: not null } document)
        {
            return;
        }

        try
        {
            File.WriteAllText(document.FullPath!, document.Text);
            document.IsDirty = false;
            StatusMessage = $"Guardado {document.Title}";
            Log.Info($"Script guardado: {document.Title}", "Editor");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar el script: {exception.Message}", "Editor");
        }
    }

    public void CloseDocument(DocumentTab document, bool force = false)
    {
        if (document.IsScene)
        {
            if (_runtime.IsPlaying)
            {
                StatusMessage = "Detén la reproducción para cerrar escenas";
                return;
            }

            if (Documents.Count(d => d.IsScene) <= 1)
            {
                StatusMessage = "No se puede cerrar la única escena abierta";
                return;
            }

            if (!force && document.SceneDocument?.IsDirty == true)
            {
                StatusMessage = "Guarda la escena antes de cerrarla";
                return;
            }

            var next = Documents.FirstOrDefault(d => d.IsScene && !ReferenceEquals(d, document));
            if (ReferenceEquals(ActiveDocument, document) && next is not null)
            {
                ActiveDocument = next;
            }

            Documents.Remove(document);
            return;
        }

        Documents.Remove(document);

        if (ReferenceEquals(ActiveDocument, document))
        {
            ActiveDocument = Documents.FirstOrDefault();
        }
    }

    /// <summary>Abre el script del componente seleccionado.</summary>
    public void OpenSelectedScript()
    {
        var path = Selected?.ScriptPath;
        if (string.IsNullOrWhiteSpace(path))
        {
            Log.Warn("La entidad seleccionada no tiene script", "Editor");
            return;
        }

        var full = Path.IsPathRooted(path) ? path : Path.Combine(ProjectRoot, path);
        if (File.Exists(full))
        {
            OpenScript(full);
        }
        else
        {
            Log.Warn($"No existe el script: {path}", "Editor");
        }
    }
}
