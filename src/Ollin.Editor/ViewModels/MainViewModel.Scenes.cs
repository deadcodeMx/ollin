using Ollin.Core.History;
using Ollin.Core.Scenes;

namespace Ollin.Editor.ViewModels;

/// <summary>Escenas múltiples: cada pestaña conserva su escena, cámara, selección e historial.</summary>
public sealed partial class MainViewModel
{
    private SceneDocument? _currentSceneDoc;

    /// <summary>Crea una pestaña con una escena nueva y la activa.</summary>
    private void AddSceneDocument(Scene scene, string? path)
    {
        var document = new SceneDocument(scene, path);
        var tab = new DocumentTab(document.Title, isScene: true, scene: document);
        Documents.Add(tab);
        ActiveDocument = tab;
    }

    /// <summary>Guarda el estado de la escena activa antes de cambiar de pestaña.</summary>
    private void StoreCurrentSceneState()
    {
        if (_currentSceneDoc is null)
        {
            return;
        }

        _currentSceneDoc.Scene = _scene;
        _currentSceneDoc.Path = _scenePath;
        _currentSceneDoc.IsDirty = _isDirty;
        _currentSceneDoc.CameraPosition = CameraPosition;
        _currentSceneDoc.Zoom = ActiveCamera.Zoom;
        _currentSceneDoc.SelectedId = Selected?.Id;
    }

    /// <summary>Activa una escena ya cargada, restaurando cámara, selección e historial.</summary>
    private void ActivateSceneDocument(SceneDocument document)
    {
        if (ReferenceEquals(_currentSceneDoc, document))
        {
            return;
        }

        StoreCurrentSceneState();

        _currentSceneDoc = document;
        _scene = document.Scene;
        _scenePath = document.Path;
        _isDirty = document.IsDirty;
        _history = document.History;

        RebuildTree();
        Selected = document.SelectedId is { } id ? FindNode(id) : null;

        MoveCamera(document.CameraPosition);
        ActiveCamera.Zoom = document.Zoom;
        _homeCameraPosition = document.CameraPosition;
        _homeZoom = document.Zoom;

        OnPropertyChanged(nameof(Scene));
        OnPropertyChanged(nameof(ScenePath));
        OnPropertyChanged(nameof(ScenePathText));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(DirtyMarker));
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(EntityCountText));
        OnPropertyChanged(nameof(ZoomText));
        NotifyHistory();
        RequestRedraw();
    }

    /// <summary>Título que debe mostrar la pestaña de la escena activa.</summary>
    private string CurrentSceneTitle => _currentSceneDoc?.Title ?? Scene.Name;

    private void RefreshSceneTabTitle()
    {
        if (ActiveDocument is { IsScene: true } tab)
        {
            tab.UpdateTitle(CurrentSceneTitle);
        }
    }
}
