using System.Reflection;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace Ollin.Scripting;

/// <summary>Diagnóstico de compilación de un script, con archivo y línea.</summary>
public sealed record ScriptDiagnostic(string Id, string Message, string? File, int Line, int Column, bool IsError)
{
    public string Text =>
        $"{Path.GetFileName(File ?? "script.cs")}({Line},{Column}): {(IsError ? "error" : "aviso")} {Id}: {Message}";
}

/// <summary>Resultado de compilar el conjunto de scripts del proyecto.</summary>
public sealed record ScriptCompilation(byte[]? Image, IReadOnlyList<ScriptDiagnostic> Diagnostics)
{
    public bool Success => Image is not null && Diagnostics.All(d => !d.IsError);
}

/// <summary>
/// Compila scripts en memoria con Roslyn. No toca el disco más allá de leer los
/// archivos: el resultado es un ensamblado en bytes que el workspace carga en un
/// contexto recolectable para poder recargarlo.
/// </summary>
public static class ScriptCompiler
{
    public static ScriptCompilation CompileDirectory(string scriptsFolder)
    {
        if (!Directory.Exists(scriptsFolder))
        {
            return new ScriptCompilation(null, Array.Empty<ScriptDiagnostic>());
        }

        var files = Directory
            .EnumerateFiles(scriptsFolder, "*.cs", SearchOption.AllDirectories)
            .OrderBy(f => f, StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (files.Length == 0)
        {
            return new ScriptCompilation(null, Array.Empty<ScriptDiagnostic>());
        }

        var sources = files.Select(file => (file, File.ReadAllText(file)));
        return Compile(sources);
    }

    public static ScriptCompilation Compile(IEnumerable<(string Path, string Source)> sources)
    {
        var parseOptions = new CSharpParseOptions(LanguageVersion.Latest, DocumentationMode.None);
        var trees = new List<SyntaxTree>();

        trees.Add(CSharpSyntaxTree.ParseText(
            "global using System;global using System.Collections.Generic;",
            parseOptions, path: "__OllinGlobalUsings.cs"));

        foreach (var (path, source) in sources)
        {
            trees.Add(CSharpSyntaxTree.ParseText(source, parseOptions, path: path));
        }

        var compilation = CSharpCompilation.Create(
            assemblyName: "Ollin.UserScripts",
            syntaxTrees: trees,
            references: GetReferences(),
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary,
                optimizationLevel: OptimizationLevel.Debug,
                allowUnsafe: true,
                nullableContextOptions: NullableContextOptions.Disable));

        using var stream = new MemoryStream();
        var result = compilation.Emit(stream);
        var diagnostics = result.Diagnostics
            .Where(d => d.Severity is DiagnosticSeverity.Error or DiagnosticSeverity.Warning)
            .Select(ToDiagnostic)
            .ToArray();

        return result.Success
            ? new ScriptCompilation(stream.ToArray(), diagnostics)
            : new ScriptCompilation(null, diagnostics);
    }

    /// <summary>Referencias: toda la plataforma .NET más los ensamblados del motor.</summary>
    public static IReadOnlyList<MetadataReference> GetReferences()
    {
        var references = new List<MetadataReference>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        if (AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES") is string trusted)
        {
            foreach (var path in trusted.Split(Path.PathSeparator, StringSplitOptions.RemoveEmptyEntries))
            {
                if (seen.Add(path))
                {
                    references.Add(MetadataReference.CreateFromFile(path));
                }
            }
        }

        foreach (var assembly in new[] { typeof(OllinScript).Assembly, typeof(Core.Model.Entity).Assembly })
        {
            if (!assembly.IsDynamic && !string.IsNullOrEmpty(assembly.Location) && seen.Add(assembly.Location))
            {
                references.Add(MetadataReference.CreateFromFile(assembly.Location));
            }
        }

        return references;
    }

    private static ScriptDiagnostic ToDiagnostic(Diagnostic diagnostic)
    {
        var span = diagnostic.Location.GetLineSpan();
        var line = span.StartLinePosition.Line + 1;
        var column = span.StartLinePosition.Character + 1;
        var file = span.Path;

        return new ScriptDiagnostic(
            diagnostic.Id,
            diagnostic.GetMessage(),
            string.IsNullOrEmpty(file) ? null : file,
            line,
            column,
            diagnostic.Severity == DiagnosticSeverity.Error);
    }
}
