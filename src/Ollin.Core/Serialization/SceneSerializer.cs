using System.Text.Json;
using System.Text.Json.Serialization;
using Ollin.Core.Model;
using Ollin.Core.Scenes;

namespace Ollin.Core.Serialization;

/// <summary>
/// Guarda y carga escenas en JSON legible. El formato es estable: incluye la
/// versión del formato para poder migrarlo en el futuro.
/// </summary>
public static class SceneSerializer
{
    public const int CurrentFormatVersion = 1;

    public static JsonSerializerOptions Options { get; } = CreateOptions();

    private static JsonSerializerOptions CreateOptions()
    {
        var options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true,
        };

        options.Converters.Add(new Vector2JsonConverter());
        options.Converters.Add(new Vector4JsonConverter());
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }

    public static string Serialize(Scene scene)
    {
        var document = new SceneDocument
        {
            FormatVersion = CurrentFormatVersion,
            Scene = scene,
        };

        return JsonSerializer.Serialize(document, Options);
    }

    public static Scene Deserialize(string json)
    {
        var document = JsonSerializer.Deserialize<SceneDocument>(json, Options)
            ?? throw new InvalidOperationException("El archivo de escena está vacío o no es válido.");

        var scene = document.Scene ?? throw new InvalidOperationException("El archivo no contiene una escena.");
        scene.Rebind();
        return scene;
    }

    public static void Save(Scene scene, string path)
    {
        var directory = Path.GetDirectoryName(Path.GetFullPath(path));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        File.WriteAllText(path, Serialize(scene));
    }

    public static Scene Load(string path) => Deserialize(File.ReadAllText(path));
}

/// <summary>Envoltura con metadatos del archivo .ollin-scene.</summary>
public sealed class SceneDocument
{
    public int FormatVersion { get; set; } = SceneSerializer.CurrentFormatVersion;

    public DateTime SavedAt { get; set; } = DateTime.Now;

    public string? Engine { get; set; } = "Ollin";

    public Scene? Scene { get; set; }
}
