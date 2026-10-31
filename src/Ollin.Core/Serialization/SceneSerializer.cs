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
        options.Converters.Add(new Vector3JsonConverter());
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

    /// <summary>Intenta cargar una escena sin lanzar excepciones hacia la UI.</summary>
    public static bool TryLoad(string path, out Scene? scene, out string? error)
    {
        try
        {
            var json = File.ReadAllText(path);
            return TryDeserialize(json, out scene, out error);
        }
        catch (Exception exception)
        {
            scene = null;
            error = exception switch
            {
                FileNotFoundException => $"No existe la escena: {path}",
                DirectoryNotFoundException => $"No existe la carpeta de la escena: {path}",
                UnauthorizedAccessException => $"Sin permisos para leer: {path}",
                _ => $"No se pudo leer la escena: {exception.Message}",
            };
            return false;
        }
    }

    /// <summary>Intenta deserializar JSON y devuelve un mensaje con línea/columna.</summary>
    public static bool TryDeserialize(string json, out Scene? scene, out string? error)
    {
        try
        {
            scene = Deserialize(json);
            error = null;
            return true;
        }
        catch (JsonException exception)
        {
            scene = null;
            error = $"JSON inválido en línea {exception.LineNumber + 1}, columna {exception.BytePositionInLine + 1}: {exception.Message}";
            return false;
        }
        catch (Exception exception)
        {
            scene = null;
            error = $"La escena no es válida: {exception.Message}";
            return false;
        }
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
