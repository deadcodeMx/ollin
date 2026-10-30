using System.Reflection;
using System.Runtime.Loader;
using Ollin.Core.Diagnostics;
using Ollin.Core.Model;

namespace Ollin.Scripting;

/// <summary>Un tipo de script encontrado en el ensamblado compilado.</summary>
public sealed record ScriptTypeInfo(string Name, string FullName, Type Type);

/// <summary>Resultado de recargar los scripts del proyecto.</summary>
public sealed record ScriptReloadResult(bool Success, IReadOnlyList<ScriptDiagnostic> Diagnostics, IReadOnlyList<ScriptTypeInfo> Types)
{
    public static readonly ScriptReloadResult Empty = new(true, Array.Empty<ScriptDiagnostic>(), Array.Empty<ScriptTypeInfo>());
}

/// <summary>
/// Compila y mantiene los scripts de un proyecto. Cada recarga compila en memoria y
/// carga el resultado en un <see cref="AssemblyLoadContext"/> recolectable, de modo
/// que el ensamblado anterior se puede descargar (recarga en caliente).
/// </summary>
public sealed class ScriptWorkspace : IDisposable
{
    private AssemblyLoadContext? _loadContext;
    private Assembly? _assembly;
    private List<ScriptTypeInfo> _types = new();

    public ScriptWorkspace(string projectRoot)
    {
        ProjectRoot = projectRoot;
    }

    public string ProjectRoot { get; }

    public string ScriptsFolder => Path.Combine(ProjectRoot, "scripts");

    public IReadOnlyList<ScriptTypeInfo> Types => _types;

    public IReadOnlyList<ScriptDiagnostic> LastDiagnostics { get; private set; } = Array.Empty<ScriptDiagnostic>();

    public bool HasScripts => _assembly is not null && _types.Count > 0;

    /// <summary>Recompila todos los scripts y reemplaza el ensamblado anterior.</summary>
    public ScriptReloadResult Reload()
    {
        UnloadPrevious();

        var folder = ScriptsFolder;
        var hasScripts = Directory.Exists(folder) &&
                         Directory.EnumerateFiles(folder, "*.cs", SearchOption.AllDirectories).Any();

        if (!hasScripts)
        {
            LastDiagnostics = Array.Empty<ScriptDiagnostic>();
            return ScriptReloadResult.Empty;
        }

        var compilation = ScriptCompiler.CompileDirectory(folder);
        LastDiagnostics = compilation.Diagnostics;

        foreach (var diagnostic in compilation.Diagnostics)
        {
            if (diagnostic.IsError)
            {
                Log.Error(diagnostic.Text, "Scripts");
            }
            else
            {
                Log.Warn(diagnostic.Text, "Scripts");
            }
        }

        if (!compilation.Success || compilation.Image is null)
        {
            return new ScriptReloadResult(compilation.Success, compilation.Diagnostics, Array.Empty<ScriptTypeInfo>());
        }

        _loadContext = new AssemblyLoadContext($"Ollin.Scripts.{Guid.NewGuid():N}", isCollectible: true);
        using var stream = new MemoryStream(compilation.Image);
        _assembly = _loadContext.LoadFromStream(stream);
        _types = DiscoverTypes(_assembly);

        if (_types.Count > 0)
        {
            Log.Info($"Scripts compilados: {_types.Count} ({string.Join(", ", _types.Select(t => t.Name))})", "Scripts");
        }

        return new ScriptReloadResult(true, compilation.Diagnostics, _types);
    }

    /// <summary>
    /// Carga scripts ya compilados (por ejemplo `game.dll` de un juego exportado).
    /// No compila ni necesita los archivos `.cs`.
    /// </summary>
    public ScriptReloadResult LoadImage(byte[] image)
    {
        UnloadPrevious();

        _loadContext = new AssemblyLoadContext($"Ollin.Scripts.{Guid.NewGuid():N}", isCollectible: true);
        using var stream = new MemoryStream(image);
        _assembly = _loadContext.LoadFromStream(stream);
        _types = DiscoverTypes(_assembly);

        return new ScriptReloadResult(true, Array.Empty<ScriptDiagnostic>(), _types);
    }

    /// <summary>Crea una instancia del script indicado, o null si no existe.</summary>
    public OllinScript? CreateInstance(string? className)
    {
        if (string.IsNullOrWhiteSpace(className))
        {
            return null;
        }

        var type = _types.FirstOrDefault(t =>
                string.Equals(t.Name, className, StringComparison.Ordinal) ||
                string.Equals(t.FullName, className, StringComparison.Ordinal))
            ?.Type;

        if (type is null)
        {
            Log.Warn($"No se encontró el script '{className}'", "Scripts");
            return null;
        }

        try
        {
            return Activator.CreateInstance(type) as OllinScript;
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo crear '{className}': {exception.Message}", "Scripts");
            return null;
        }
    }

    private static List<ScriptTypeInfo> DiscoverTypes(Assembly assembly)
    {
        Type?[] types;
        try
        {
            types = assembly.GetTypes();
        }
        catch (ReflectionTypeLoadException exception)
        {
            types = exception.Types;
        }

        return types
            .Where(t => t is { IsAbstract: false, IsInterface: false } && typeof(OllinScript).IsAssignableFrom(t))
            .Select(t => new ScriptTypeInfo(t!.Name, t.FullName ?? t.Name, t))
            .OrderBy(t => t.Name, StringComparer.Ordinal)
            .ToList();
    }

    private void UnloadPrevious()
    {
        _types = new List<ScriptTypeInfo>();
        _assembly = null;

        if (_loadContext is null)
        {
            return;
        }

        try
        {
            _loadContext.Unload();
        }
        catch (Exception exception)
        {
            Log.Warn($"No se pudo descargar el ensamblado anterior: {exception.Message}", "Scripts");
        }

        _loadContext = null;
    }

    public void Dispose() => UnloadPrevious();
}
