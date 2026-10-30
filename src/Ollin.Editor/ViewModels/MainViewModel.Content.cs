using System.Numerics;
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
}
