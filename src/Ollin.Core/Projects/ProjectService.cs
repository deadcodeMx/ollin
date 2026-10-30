using System.Text.Json;

namespace Ollin.Core.Projects;

/// <summary>Datos de un proyecto de Ollin (`project.ollin`) y su carpeta raíz.</summary>
public sealed class ProjectInfo
{
    public string Name { get; set; } = "Proyecto";

    public int Version { get; set; } = 1;

    /// <summary>Escena que se abre al cargar el proyecto.</summary>
    public string StartScene { get; set; } = "scenes/inicio.ollin.json";

    public string? Description { get; set; }

    /// <summary>Carpeta raíz del proyecto (no se guarda en el archivo).</summary>
    public string RootPath { get; set; } = string.Empty;

    public string ProjectFile => Path.Combine(RootPath, ProjectService.FileName);

    public string ScenesFolder => Path.Combine(RootPath, "scenes");

    public string ScriptsFolder => Path.Combine(RootPath, "scripts");

    public string AssetsFolder => Path.Combine(RootPath, "assets");

    public string PrefabsFolder => Path.Combine(RootPath, "prefabs");

    public string StartSceneFullPath => Path.Combine(RootPath, StartScene.Replace('/', Path.DirectorySeparatorChar));
}

/// <summary>Crea, abre y guarda proyectos con su estructura de carpetas.</summary>
public static class ProjectService
{
    public const string FileName = "project.ollin";

    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    /// <summary>
    /// Crea un proyecto nuevo en <paramref name="parentFolder"/> con una escena y un
    /// script iniciales. Si se indica <paramref name="templateAssets"/>, copia esas
    /// texturas al proyecto.
    /// </summary>
    public static ProjectInfo Create(string parentFolder, string name, string? templateAssets = null, ProjectTemplate template = ProjectTemplate.Empty)
    {
        var folderName = Sanitize(name);
        var root = Path.Combine(parentFolder, folderName);

        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            throw new InvalidOperationException($"La carpeta '{root}' ya existe y no está vacía.");
        }

        Directory.CreateDirectory(root);
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        Directory.CreateDirectory(Path.Combine(root, "scripts"));
        Directory.CreateDirectory(Path.Combine(root, "scenes"));
        Directory.CreateDirectory(Path.Combine(root, "prefabs"));
        Directory.CreateDirectory(Path.Combine(root, "dialogs"));

        var project = new ProjectInfo
        {
            Name = name.Trim(),
            RootPath = root,
            StartScene = "scenes/inicio.ollin.json",
        };

        if (!string.IsNullOrWhiteSpace(templateAssets) && Directory.Exists(templateAssets))
        {
            CopyDirectory(templateAssets, Path.Combine(root, "assets"));
        }

        var scene = StarterProject.CreateScene(template);
        Serialization.SceneSerializer.Save(scene, project.StartSceneFullPath);

        var (scriptFile, scriptSource) = StarterProject.ScriptFor(template);
        File.WriteAllText(Path.Combine(root, "scripts", scriptFile), scriptSource);
        Directory.CreateDirectory(Path.Combine(root, "dialogs"));
        File.WriteAllText(Path.Combine(root, "dialogs", "bienvenida.json"), StarterProject.WelcomeDialogue);

        Save(project);
        return project;
    }

    /// <summary>Busca el `project.ollin` subiendo desde un archivo o carpeta.</summary>
    public static ProjectInfo? Find(string path)
    {
        var directory = File.Exists(path)
            ? Path.GetDirectoryName(Path.GetFullPath(path))
            : Path.GetFullPath(path);

        for (var depth = 0; depth < 12 && !string.IsNullOrEmpty(directory); depth++)
        {
            var candidate = Path.Combine(directory, FileName);
            if (File.Exists(candidate))
            {
                return Load(candidate);
            }

            directory = Path.GetDirectoryName(directory);
        }

        return null;
    }

    public static ProjectInfo Load(string projectFilePath)
    {
        var fullPath = Path.GetFullPath(projectFilePath);
        var json = File.ReadAllText(fullPath);
        var project = JsonSerializer.Deserialize<ProjectInfo>(json, Options)
            ?? throw new InvalidOperationException("El archivo de proyecto no es válido.");

        project.RootPath = Path.GetDirectoryName(fullPath)!;
        return project;
    }

    /// <summary>Abre una carpeta que no tiene `project.ollin` (por ejemplo, la de ejemplo).</summary>
    public static ProjectInfo OpenFolder(string root, string? name = null)
    {
        var full = Path.GetFullPath(root);
        var project = new ProjectInfo
        {
            Name = name ?? new DirectoryInfo(full).Name,
            RootPath = full,
            StartScene = string.Empty,
        };

        var scenes = Path.Combine(full, "scenes");
        if (Directory.Exists(scenes))
        {
            var first = Directory.EnumerateFiles(scenes, "*.json").OrderBy(f => f).FirstOrDefault();
            if (first is not null)
            {
                project.StartScene = Path.GetRelativePath(full, first).Replace('\\', '/');
            }
        }

        return project;
    }

    public static void Save(ProjectInfo project)
    {
        Directory.CreateDirectory(project.RootPath);
        var json = JsonSerializer.Serialize(project, Options);
        File.WriteAllText(project.ProjectFile, json);
    }

    /// <summary>Nombre de carpeta que se usará para el proyecto.</summary>
    public static string FolderNameFor(string name) => Sanitize(name);

    /// <summary>Comprueba si se puede crear el proyecto sin tocar el disco.</summary>
    public static bool CanCreate(string? parentFolder, string? name, out string? error)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            error = "Escribe un nombre para el proyecto.";
            return false;
        }

        if (string.IsNullOrWhiteSpace(parentFolder))
        {
            error = "Elige la carpeta donde crear el proyecto.";
            return false;
        }

        var target = Path.Combine(parentFolder!, FolderNameFor(name!));

        try
        {
            if (Directory.Exists(target) && Directory.EnumerateFileSystemEntries(target).Any())
            {
                error = $"Ya existe '{target}' y no está vacía. Usa otro nombre o carpeta.";
                return false;
            }
        }
        catch (Exception exception)
        {
            error = $"No se puede usar esa carpeta: {exception.Message}";
            return false;
        }

        error = null;
        return true;
    }

    private static string Sanitize(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var cleaned = new string(name.Trim().Select(c => invalid.Contains(c) ? '_' : c).ToArray());
        cleaned = cleaned.Replace(' ', '_');

        if (string.IsNullOrWhiteSpace(cleaned))
        {
            cleaned = "Proyecto";
        }

        return cleaned;
    }

    private static void CopyDirectory(string source, string destination)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var relative = Path.GetRelativePath(source, file);
            var target = Path.Combine(destination, relative);
            Directory.CreateDirectory(Path.GetDirectoryName(target)!);
            File.Copy(file, target, overwrite: true);
        }
    }
}
