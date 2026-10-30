using System.Numerics;
using Ollin.Core.History;
using Ollin.Core.Scenes;

namespace Ollin.Editor.ViewModels;

/// <summary>
/// Estado de una escena abierta: documento, cámara, selección e historial propios.
/// Permite tener varias escenas en pestañas sin que se pisen entre ellas.
/// </summary>
public sealed class SceneDocument
{
    public SceneDocument(Scene scene, string? path, SceneHistory? history = null)
    {
        Scene = scene;
        Path = path;
        History = history ?? new SceneHistory();
        History.Initialize(scene);
    }

    public Scene Scene { get; set; }

    public string? Path { get; set; }

    public bool IsDirty { get; set; }

    public Vector2 CameraPosition { get; set; }

    public float Zoom { get; set; } = 1f;

    public Guid? SelectedId { get; set; }

    public SceneHistory History { get; }

    public string Title
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(Path))
            {
                var name = System.IO.Path.GetFileName(Path);
                return name.EndsWith(".ollin.json", StringComparison.OrdinalIgnoreCase)
                    ? name[..^".ollin.json".Length]
                    : System.IO.Path.GetFileNameWithoutExtension(name);
            }

            return Scene.Name;
        }
    }
}
