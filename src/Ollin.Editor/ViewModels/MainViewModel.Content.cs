using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Diagnostics;
using Ollin.Core.Prefabs;

namespace Ollin.Editor.ViewModels;

/// <summary>Portapapeles, prefabs y búsqueda en la jerarquía.</summary>
public sealed partial class MainViewModel
{
    public event Action? ComponentsChanged;

    public void NotifyComponentsChanged() => ComponentsChanged?.Invoke();
    private List<string> _clipboard = new();
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

    /// <summary>Aplica y guarda el tema de la interfaz.</summary>
    public void SetTheme(Styles.EditorThemeMode mode)
    {
        Styles.EditorTheme.Apply(mode);
        _settings.Theme = mode.ToString();
        _settings.Save();
        RequestRedraw();
    }

    /// <summary>Carpeta de prefabs del proyecto (o una local si no hay proyecto).</summary>
    public string PrefabsFolder => Project?.PrefabsFolder ?? Path.Combine(ProjectRoot, "prefabs");

    /// <summary>Scripts .cs disponibles en la carpeta scripts del proyecto.</summary>
    public IReadOnlyList<string> GetProjectScripts()
    {
        var folder = Path.Combine(ProjectRoot, "scripts");
        if (!Directory.Exists(folder)) return Array.Empty<string>();

        try
        {
            return Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }
        catch
        {
            return Array.Empty<string>();
        }
    }

    /// <summary>Campos públicos del script compilado, para el inspector.</summary>
    public IReadOnlyList<ScriptFieldDefinition> GetScriptFields(string className)
    {
        if (string.IsNullOrWhiteSpace(className)) return Array.Empty<ScriptFieldDefinition>();
        var type = _scripts.Types.FirstOrDefault(item => item.Name == className || item.FullName == className)?.Type;
        if (type is null) return Array.Empty<ScriptFieldDefinition>();

        static bool Supported(Type type) => type == typeof(float) || type == typeof(int) ||
            type == typeof(bool) || type == typeof(string) || type == typeof(double);

        static string Format(object? value, Type type)
        {
            if (value is null) return string.Empty;
            if (type == typeof(bool)) return (bool)value ? "true" : "false";
            if (type == typeof(float)) return ((float)value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(double)) return ((double)value).ToString("0.###", System.Globalization.CultureInfo.InvariantCulture);
            if (type == typeof(int)) return ((int)value).ToString(System.Globalization.CultureInfo.InvariantCulture);
            return value.ToString() ?? string.Empty;
        }

        object? instance = null;
        try { instance = Activator.CreateInstance(type); } catch { }

        var flags = System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance;
        var result = new List<ScriptFieldDefinition>();
        foreach (var field in type.GetFields(flags))
        {
            if (!Supported(field.FieldType)) continue;
            object? value = null;
            try { value = field.GetValue(instance); } catch { }
            result.Add(new ScriptFieldDefinition(field.Name, field.FieldType.Name, Format(value, field.FieldType)));
        }
        foreach (var property in type.GetProperties(flags))
        {
            if (!property.CanRead || !property.CanWrite || !Supported(property.PropertyType)) continue;
            object? value = null;
            try { value = property.GetValue(instance); } catch { }
            result.Add(new ScriptFieldDefinition(property.Name, property.PropertyType.Name, Format(value, property.PropertyType)));
        }
        return result;
    }
    public void ClearSearch() => SearchText = string.Empty;

    /// <summary>Copia la selección al portapapeles interno (JSON por entidad).</summary>
    public void CopySelected()
    {
        var nodes = _selection.Count > 0
            ? _selection.ToList()
            : Selected is { } primary ? new List<EntityNode> { primary } : new List<EntityNode>();

        if (nodes.Count == 0)
        {
            StatusMessage = "Selecciona una entidad para copiar";
            return;
        }

        _clipboard = nodes.Select(node => PrefabSerializer.Serialize(node.Entity)).ToList();
        StatusMessage = nodes.Count == 1 ? $"Copiado: {nodes[0].Name}" : $"{nodes.Count} entidades copiadas";
        Log.Info(nodes.Count == 1 ? $"Entidad copiada: {nodes[0].Name}" : $"{nodes.Count} entidades copiadas", "Editor");
    }

