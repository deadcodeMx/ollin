using System.Collections.Specialized;
using System.Reflection;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Highlighting.Xshd;
using Ollin.Scripting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Platform.Storage;
using Ollin.Core.Assets;
using Ollin.Core.Input;
using Ollin.Editor.Controls;
using Ollin.Editor.ViewModels;

namespace Ollin.Editor.Views;

public partial class MainWindow : Window
{
    private static readonly FilePickerFileType SceneFileType = new("Escena de Ollin")
    {
        Patterns = new[] { "*.ollin.json", "*.json" },
    };

    private bool _loadingScript;
    private CompletionWindow? _completionWindow;

    public MainWindow()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
        SetupScriptEditor();
        SetupAudio();
        SetupDragAndDrop();
    }

    private void SetupAudio()
    {
        try
        {
            var backend = new Ollin.Editor.Audio.NAudioBackend();
            Ollin.Core.Audio.Audio.Initialize(backend);
            Closed += (_, _) =>
            {
                Ollin.Core.Audio.Audio.Shutdown();
                backend.Dispose();
            };
        }
        catch (Exception exception)
        {
            Ollin.Core.Diagnostics.Log.Warn($"Sin salida de audio: {exception.Message}", "Audio");
        }
    }

    private void SetupScriptEditor()
    {
        ScriptEditor.Options.ConvertTabsToSpaces = true;
        ScriptEditor.Options.IndentationSize = 4;

        try
        {
            using var reader = XmlReader.Create(new StringReader(SyntaxTheme.CSharpDark));
            ScriptEditor.SyntaxHighlighting = HighlightingLoader.Load(reader, HighlightingManager.Instance);
        }
        catch
        {
            // Sin resaltado el editor sigue siendo usable.
        }

        ScriptEditor.TextArea.TextEntered += OnScriptTextEntered;

        ScriptEditor.TextChanged += (_, _) =>
        {
            if (_loadingScript || ViewModel?.ActiveScript is not { } document)
            {
                return;
            }

            if (!string.Equals(ScriptEditor.Text, document.Text, StringComparison.Ordinal))
            {
                document.Text = ScriptEditor.Text ?? string.Empty;
            }
        };
    }

    private void OnScriptTextEntered(object? sender, TextInputEventArgs e)
    {
        var text = e.Text;

        if (text == ".")
        {
            ShowCompletion(CompletionCatalog.Members, ScriptEditor.CaretOffset);
            return;
        }

        if (_completionWindow is not null && text is { Length: 1 } && (char.IsLetterOrDigit(text[0]) || text[0] == '_'))
        {
            _completionWindow.CompletionList.SelectItem(text);
        }
    }

    private int WordStartOffset()
    {
        var text = ScriptEditor.Document.Text ?? string.Empty;
        var index = ScriptEditor.CaretOffset;
        while (index > 0 && (char.IsLetterOrDigit(text[index - 1]) || text[index - 1] == '_'))
        {
            index--;
        }

        return index;
    }

    private void ShowCompletion(IReadOnlyList<CompletionEntry> entries, int startOffset)
    {
        if (ViewModel?.IsScriptActive != true || ScriptEditor.Document is null)
        {
            return;
        }

        _completionWindow?.Close();

        var window = new CompletionWindow(ScriptEditor)
        {
            StartOffset = startOffset,
            EndOffset = ScriptEditor.CaretOffset,
        };

        foreach (var entry in entries)
        {
            window.CompletionList.CompletionData.Add(new OllinCompletionData(entry.Text, entry.Description));
        }

        window.Closed += (_, _) => _completionWindow = null;
        _completionWindow = window;
        window.Show();
    }

    private void LoadActiveScriptText()
    {
        _loadingScript = true;
        ScriptEditor.Text = ViewModel?.ActiveScript?.Text ?? string.Empty;
        _loadingScript = false;
    }

    private MainViewModel? ViewModel => DataContext as MainViewModel;

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is not MainViewModel vm)
        {
            return;
        }

        ViewportHost.Viewport.ViewModel = vm;
        vm.Logs.CollectionChanged += OnLogsChanged;
        vm.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName is nameof(ViewModels.MainViewModel.ActiveDocument) or nameof(ViewModels.MainViewModel.ActiveScript))
            {
                LoadActiveScriptText();
            }
        };

        // El proyecto puede haberse abierto antes de crear la ventana (por ejemplo con --script).
        LoadActiveScriptText();
        vm.TexturesReloadRequested += () => ViewportHost.Viewport.RequestTextureReload();
    }

    private void OnLogsChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add || LogList.ItemCount == 0)
        {
            return;
        }

        // Se difiere al final del layout: con virtualización, desplazar antes de medir no surte efecto.
        Avalonia.Threading.Dispatcher.UIThread.Post(
            () =>
            {
                if (LogList.ItemCount > 0)
                {
                    LogList.ScrollIntoView(LogList.ItemCount - 1);
                }
            },
            Avalonia.Threading.DispatcherPriority.Background);
    }

    // ---------- Archivo ----------

    private void OnNewScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.NewScene();

    private async void OnOpenScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Abrir escena",
            AllowMultiple = false,
            FileTypeFilter = new[] { SceneFileType },
        });

        var path = files.Count > 0 ? files[0].TryGetLocalPath() : null;
        if (!string.IsNullOrEmpty(path))
        {
            ViewModel.OpenScene(path);
        }
    }

    private async void OnSaveScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        if (!string.IsNullOrEmpty(ViewModel.ScenePath))
        {
            ViewModel.SaveScene(ViewModel.ScenePath);
            return;
        }

        await SaveAsAsync();
    }

    private async void OnSaveSceneAs(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => await SaveAsAsync();

    private async Task SaveAsAsync()
    {
        if (ViewModel is null)
        {
            return;
        }

        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar escena",
            SuggestedFileName = $"{ViewModel.Scene.Name}.ollin.json",
            DefaultExtension = "json",
            FileTypeChoices = new[] { SceneFileType },
        });

        var path = file?.TryGetLocalPath();
        if (!string.IsNullOrEmpty(path))
        {
            ViewModel.SaveScene(path);
        }
    }

    private void OnExit(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => Close();

    // ---------- Edición ----------

    private void OnAddSprite(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AddEntity(true);

    private void OnAddEmpty(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AddEntity(false);

    private void OnDuplicate(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DuplicateSelected();

    private void OnDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DeleteSelected();

    private void OnClearConsole(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Logs.Clear();

    private void OnUndo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Undo();

    private void OnRedo(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Redo();

    // ---------- Assets ----------

    private async void OnImportAsset(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Importar textura",
            AllowMultiple = true,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("Imágenes")
                {
                    Patterns = new[] { "*.png", "*.jpg", "*.jpeg", "*.bmp" },
                },
            },
        });

        foreach (var file in files)
        {
            var path = file.TryGetLocalPath();
            if (!string.IsNullOrEmpty(path))
            {
                ViewModel.ImportTexture(path);
            }
        }
    }

    private void OnReloadAssets(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        ViewModel.RefreshAssets();
        ViewModel.RequestTextureReload();
    }

    private void OnAssignAsset(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewModel?.AssignAssetToSelected();

    private void OnAssetListKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key is Key.Enter or Key.Space)
        {
            ViewModel?.AssignAssetToSelected();
            e.Handled = true;
        }
    }

    // ---------- Scripts ----------

    private void OnReloadScripts(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ReloadScripts();

    private void OnCreateScript(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewModel?.CreateScriptForSelected();

    // ---------- Modo juego ----------

    private void OnPlay(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.TogglePlay();

    private void OnPause(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.TogglePause();

    private void OnStopPlay(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is { IsPlaying: true })
        {
            ViewModel.TogglePlay();
        }
    }

    // ---------- Ver ----------

    private void OnResetCamera(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ResetCamera();

    private void OnFrameSelected(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.FrameSelected();

    // ---------- Ayuda ----------

    private void OnAbout(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var about = new Window
        {
            Title = "Acerca de Ollin",
            Width = 460,
            Height = 400,
            CanResize = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Background = new SolidColorBrush(Color.Parse("#151922")),
        };

        var version = typeof(MainWindow).Assembly
            .GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion?
            .Split('+')[0] ?? "0.0.0";

        Bitmap? logo = null;
        try
        {
            logo = new Bitmap(AssetLoader.Open(new Uri("avares://Ollin.Editor/Assets/ollin.png")));
        }
        catch
        {
            // Sin logo el diálogo sigue funcionando.
        }

        var aboutPanel = new StackPanel
        {
            Margin = new Thickness(26),
            Spacing = 10,
            Children =
            {
                new TextBlock
                {
                    Text = "Ollin",
                    FontSize = 26,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock
                {
                    Text = $"Motor de juego 2D · versión {version} beta",
                    FontSize = 12,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new TextBlock
                {
                    Text = "Editor con Avalonia y renderizado 2D con OpenGL (Silk.NET). " +
                           "Animación, tilemaps, física y scripts en C# con recarga en caliente.",
                    FontSize = 12,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
                },
                new TextBlock
                {
                    Text = "«Ollin» significa movimiento en náhuatl.",
                    FontSize = 11,
                    Foreground = new SolidColorBrush(Color.Parse("#47B8A8")),
                },
            },
        };

        if (logo is not null)
        {
            aboutPanel.Children.Insert(0, new Image
            {
                Source = logo,
                Width = 72,
                Height = 72,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left,
            });
        }

        about.Content = aboutPanel;
        about.ShowDialog(this);
    }

    // ---------- Proyectos ----------

    private async void OnNewProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var dialog = new NewProjectWindow();
        var result = await dialog.ShowDialog<NewProjectResult?>(this);

        if (result is not null)
        {
            ViewModel.CreateProject(result.ParentFolder, result.Name, result.Template);
        }
    }

    private async void OnOpenProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new FolderPickerOpenOptions
        {
            Title = "Abrir proyecto de Ollin",
            AllowMultiple = false,
        });

        var path = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.OpenProjectLocation(path);
        }
    }

    private void OnOpenSampleProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        var root = ProjectLocator.FindProjectRoot() ?? AppContext.BaseDirectory;
        ViewModel?.OpenProjectLocation(root);
    }

    private void OnCloseProject(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.CloseProject();

    // ---------- Documentos ----------

    private void OnSelectDocument(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: ViewModels.DocumentTab tab } && ViewModel is not null)
        {
            ViewModel.ActiveDocument = tab;
        }
    }

    private async void OnCloseDocument(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is not Button { Tag: ViewModels.DocumentTab tab } || ViewModel is null)
        {
            return;
        }

        if (tab is { IsScene: true, SceneDocument.IsDirty: true })
        {
            var confirm = await new ConfirmWindow(
                "La escena tiene cambios sin guardar.",
                "Cerrar sin guardar",
                $"{tab.Title} se cerrará y los cambios se perderán.").ShowDialog<bool?>(this);

            if (confirm != true)
            {
                return;
            }

            ViewModel.CloseDocument(tab, force: true);
            return;
        }

        ViewModel.CloseDocument(tab);
    }

    private void OnSaveScript(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.SaveActiveScript();



    private void OnOpenSelectedScript(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewModel?.OpenSelectedScript();

    private void OnCopyEntity(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.CopySelected();

    private void OnPasteEntity(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.PasteCopied();

    private void OnClearSearch(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ClearSearch();

    private async void OnSavePrefab(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Selected is null)
        {
            ViewModel?.CopySelected();
            return;
        }

        var start = await StorageProvider.TryGetFolderFromPathAsync(ViewModel.PrefabsFolder);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar entidad como prefab",
            SuggestedFileName = $"{ViewModel.Selected.Name}{Ollin.Core.Prefabs.PrefabSerializer.Extension}",
            DefaultExtension = "json",
            SuggestedStartLocation = start,
        });

        var path = file?.TryGetLocalPath();
        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.SaveSelectedAsPrefab(Ollin.Core.Prefabs.PrefabSerializer.NormalizePath(path));
        }
    }

    private void OnRefreshFiles(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel?.RefreshProjectFiles();
        ViewModel?.RefreshAssets();
    }

    private void OnFileDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (FileTree.SelectedItem is ViewModels.FileNode node)
        {
            ViewModel?.OpenProjectFile(node);
        }
    }

    // ---------- Arrastrar y soltar / menús ----------

    private const string AssetDragFormat = "ollin/asset-path";

    private Point _fileDragStart;
    private bool _fileDragArmed;

    private void SetupDragAndDrop()
    {
        DragDrop.SetAllowDrop(SceneTree, true);
        SceneTree.AddHandler(DragDrop.DragOverEvent, OnEntityDragOver);
        SceneTree.AddHandler(DragDrop.DropEvent, OnEntityDrop);

        DragDrop.SetAllowDrop(ViewportHost, true);
        ViewportHost.AddHandler(DragDrop.DragOverEvent, OnViewportDragOver);
        ViewportHost.AddHandler(DragDrop.DropEvent, OnViewportDrop);

        FileTree.AddHandler(PointerPressedEvent, OnFileTreePointerPressed, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        FileTree.AddHandler(PointerMovedEvent, OnFileTreePointerMoved, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }


    private static ViewModels.EntityNode? EntityUnder(object? source)
    {
        var control = source as Control;
        while (control is not null && control is not TreeView)
        {
            if (control.DataContext is ViewModels.EntityNode node)
            {
                return node;
            }

            control = control.Parent as Control;
        }

        return null;
    }

    private static ViewModels.FileNode? FileUnder(object? source)
    {
        var control = source as Control;
        while (control is not null && control is not TreeView)
        {
            if (control.DataContext is ViewModels.FileNode node)
            {
                return node;
            }

            control = control.Parent as Control;
        }

        return null;
    }

    private static string? DraggedPath(DragEventArgs e) =>
        e.Data.Get(AssetDragFormat) as string ?? e.Data.GetText();

    private void OnEntityDragOver(object? sender, DragEventArgs e)
    {
        var path = DraggedPath(e);
        e.DragEffects = path is not null &&
                        (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                         path.EndsWith(Ollin.Core.Prefabs.PrefabSerializer.Extension, StringComparison.OrdinalIgnoreCase))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnEntityDrop(object? sender, DragEventArgs e)
    {
        var path = DraggedPath(e);
        if (path is null || ViewModel is null)
        {
            return;
        }

        var node = EntityUnder(e.Source);

        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            if (node is null)
            {
                ViewModel.StatusMessage = "Suelta el script sobre una entidad";
            }
            else
            {
                ViewModel.AttachScript(node.Entity, path);
            }

            e.Handled = true;
            return;
        }

        if (path.EndsWith(Ollin.Core.Prefabs.PrefabSerializer.Extension, StringComparison.OrdinalIgnoreCase))
        {
            var position = node?.Entity.Transform.Position ?? ViewModel.CameraPosition;
            ViewModel.InstantiatePrefabAt(path, position);
            e.Handled = true;
        }
    }

    private void OnViewportDragOver(object? sender, DragEventArgs e)
    {
        var path = DraggedPath(e);
        e.DragEffects = path is not null &&
                        (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase) ||
                         path.EndsWith(Ollin.Core.Prefabs.PrefabSerializer.Extension, StringComparison.OrdinalIgnoreCase))
            ? DragDropEffects.Copy
            : DragDropEffects.None;
        e.Handled = true;
    }

    private void OnViewportDrop(object? sender, DragEventArgs e)
    {
        var path = DraggedPath(e);
        if (path is null || ViewModel is null)
        {
            return;
        }

        var local = e.GetPosition(ViewportHost);
        var world = ViewportHost.Viewport.ToWorld(local);

        if (path.EndsWith(".cs", StringComparison.OrdinalIgnoreCase))
        {
            ViewportHost.Viewport.SelectAt(local);
            if (ViewModel.Selected is { } selected)
            {
                ViewModel.AttachScript(selected.Entity, path);
            }
            else
            {
                ViewModel.StatusMessage = "Suelta el script sobre una entidad del viewport";
            }

            e.Handled = true;
            return;
        }

        if (path.EndsWith(Ollin.Core.Prefabs.PrefabSerializer.Extension, StringComparison.OrdinalIgnoreCase))
        {
            ViewModel.InstantiatePrefabAt(path, world);
            e.Handled = true;
        }
    }

    private void OnFileTreePointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.GetCurrentPoint(FileTree).Properties.IsLeftButtonPressed)
        {
            _fileDragStart = e.GetPosition(FileTree);
            _fileDragArmed = true;
        }
    }

    private async void OnFileTreePointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_fileDragArmed || !e.GetCurrentPoint(FileTree).Properties.IsLeftButtonPressed)
        {
            return;
        }

        var position = e.GetPosition(FileTree);
        if (System.Math.Abs(position.X - _fileDragStart.X) < 6 && System.Math.Abs(position.Y - _fileDragStart.Y) < 6)
        {
            return;
        }

        _fileDragArmed = false;

        var node = FileUnder(e.Source) ?? FileTree.SelectedItem as FileNode;
        if (node is null || node.IsFolder)
        {
            return;
        }

        var data = new DataObject();
        data.Set(AssetDragFormat, node.FullPath);
        await DragDrop.DoDragDrop(e, data, DragDropEffects.Copy);
    }

    private ViewModels.EntityNode? MenuEntity(object? sender) =>
        (sender as MenuItem)?.DataContext as ViewModels.EntityNode ?? ViewModel?.Selected;

    private void OnHierarchyDoubleTapped(object? sender, Avalonia.Input.TappedEventArgs e)
    {
        if (SceneTree.SelectedItem is ViewModels.EntityNode node)
        {
            ViewModel?.FocusEntity(node.Entity);
        }
    }

    private void OnCreateChild(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (MenuEntity(sender) is { } node)
        {
            ViewModel?.CreateChild(node.Entity);
        }
    }

    private void OnFocusEntity(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (MenuEntity(sender) is { } node)
        {
            ViewModel?.FocusEntity(node.Entity);
        }
    }

    private void AddComponentToNode(object? sender, string kind)
    {
        if (MenuEntity(sender) is { } node && ViewModel is not null)
        {
            ViewModel.Select(node.Entity);
            ViewModel.AddComponentTo(kind);
        }
    }

    private void OnAddCollider(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "collider");

    private void OnAddTrigger(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "trigger");

    private void OnAddRigidbody(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "rigidbody");

    private void OnAddCameraComponent(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "camera");

    private void OnAddAnimator(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "animator");

    private void OnAddTilemap(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "tilemap");

    private void OnAddAudio(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "audio");

    private void OnAddRotator(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "rotator");

    private void OnAddOscillator(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "oscillator");

    private void OnAddScriptComponent(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "script");

    private void OnRemoveScript(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.RemoveScriptFromSelected();

    private void OnFileMenuOpen(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is ViewModels.FileNode node)
        {
            ViewModel?.OpenProjectFile(node);
        }
    }

    private async void OnFileMenuDelete(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if ((sender as MenuItem)?.DataContext is not ViewModels.FileNode node)
        {
            return;
        }

        var confirm = await new ConfirmWindow(
            $"¿Eliminar «{node.Name}»?",
            "Eliminar",
            "Esta acción no se puede deshacer.").ShowDialog<bool?>(this);

        if (confirm == true)
        {
            ViewModel?.DeleteFile(node);
        }
    }

    // ---------- Atajos ----------

    protected override void OnKeyDown(KeyEventArgs e)
    {
        var vm = ViewModel;
        if (vm is null)
        {
            base.OnKeyDown(e);
            return;
        }

        // Durante la reproducción el teclado pertenece al juego.
        if (vm.IsPlaying)
        {
            if (e.Key == Key.F5)
            {
                vm.TogglePlay();
                e.Handled = true;
                return;
            }

            if (e.Key == Key.F6)
            {
                vm.TogglePause();
                e.Handled = true;
                return;
            }

            if (KeyMapping.ToKeyCode(e.Key) is { } playKey)
            {
                InputState.SetKey(playKey, true);
                e.Handled = true;
            }

            base.OnKeyDown(e);
            return;
        }

        // Autocompletar con Ctrl+Espacio dentro del editor de scripts.
        if (e.Key == Key.Space && e.KeyModifiers.HasFlag(KeyModifiers.Control) && vm.IsScriptActive)
        {
            var entities = vm.Scene.Entities.Select(entity => entity.Name);
            ShowCompletion(CompletionCatalog.All(entities, vm.ScriptClassNames), WordStartOffset());
            e.Handled = true;
            return;
        }

        // Guardar script con Ctrl+S aunque el foco esté en el editor de texto.
        if (e.Key == Key.S && e.KeyModifiers.HasFlag(KeyModifiers.Control) && vm.IsScriptActive)
        {
            vm.SaveActiveScript();
            e.Handled = true;
            return;
        }

        if (IsTextInputFocused())
        {
            base.OnKeyDown(e);
            return;
        }

        var control = e.KeyModifiers.HasFlag(KeyModifiers.Control);
        switch (e.Key)
        {
            case Key.Z when control:
                vm.Undo();
                e.Handled = true;
                break;
            case Key.Y when control:
                vm.Redo();
                e.Handled = true;
                break;
            case Key.S when control:
                OnSaveScene(this, new Avalonia.Interactivity.RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.C when control:
                vm.CopySelected();
                e.Handled = true;
                break;
            case Key.V when control:
                vm.PasteCopied();
                e.Handled = true;
                break;
            case Key.Delete:
                vm.DeleteSelected();
                e.Handled = true;
                break;
            case Key.F:
                vm.FrameSelected();
                e.Handled = true;
                break;
            case Key.Home:
                vm.ResetCamera();
                e.Handled = true;
                break;
            case Key.Escape:
                vm.Selected = null;
                e.Handled = true;
                break;
            case Key.O when control:
                OnOpenScene(this, new Avalonia.Interactivity.RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.N when control:
                vm.NewScene();
                e.Handled = true;
                break;
            case Key.F5:
                vm.TogglePlay();
                e.Handled = true;
                break;
            case Key.F6:
                vm.TogglePause();
                e.Handled = true;
                break;
        }

        base.OnKeyDown(e);
    }
    protected override void OnKeyUp(KeyEventArgs e)
    {
        if (ViewModel is { IsPlaying: true } && KeyMapping.ToKeyCode(e.Key) is { } playKey)
        {
            InputState.SetKey(playKey, false);
            e.Handled = true;
        }

        base.OnKeyUp(e);
    }

    private bool IsTextInputFocused() =>
        FocusManager?.GetFocusedElement() is TextBox or NumericUpDown or AvaloniaEdit.TextEditor ||
        FocusManager?.GetFocusedElement() is Control { Name: "PART_TextBox" };
}
