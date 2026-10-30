using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Ollin.Editor.ViewModels;

/// <summary>Pestaña del área central: la escena o un script abierto.</summary>
public sealed class DocumentTab : INotifyPropertyChanged
{
    private string _text = string.Empty;
    private bool _isDirty;
    private bool _isActive;

    public DocumentTab(string title, bool isScene, string? fullPath = null, SceneDocument? scene = null)
    {
        Title = title;
        IsScene = isScene;
        FullPath = fullPath;
        SceneDocument = scene;
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; private set; }

    /// <summary>Escena asociada cuando la pestaña es de escena.</summary>
    public SceneDocument? SceneDocument { get; }

    public void UpdateTitle(string title)
    {
        if (Title == title)
        {
            return;
        }

        Title = title;
        OnPropertyChanged(nameof(Title));
        OnPropertyChanged(nameof(TitleWithDirty));
    }

    public bool IsScene { get; }

    public bool IsScript => !IsScene;

    public string? FullPath { get; }

    public string Text
    {
        get => _text;
        set
        {
            if (_text == value)
            {
                return;
            }

            _text = value;
            IsDirty = true;
            OnPropertyChanged();
        }
    }

    /// <summary>Texto sin marcar cambios (al cargar desde disco).</summary>
    public void LoadText(string text)
    {
        _text = text;
        _isDirty = false;
        OnPropertyChanged(nameof(Text));
        OnPropertyChanged(nameof(IsDirty));
        OnPropertyChanged(nameof(TitleWithDirty));
    }

    public bool IsDirty
    {
        get => _isDirty;
        set
        {
            if (_isDirty == value)
            {
                return;
            }

            _isDirty = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(TitleWithDirty));
        }
    }

    public string TitleWithDirty => IsDirty ? $"{Title} ●" : Title;

    public bool IsActive
    {
        get => _isActive;
        set
        {
            if (_isActive == value)
            {
                return;
            }

            _isActive = value;
            OnPropertyChanged();
        }
    }

    private void OnPropertyChanged([CallerMemberName] string? name = null) =>
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
}

/// <summary>Nodo del explorador de archivos del proyecto.</summary>
public sealed class FileNode
{
    public FileNode(string name, string fullPath, bool isFolder)
    {
        Name = name;
        FullPath = fullPath;
        IsFolder = isFolder;
    }

    public string Name { get; }

    public string FullPath { get; }

    public bool IsFolder { get; }

    public ObservableCollection<FileNode> Children { get; } = new();

    public string Icon => IsFolder ? "▸" : Path.GetExtension(FullPath).ToLowerInvariant() switch
    {
        ".cs" => "C#",
        ".json" => "◇",
        ".png" or ".jpg" or ".jpeg" or ".bmp" => "▣",
        _ => "·",
    };
}