    /// <summary>Pega las entidades copiadas con desplazamiento y nombres únicos.</summary>
    public void PasteCopied()
    {
        if (IsPlaying) return;

        if (_clipboard.Count == 0)
        {
            StatusMessage = "No hay nada que pegar";
            return;
        }

        try
        {
            var copies = new List<Entity>();
            foreach (var serialized in _clipboard)
            {
                var entity = PrefabSerializer.Deserialize(serialized);
                entity.Id = Guid.NewGuid();
                entity.Name = UniqueName(entity.Name);
                entity.Transform.Position += new Vector2(1f, -0.5f);
                Scene.Add(entity);
                copies.Add(entity);
            }

            RebuildTree();
            SelectMany(copies);
            IsDirty = true;
            CommitEdit(copies.Count == 1 ? "Pegar entidad" : $"Pegar {copies.Count} entidades");
            OnPropertyChanged(nameof(EntityCountText));
            StatusMessage = copies.Count == 1 ? $"Pegado: {copies[0].Name}" : $"{copies.Count} entidades pegadas";
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
            var component = target.AddComponent(new ScriptComponent());
            component.ScriptPath = relative;
            component.ClassName = className;

            RebuildTree();
            Select(target);
            Selected?.RefreshAll();
            NotifyComponentsChanged();
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
        if (IsPlaying || Selected?.ActiveScript is not { } component)
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
            case "sprite":
                if (entity.HasComponent<SpriteRenderer>()) { added = false; break; }
                entity.AddComponent(new SpriteRenderer());
                break;

            case "interactable":
                if (entity.HasComponent<Interactable>()) { added = false; break; }
                entity.AddComponent(new Interactable());
                break;

            case "area":
                if (entity.HasComponent<Collider2D>()) { added = false; break; }
                entity.AddComponent(new Area2D());
                break;

            case "circle":
                if (entity.HasComponent<Collider2D>()) { added = false; break; }
                entity.AddComponent(new CircleCollider2D());
                break;

            case "capsule":
                if (entity.HasComponent<Collider2D>()) { added = false; break; }
                entity.AddComponent(new CapsuleCollider2D());
                break;

            case "edge":
                if (entity.HasComponent<Collider2D>()) { added = false; break; }
                entity.AddComponent(new EdgeCollider2D());
                break;

            case "polygon":
                if (entity.HasComponent<Collider2D>()) { added = false; break; }
                entity.AddComponent(new PolygonCollider2D());
                break;

            case "movingplatform":
                if (entity.HasComponent<MovingPlatform2D>()) { added = false; break; }
                entity.AddComponent(new MovingPlatform2D());
                break;

            case "turnactor":
                if (entity.HasComponent<TurnActor>()) { added = false; break; }
                entity.AddComponent(new TurnActor());
                break;

            case "turnmanager":
                if (entity.HasComponent<TurnManager>()) { added = false; break; }
                entity.AddComponent(new TurnManager());
                break;

            case "skeleton":
                if (entity.HasComponent<Skeleton2D>()) { added = false; break; }
                entity.AddComponent(new Skeleton2D());
                break;

            case "carddeck":
                if (entity.HasComponent<CardDeck>()) { added = false; break; }
                entity.AddComponent(new CardDeck());
                break;

            case "dungeon":
                if (entity.HasComponent<DungeonGenerator2D>()) { added = false; break; }
                if (!entity.HasComponent<Tilemap>()) entity.AddComponent(new Tilemap());
                entity.AddComponent(new DungeonGenerator2D());
                break;

            case "equipment":
                if (entity.HasComponent<Equipment>()) { added = false; break; }
                entity.AddComponent(new Equipment());
                break;

            case "questlog":
                if (entity.HasComponent<QuestLog>()) { added = false; break; }
                entity.AddComponent(new QuestLog());
                break;

            case "animfsm":
                if (entity.HasComponent<AnimationStateMachine>()) { added = false; break; }
                if (!entity.HasComponent<SpriteAnimator>()) entity.AddComponent(new SpriteAnimator());
                entity.AddComponent(new AnimationStateMachine());
                break;

            case "transition":
                if (entity.HasComponent<ScreenTransition2D>()) { added = false; break; }
                entity.AddComponent(new ScreenTransition2D());
                break;

            case "particles":
                if (entity.HasComponent<ParticleEmitter2D>()) { added = false; break; }
                entity.AddComponent(new ParticleEmitter2D());
                break;

            case "trail":
                if (entity.HasComponent<Trail2D>()) { added = false; break; }
                entity.AddComponent(new Trail2D());
                break;

            case "health":
                if (entity.HasComponent<Health>()) { added = false; break; }
                entity.AddComponent(new Health());
                break;

            case "hitbox":
                if (entity.HasComponent<Hitbox>()) { added = false; break; }
                if (!entity.HasComponent<Collider2D>()) entity.AddComponent(new BoxCollider());
                entity.AddComponent(new Hitbox());
                break;

            case "knockback2d":
                if (entity.HasComponent<Knockback2D>()) { added = false; break; }
                entity.AddComponent(new Knockback2D());
                break;

            case "hurtbox":
                if (entity.HasComponent<Hurtbox>()) { added = false; break; }
                entity.AddComponent(new Hurtbox());
                break;

            case "checkpoint2d":
                if (entity.HasComponent<Checkpoint2D>()) { added = false; break; }
                entity.AddComponent(new Checkpoint2D());
                break;

            case "weapon2d":
                if (entity.HasComponent<Weapon2D>()) { added = false; break; }
                entity.AddComponent(new Weapon2D());
                break;

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

            case "character":
                if (entity.HasComponent<CharacterBody2D>()) { added = false; break; }
                var characterSize = entity.GetComponent<SpriteRenderer>()?.Size ?? Vector2.One;
                entity.AddComponent(new CharacterBody2D());
                if (!entity.HasComponent<Rigidbody2D>())
                    entity.AddComponent(new Rigidbody2D { AffectedByGravity = false });
                if (!entity.HasComponent<BoxCollider>())
                    entity.AddComponent(new BoxCollider { Size = characterSize });
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

            case "camera3d":
                if (entity.HasComponent<Camera3D>()) { added = false; break; }
                entity.AddComponent(new Camera3D());
                break;

            case "wall3d":
                if (entity.HasComponent<Wall3D>()) { added = false; break; }
                entity.AddComponent(new Wall3D());
                break;

            case "fps":
                if (entity.HasComponent<FirstPersonController>()) { added = false; break; }
                entity.AddComponent(new FirstPersonController());
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
        ComponentsChanged?.Invoke();
        CommitEdit("Añadir componente");
        StatusMessage = $"Componente {kind} añadido a {entity.Name}";
        RequestRedraw();
    }

    /// <summary>Quita un componente de la entidad seleccionada (menú de jerarquía).</summary>
    public void RemoveComponent(string kind)
    {
        if (IsPlaying || Selected is null)
        {
            return;
        }

        var entity = Selected.Entity;
        Component? component = kind switch
        {
            "sprite" => entity.GetComponent<SpriteRenderer>(),
            "animator" => entity.GetComponent<SpriteAnimator>(),
            "camera" => entity.GetComponent<Camera2D>(),
            "tilemap" => entity.GetComponents<TileMapLayer>().FirstOrDefault(),
            "tilemaplayer" => entity.GetComponents<TileMapLayer>().LastOrDefault(),
            "canvaslayer" => entity.GetComponent<CanvasLayer>(),
            "animationplayer" => entity.GetComponent<AnimationPlayer2D>(),
            "uicanvas" => entity.GetComponent<UiCanvas>(),
            "raycast2d" => entity.GetComponent<RayCast2D>(),
            "audiosource2d" => entity.GetComponent<AudioSource2D>(),
            "line2d" => entity.GetComponent<Line2D>(),
            "skinnedmesh" => entity.GetComponent<SkinnedMesh2D>(),
            "polygon2d" => entity.GetComponent<Polygon2D>(),
            "navigationpolygon" => entity.GetComponent<NavigationPolygon2D>(),
            "navigationagent" => entity.GetComponent<NavigationAgent2D>(),
            "audiolistener2d" => entity.GetComponent<AudioListener2D>(),
            "springjoint" => entity.GetComponent<SpringJoint2D>(),
            "pinjoint" => entity.GetComponent<PinJoint2D>(),
            "shapecast2d" => entity.GetComponent<ShapeCast2D>(),
            "collider" => entity.GetComponent<Collider2D>(),
            "rigidbody" => entity.GetComponent<Rigidbody2D>(),
            "character" => entity.GetComponent<CharacterBody2D>(),
            "hitbox" => entity.GetComponent<Hitbox>(),
            "health" => entity.GetComponent<Health>(),
            "hurtbox" => entity.GetComponent<Hurtbox>(),
            "knockback2d" => entity.GetComponent<Knockback2D>(),
            "equipment" => entity.GetComponent<Equipment>(),
            "turnactor" => entity.GetComponent<TurnActor>(),
            "turnmanager" => entity.GetComponent<TurnManager>(),
            "carddeck" => entity.GetComponent<CardDeck>(),
            "dungeon" => entity.GetComponent<DungeonGenerator2D>(),
            "skeleton" => entity.GetComponent<Skeleton2D>(),
            "questlog" => entity.GetComponent<QuestLog>(),
            "checkpoint2d" => entity.GetComponent<Checkpoint2D>(),
            "weapon2d" => entity.GetComponent<Weapon2D>(),
            "edge" => entity.GetComponent<EdgeCollider2D>(),
            "audio" => entity.GetComponent<AudioSource>(),
            "light" => entity.GetComponent<Light2D>(),
            "interactable" => entity.GetComponent<Interactable>(),
            "inventory" => entity.GetComponent<Inventory>(),
            "stats" => entity.GetComponent<Stats>(),
            "daynight" => entity.GetComponent<DayNightCycle>(),
            "ai" => entity.GetComponent<AiAgent>(),
            "script" => entity.GetComponent<ScriptComponent>(),
            "rotator" => entity.GetComponent<RotatorComponent>(),
            "oscillator" => entity.GetComponent<OscillatorComponent>(),
            _ => null,
        };

        if (component is null)
        {
            StatusMessage = "La entidad no tiene ese componente";
            return;
        }

        entity.RemoveComponent(component);
        Selected.RefreshAll();
        IsDirty = true;
        CommitEdit("Quitar componente");
        StatusMessage = $"Componente '{kind}' quitado de {entity.Name}";
        RequestRedraw();
    }

    /// <summary>Guarda la entidad seleccionada y sus hijos como blueprint.</summary>
    public void SaveSelectedAsBlueprint(string path)
    {
        if (Selected is null)
        {
            StatusMessage = "Selecciona una entidad para guardarla como blueprint";
            return;
        }

        try
        {
            var normalized = BlueprintSerializer.NormalizePath(path);
            BlueprintSerializer.Save(Scene, Selected.Entity, normalized);
            RefreshProjectFiles();
            StatusMessage = $"Blueprint guardado: {Path.GetFileName(normalized)}";
            Log.Info($"Blueprint guardado: {normalized}", "Editor");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar el blueprint: {exception.Message}", "Editor");
        }
    }

    /// <summary>Instancia un blueprint completo (entidad + hijos).</summary>
    public void InstantiateBlueprint(string path)
    {
        if (IsPlaying)
        {
            return;
        }

        try
        {
            var document = BlueprintSerializer.Load(path);
            var root = BlueprintSerializer.Instantiate(Scene, document, CameraPosition);
            RebuildTree();
            Select(root);
            IsDirty = true;
            CommitEdit("Instanciar blueprint");
            OnPropertyChanged(nameof(EntityCountText));
            StatusMessage = $"Blueprint instanciado: {root.Name}";
            RequestRedraw();
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo instanciar el blueprint: {exception.Message}", "Editor");
        }
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
