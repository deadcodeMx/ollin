using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Diagnostics;
using Ollin.Core.Prefabs;

namespace Ollin.Editor.ViewModels;

/// <summary>Portapapeles, prefabs y búsqueda en la jerarquía.</summary>
public sealed partial class MainViewModel
{
    private string? _clipboard;
    private string _searchText = string.Empty;

    public string SearchText
    {
        get => _searchText;
        set
        {
            if (!SetProperty(ref _searchText, value))
            {
                return;
            }

            RebuildTree();
            OnPropertyChanged(nameof(HasSearch));
        }
    }

    public bool HasSearch => !string.IsNullOrWhiteSpace(SearchText);

    /// <summary>Nombres de las clases de script compiladas (para autocompletar).</summary>
    public IReadOnlyList<string> ScriptClassNames => _scripts.Types.Select(t => t.Name).ToList();

    /// <summary>Carpeta de prefabs del proyecto (o una local si no hay proyecto).</summary>
    public string PrefabsFolder => Project?.PrefabsFolder ?? Path.Combine(ProjectRoot, "prefabs");

    public void ClearSearch() => SearchText = string.Empty;

    /// <summary>Copia la entidad seleccionada al portapapeles interno (JSON).</summary>
    public void CopySelected()
    {
        if (Selected is null)
        {
            StatusMessage = "Selecciona una entidad para copiar";
            return;
        }

        _clipboard = PrefabSerializer.Serialize(Selected.Entity);
        StatusMessage = $"Copiado: {Selected.Name}";
        Log.Info($"Entidad copiada: {Selected.Name}", "Editor");
    }

    /// <summary>Pega la entidad copiada con un desplazamiento y nombre único.</summary>
    public void PasteCopied()
    {
        if (IsPlaying)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(_clipboard))
        {
            StatusMessage = "No hay nada que pegar";
            return;
        }

