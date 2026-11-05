namespace Ollin.Core.Scenes;

/// <summary>Solicitud de cambio de escena para que el runtime la procese en su ciclo.</summary>
public static class SceneLoadQueue
{
    private static string? _pending;

    public static void Request(string path) => _pending = path;

    public static bool TryTake(out string? path)
    {
        path = _pending;
        _pending = null;
        return !string.IsNullOrWhiteSpace(path);
    }
}