        try
        {
            var entity = PrefabSerializer.Deserialize(_clipboard);
            entity.Id = Guid.NewGuid();
            entity.Name = UniqueName(entity.Name);
            entity.Transform.Position += new Vector2(1f, -0.5f);
            Scene.Add(entity);
            RebuildTree();
            Select(entity);
            IsDirty = true;
            CommitEdit("Pegar entidad");
            OnPropertyChanged(nameof(EntityCountText));
            StatusMessage = $"Pegado: {entity.Name}";
            RequestRedraw();
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo pegar: {exception.Message}", "Editor");
        }
    }

    /// <summary>Guarda la entidad seleccionada como prefab reutilizable.</summary>
    public void SaveSelectedAsPrefab(string path)
    {
        if (Selected is null)
        {
            StatusMessage = "Selecciona una entidad para guardarla como prefab";
            return;
        }

        try
        {
            PrefabSerializer.Save(Selected.Entity, path);
            RefreshProjectFiles();
            StatusMessage = $"Prefab guardado: {Path.GetFileName(path)}";
            Log.Info($"Prefab guardado: {path}", "Editor");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar el prefab: {exception.Message}", "Editor");
        }
    }

    /// <summary>Instancia un archivo `.prefab.json` delante de la cámara.</summary>
    public void InstantiatePrefab(string path)
    {
        if (IsPlaying)
        {
            return;
        }

        try
        {
            var prefab = PrefabSerializer.Load(path);
            var instance = PrefabSerializer.Instantiate(prefab, CameraPosition, UniqueName(prefab.Name));
            Scene.Add(instance);
            RebuildTree();
            Select(instance);
            IsDirty = true;
            CommitEdit("Instanciar prefab");
            OnPropertyChanged(nameof(EntityCountText));
            StatusMessage = $"Prefab instanciado: {instance.Name}";
            RequestRedraw();
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo instanciar el prefab: {exception.Message}", "Editor");
        }
    }

    private string UniqueName(string baseName)
    {
        var name = $"{baseName} (copia)";
        var counter = 2;

        while (Scene.Find(name) is not null)
        {
            name = $"{baseName} (copia {counter++})";
        }

        return name;
    }

    /// <summary>Adjunta (o reemplaza) el script de una entidad, por ejemplo al arrastrarlo.</summary>
    public void AttachScript(Entity target, string scriptPath)
    {
        if (IsPlaying)
        {
            return;
        }

        try
        {
            var fullPath = Path.GetFullPath(scriptPath);
            if (!fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) || !File.Exists(fullPath))
            {
                StatusMessage = "Solo se pueden arrastrar scripts .cs";
                return;
            }

            var relative = Path.GetRelativePath(ProjectRoot, fullPath).Replace('\\', '/');
            var className = Path.GetFileNameWithoutExtension(fullPath);
            var component = target.GetComponent<ScriptComponent>() ?? target.AddComponent(new ScriptComponent());
            component.ScriptPath = relative;
            component.ClassName = className;

            RebuildTree();
            Select(target);
            Selected?.RefreshAll();
            IsDirty = true;
            CommitEdit("Añadir script");
            StatusMessage = $"Script {className} en {target.Name}";
            Log.Info($"Script {relative} añadido a {target.Name}", "Editor");
            RequestRedraw();
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo añadir el script: {exception.Message}", "Editor");
        }
    }

    /// <summary>Quita el componente de script de la entidad seleccionada.</summary>
    public void RemoveScriptFromSelected()
    {
        if (IsPlaying || Selected?.Entity.GetComponent<ScriptComponent>() is not { } component)
        {
            return;
        }

        Selected.Entity.RemoveComponent(component);
        Selected.RefreshAll();
        IsDirty = true;
        CommitEdit("Quitar script");
        StatusMessage = $"Script quitado de {Selected.Name}";
        RequestRedraw();
    }

    /// <summary>Añade un componente a la entidad seleccionada si no lo tiene ya.</summary>
    public void AddComponentTo(string kind)
    {
        if (IsPlaying || Selected is null)
        {
            return;
        }

        var entity = Selected.Entity;
        var added = true;

        switch (kind)
        {
            case "collider":
                if (entity.HasComponent<BoxCollider>()) { added = false; break; }
                var size = entity.GetComponent<SpriteRenderer>()?.Size ?? Vector2.One;
                entity.AddComponent(new BoxCollider { Size = size, IsTrigger = false });
                break;

            case "trigger":
                if (entity.HasComponent<BoxCollider>()) { added = false; break; }
                var triggerSize = entity.GetComponent<SpriteRenderer>()?.Size ?? Vector2.One;
                entity.AddComponent(new BoxCollider { Size = triggerSize, IsTrigger = true });
                break;

            case "rigidbody":
                if (entity.HasComponent<Rigidbody2D>()) { added = false; break; }
                entity.AddComponent(new Rigidbody2D());
                break;

            case "camera":
                if (entity.HasComponent<Camera2D>()) { added = false; break; }
                entity.AddComponent(new Camera2D());
                break;

            case "animator":
                if (entity.HasComponent<SpriteAnimator>()) { added = false; break; }
                entity.AddComponent(new SpriteAnimator
                {
                    TexturePath = entity.GetComponent<SpriteRenderer>()?.TexturePath,
                });
                break;

            case "tilemap":
                if (entity.HasComponent<Tilemap>()) { added = false; break; }
                entity.AddComponent(new Tilemap());
                break;

            case "audio":
                if (entity.HasComponent<AudioSource>()) { added = false; break; }
                entity.AddComponent(new AudioSource { PlayOnStart = false });
                break;

            case "light":
                if (entity.HasComponent<Light2D>()) { added = false; break; }
                entity.AddComponent(new Light2D());
                break;

            case "inventory":
                if (entity.HasComponent<Inventory>()) { added = false; break; }
                entity.AddComponent(new Inventory());
                break;

            case "stats":
                if (entity.HasComponent<Stats>()) { added = false; break; }
                entity.AddComponent(new Stats());
                break;

            case "daynight":
                if (entity.HasComponent<DayNightCycle>()) { added = false; break; }
                entity.AddComponent(new DayNightCycle());
                break;

            case "ai":
                if (entity.HasComponent<AiAgent>()) { added = false; break; }
                entity.AddComponent(new AiAgent());
                break;

            case "rotator":
                if (entity.HasComponent<RotatorComponent>()) { added = false; break; }
                entity.AddComponent(new RotatorComponent());
                break;

            case "oscillator":
                if (entity.HasComponent<OscillatorComponent>()) { added = false; break; }
                entity.AddComponent(new OscillatorComponent());
                break;

            case "script":
                if (entity.HasComponent<ScriptComponent>()) { added = false; break; }
                entity.AddComponent(new ScriptComponent());
                break;

            default:
                added = false;
                break;
        }

        if (!added)
        {
            StatusMessage = "La entidad ya tiene ese componente";
            return;
        }

        Selected.RefreshAll();
        IsDirty = true;
        CommitEdit("Añadir componente");
        StatusMessage = $"Componente {kind} añadido a {entity.Name}";
        RequestRedraw();
    }

    /// <summary>Crea una entidad hija de la indicada (estilo Unity).</summary>
    public void CreateChild(Entity parent)
    {
        if (IsPlaying)
        {
            return;
        }

        var child = Scene.Create("Hijo", Scene.GetWorldPosition(parent) + new Vector2(0.5f, -0.5f), parent.Id);
        RebuildTree();
        Select(child);
        IsDirty = true;
        CommitEdit("Crear hijo");
        StatusMessage = $"Hijo creado bajo {parent.Name}";
        RequestRedraw();
    }

    /// <summary>Instancia un prefab en una posición concreta (arrastrar y soltar).</summary>
    public void InstantiatePrefabAt(string path, Vector2 position)
    {
        if (IsPlaying)
        {
            return;
        }

        try
        {
            var prefab = PrefabSerializer.Load(path);
            var instance = PrefabSerializer.Instantiate(prefab, position, UniqueName(prefab.Name));
            Scene.Add(instance);
            RebuildTree();
            Select(instance);
            IsDirty = true;
            CommitEdit("Instanciar prefab");
            StatusMessage = $"Prefab instanciado: {instance.Name}";
            RequestRedraw();
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo instanciar el prefab: {exception.Message}", "Editor");
        }
    }

    /// <summary>Borra un archivo o una carpeta vacía del proyecto.</summary>
    public void DeleteFile(FileNode node)
    {
        if (IsPlaying)
        {
            StatusMessage = "Detén la reproducción para eliminar archivos";
            return;
        }

        try
        {
            if (node.IsFolder)
            {
                if (Directory.EnumerateFileSystemEntries(node.FullPath).Any())
                {
                    StatusMessage = "Solo se pueden borrar carpetas vacías";
                    return;
                }

                Directory.Delete(node.FullPath);
                RefreshProjectFiles();
                StatusMessage = $"Carpeta eliminada: {node.Name}";
                return;
            }

            var fullPath = Path.GetFullPath(node.FullPath);

            foreach (var document in Documents.Where(d =>
                         d.FullPath is not null &&
                         string.Equals(d.FullPath, fullPath, StringComparison.OrdinalIgnoreCase)).ToList())
            {
                if (document.IsScene)
                {
                    var otherScene = Documents.FirstOrDefault(d => d.IsScene && !ReferenceEquals(d, document));
                    if (otherScene is null)
                    {
                        StatusMessage = "No se puede borrar la única escena abierta";
                        return;
                    }

                    if (ReferenceEquals(ActiveDocument, document))
                    {
                        ActiveDocument = otherScene;
                    }
                }

                Documents.Remove(document);
            }

            File.Delete(fullPath);
            RefreshProjectFiles();
            RefreshAssets(logToConsole: false);

            if (fullPath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
            {
                ReloadScripts();
            }

            StatusMessage = $"Archivo eliminado: {node.Name}";
            Log.Info($"Archivo eliminado: {fullPath}", "Editor");
            RequestRedraw();
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo eliminar: {exception.Message}", "Editor");
            StatusMessage = "No se pudo eliminar el archivo";
        }
    }

    /// <summary>Centra la cámara en una entidad de la jerarquía.</summary>
    public void FocusEntity(Entity entity)
    {
        Select(entity);
        FrameSelected();
    }
}
