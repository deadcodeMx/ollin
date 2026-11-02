using System.Collections.Specialized;
using System.Globalization;
using System.Reflection;
using System.Xml;
using AvaloniaEdit.Highlighting;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Highlighting.Xshd;
using Ollin.Scripting;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
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
    private bool _syncingHierarchy;
    private GameWindow? _gameWindow;
    private CompletionWindow? _completionWindow;

    public MainWindow()
    {
        InitializeComponent();
        ConsolidateInspector();
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

    private void OnHierarchySelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_syncingHierarchy || ViewModel is null) return;

        var nodes = SceneTree.SelectedItems?
            .OfType<EntityNode>()
            .ToList() ?? new List<EntityNode>();

        if (nodes.Count == 0)
        {
            ViewModel.ClearSelection();
            return;
        }

        ViewModel.SelectMany(nodes.Select(node => node.Entity));
    }

    private void SyncHierarchySelection()
    {
        if (ViewModel is null || SceneTree.SelectedItems is null) return;

        _syncingHierarchy = true;
        try
        {
            SceneTree.SelectedItems.Clear();

            foreach (var node in ViewModel.Selection)
            {
                SceneTree.SelectedItems.Add(node);
            }
        }
        finally
        {
            _syncingHierarchy = false;
        }
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
        SyncHierarchySelection();
            }
        };

        // El proyecto puede haberse abierto antes de crear la ventana (por ejemplo con --script).
        LoadActiveScriptText();

        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--play", StringComparison.OrdinalIgnoreCase)))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(vm.StartPlayback, Avalonia.Threading.DispatcherPriority.Background);
        }

        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--graph", StringComparison.OrdinalIgnoreCase)))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() =>
            {
                var entity = vm.Scene.Entities.FirstOrDefault(item => item.GetComponent<Ollin.Core.Model.Components.NodeGraph>() is not null);

                if (entity is not null)
                {
                    vm.Select(entity);
                    new NodeGraphWindow(vm, entity).Show(this);
                }
            }, Avalonia.Threading.DispatcherPriority.Background);
        }
        if (Environment.GetCommandLineArgs().Any(arg => arg.Equals("--game", StringComparison.OrdinalIgnoreCase)))
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(OpenGameWindow, Avalonia.Threading.DispatcherPriority.Background);
        }
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

    private void OnGizmoMove(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewportHost.Viewport.SetGizmo3DMode(Ollin.Rendering.Gizmo3DMode.Move);

    private void OnGizmoRotate(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewportHost.Viewport.SetGizmo3DMode(Ollin.Rendering.Gizmo3DMode.Rotate);

    private void OnGizmoScale(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewportHost.Viewport.SetGizmo3DMode(Ollin.Rendering.Gizmo3DMode.Scale);

    private void OnBrushWall(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewportHost.Viewport.Brush3DMode = ViewportHost.Viewport.Brush3DMode == Controls.Brush3DMode.Wall ? Controls.Brush3DMode.None : Controls.Brush3DMode.Wall;

    private void OnBrushFloor(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewportHost.Viewport.Brush3DMode = ViewportHost.Viewport.Brush3DMode == Controls.Brush3DMode.Floor ? Controls.Brush3DMode.None : Controls.Brush3DMode.Floor;

    private void OnTilePaletteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel is null || sender is not Button button || button.Tag is null) return;
        if (!int.TryParse(button.Tag.ToString(), out var index)) return;
        ViewModel.PaintTileIndex = index;
        ViewModel.TileFillMode = false;
        ViewModel.IsPaintingTiles = true;
    }

    private void OnTileEraser(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.SetTileEraser();

    private void OnTileClear(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ClearTilemap();

    private void OnBatchMove(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ApplyBatchMove();

    private void OnBatchRotate(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ApplyBatchRotation();

    private void OnBatchScale(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ApplyBatchScale();

    private void OnBatchSorting(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ApplyBatchSorting();

    private void OnAlignLeft(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AlignSelected("left");

    private void OnAlignCenterH(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AlignSelected("centerH");

    private void OnAlignRight(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AlignSelected("right");

    private void OnAlignTop(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AlignSelected("top");

    private void OnAlignCenterV(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AlignSelected("centerV");

    private void OnAlignBottom(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AlignSelected("bottom");

    private void OnDistributeH(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DistributeSelected(true);

    private void OnDistributeV(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DistributeSelected(false);

    private void OnZoomIn(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ZoomBy(1.2f);

    private void OnZoomOut(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ZoomBy(1f / 1.2f);

    private void OnZoomReset(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewModel?.SetZoom(1f);

    private void OnSendToBack(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => MoveSorting(-1, extreme: true);

    private void OnSendBackward(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => MoveSorting(-1, extreme: false);

    private void OnBringForward(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => MoveSorting(1, extreme: false);

    private void OnBringToFront(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => MoveSorting(1, extreme: true);

    private void MoveSorting(int direction, bool extreme)
    {
        if (ViewModel?.Selected is not { HasSprite: true } selected)
        {
            return;
        }

        var orders = ViewModel.Scene.Entities
            .Select(entity => entity.GetComponent<Ollin.Core.Model.Components.SpriteRenderer>()?.SortingOrder ?? 0)
            .ToList();

        var current = (int)selected.SortingOrder;
        var value = extreme
            ? (direction > 0 ? orders.DefaultIfEmpty(0).Max() + 1 : orders.DefaultIfEmpty(0).Min() - 1)
            : current + direction;

        selected.SortingOrder = value;
    }

    /// <summary>Convierte las pestañas del inspector en una sola vista con secciones.</summary>
    private void ConsolidateInspector()
    {
        var tabs = InspectorTabs.Items.OfType<TabItem>().ToList();
        var panel = new StackPanel { Spacing = 8, Margin = new Thickness(8, 4, 8, 8) };
        var entityTab = new TabItem { Header = "Entidad" };

        var occurrences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        foreach (var tab in tabs)
        {
            var headerText = tab.Header?.ToString() ?? string.Empty;
            occurrences.TryGetValue(headerText, out var occurrence);
            occurrences[headerText] = occurrence + 1;

            var content = tab.Content as Control;
            if (content is ScrollViewer scroll && scroll.Content is Control inner)
            {
                scroll.Content = null;
                content = inner;
            }

            tab.Content = null;

            var section = new StackPanel { Spacing = 4 };
            var visibilityProperty = VisibilityPropertyFor(headerText, occurrence);
            if (visibilityProperty is not null)
            {
                section.Bind(Visual.IsVisibleProperty, new Binding(visibilityProperty));
            }
            var header = new TextBlock
            {
                Text = tab.Header?.ToString()?.ToUpperInvariant() ?? string.Empty,
                FontSize = 10,
                FontWeight = Avalonia.Media.FontWeight.Bold,
                Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E9A23B")),
                Margin = new Thickness(0, 8, 0, 2),
            };

            section.Children.Add(header);
            if (content is not null) section.Children.Add(content);
            panel.Children.Add(section);
            panel.Children.Add(new Border
            {
                Height = 1,
                Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2B3342")),
                Margin = new Thickness(0, 6),
            });
        }

        entityTab.Content = new ScrollViewer { Content = panel };
        InspectorTabs.Items.Clear();
        InspectorTabs.Items.Add(entityTab);
        InspectorTabs.Items.Add(BuildTagsTab());
    }

    private static string? VisibilityPropertyFor(string header, int occurrence)
    {
        return header switch
        {
            "Entidad" or "Nivel" or "Mundo" => null,
            "Visual" => occurrence == 0 ? "Selected.HasSprite" : "Selected.HasVisualScript",
            "Audio" => "Selected.HasAudio",
            "Interacción" => "Selected.HasInteractable",
            "Inventario" => "Selected.HasInventory",
            "Estadísticas" => "Selected.HasStats",
            "Día/Noche" => "Selected.HasDayNight",
            "IA" => "Selected.HasAi",
            "Cámara 3D" => "Selected.HasCamera3D",
            "Pared 3D" => "Selected.HasWall3D",
            "FPS" => "Selected.HasFps",
            "IA 3D" => "Selected.HasAi3D",
            "Salud" => "Selected.HasHealth",
            "Arma" => "Selected.HasWeapon",
            "Recogible" => "Selected.HasPickup",
            "Puerta" => "Selected.HasDoor",
            "Reglas" => "Selected.HasRules",
            "Mensaje" => "Selected.HasIntro",
            "Reaparición" => "Selected.HasRespawn",
            "Botón 3D" => "Selected.HasButton",
            "Trigger" => "Selected.HasTrigger",
            "Generador" => "Selected.HasSpawner",
            "Checkpoint" => "Selected.HasCheckpoint",
            "Peligro" => "Selected.HasHazard",
            "Teletransporte" => "Selected.HasTeleporter",
            "Seguidor" => "Selected.HasFollower",
            "Tilemap" => "Selected.HasTilemap",
            "Luz" => "Selected.HasLight",
            "Cámara" => "Selected.HasCamera",
            _ => null,
        };
    }

    private TabItem BuildTagsTab()
    {
        var panel = new StackPanel { Margin = new Thickness(12), Spacing = 7 };
        panel.Children.Add(new TextBlock
        {
            Text = "TAGS DE LA ENTIDAD",
            FontSize = 10,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E9A23B")),
        });

        var tagsBox = new TextBox { Watermark = "player, enemy" };
        tagsBox.Bind(TextBox.TextProperty, new Binding("Selected.TagsText") { Mode = BindingMode.TwoWay });
        panel.Children.Add(tagsBox);
        panel.Children.Add(new TextBlock
        {
            Text = "Separa las etiquetas con comas.",
            FontSize = 10,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B94A7")),
        });

        panel.Children.Add(new Border
        {
            Height = 1,
            Background = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#2B3342")),
            Margin = new Thickness(0, 8),
        });
        panel.Children.Add(new TextBlock
        {
            Text = "CREAR TAG",
            FontSize = 10,
            FontWeight = Avalonia.Media.FontWeight.Bold,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E9A23B")),
        });

        var newTag = new TextBox { Watermark = "nuevo_tag" };
        var add = new Button { Content = "Añadir a la entidad", HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
        add.Click += (_, _) =>
        {
            if (ViewModel?.Selected is not { } selected) return;
            var tag = newTag.Text?.Trim();
            if (string.IsNullOrWhiteSpace(tag)) return;
            var tags = selected.Entity.Tags.ToList();
            if (!tags.Contains(tag, StringComparer.OrdinalIgnoreCase)) tags.Add(tag);
            selected.TagsText = string.Join(", ", tags);
            newTag.Text = string.Empty;
        };
        panel.Children.Add(newTag);
        panel.Children.Add(add);
        panel.Children.Add(new TextBlock
        {
            Text = "El tag se crea al asignarlo a una entidad.",
            FontSize = 10,
            Foreground = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#8B94A7")),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap,
        });

        return new TabItem { Header = "Tags", Content = new ScrollViewer { Content = panel } };
    }

    private void OnSnapToggle(object? sender, Avalonia.Interactivity.RoutedEventArgs e) =>
        ViewportHost.Viewport.Snap3D = (sender as Avalonia.Controls.Primitives.ToggleButton)?.IsChecked == true;

    private void OnSnap2DToggle(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewportHost?.Viewport is { } viewport)
        {
            viewport.Snap2D = (sender as Avalonia.Controls.Primitives.ToggleButton)?.IsChecked == true;
        }
    }

    private void OnSnap2DSizeChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (ViewportHost?.Viewport is not { } viewport || sender is not ComboBox combo ||
            combo.SelectedItem is not ComboBoxItem item)
        {
            return;
        }

        if (float.TryParse(item.Tag?.ToString(), NumberStyles.Float, CultureInfo.InvariantCulture, out var step))
        {
            viewport.Snap2DStep = Math.Clamp(step, 0.01f, 10f);
        }
    }

    private void OnDuplicateSeries(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.DuplicateSelectedSeries();

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

    private void OnOpenGameWindow(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => OpenGameWindow();

    private void OpenGameWindow()
    {
        if (ViewModel is null)
        {
            return;
        }

        if (_gameWindow is { IsVisible: true })
        {
            _gameWindow.Activate();
            return;
        }

        _gameWindow = new GameWindow(ViewModel);
        _gameWindow.Closed += (_, _) => _gameWindow = null;
        _gameWindow.Show();
    }

    private void OnResumeMenu(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.ResumeFromMenu();

    private void OnRestartScene(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.RestartPlay();

    private void OnStopFromMenu(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.StopFromMenu();

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

    private void OnAddLight(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "light");

    private void OnAddInventory(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "inventory");

    private void OnAddStats(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "stats");

    private void OnAddDayNight(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "daynight");

    private void OnAddAi(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "ai");

    private void OnAddCamera3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "camera3d");

    private void OnAddWall3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "wall3d");

    private void OnAddFps(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "fps");

    private void OnAddAi3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "ai3d");

    private void OnAddHealth(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "health");

    private void OnAddWeapon(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "weapon");

    private void OnAddWeaponSlot(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.AddWeaponToSelected();

    private void OnAddPickup(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "pickup");

    private void OnAddDoor(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "door");

    private void OnAddRules(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "rules");

    private void OnAddIntro(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "intro");

    private void OnAddRespawn(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "respawn");

    private void OnAddButton3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "button3d");

    private void OnAddTrigger3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "trigger3d");

    private void OnAddSpawner3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "spawner3d");

    private void OnAddCheckpoint3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "checkpoint3d");

    private void OnAddVisualScript(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "visual");

    private void OnAddHazard3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "hazard3d");

    private void OnAddTeleporter3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "teleporter3d");

    private void OnAddFollower3D(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "follower3d");

    private void OnAddNodeGraph(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => AddComponentToNode(sender, "nodegraph");

    private void OnOpenGraph(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Selected is not { } selected) return;
        var entity = selected.Entity;
        if (entity.GetComponent<Ollin.Core.Model.Components.NodeGraph>() is null)
        {
            entity.AddComponent(new Ollin.Core.Model.Components.NodeGraph());
            ViewModel.MarkDirty("grafo");
        }
        new NodeGraphWindow(ViewModel, entity).Show(this);
    }

    private void OnRemoveComponentFromMenu(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string kind } && MenuEntity(sender) is { } node && ViewModel is { } vm)
        {
            vm.Select(node.Entity);
            vm.RemoveComponent(kind);
        }
    }

    private async void OnSaveBlueprint(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Selected is null)
        {
            ViewModel?.CopySelected();
            return;
        }

        var start = await StorageProvider.TryGetFolderFromPathAsync(ViewModel.PrefabsFolder);
        var file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Guardar como blueprint",
            SuggestedFileName = $"{ViewModel.Selected.Name}{Ollin.Core.Prefabs.BlueprintSerializer.Extension}",
            DefaultExtension = "json",
            SuggestedStartLocation = start,
        });

        var path = file?.TryGetLocalPath();

        if (!string.IsNullOrWhiteSpace(path))
        {
            ViewModel.SaveSelectedAsBlueprint(path);
        }
    }

    private async void OnExportGame(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (ViewModel?.Project is null)
        {
            return;
        }

        var options = await new ExportOptionsWindow().ShowDialog<ExportOptions?>(this);

        if (options is null)
        {
            return;
        }

        var folders = await StorageProvider.OpenFolderPickerAsync(new Avalonia.Platform.Storage.FolderPickerOpenOptions
        {
            Title = "Carpeta donde exportar el juego",
            AllowMultiple = false,
        });

        var destination = folders.Count > 0 ? folders[0].TryGetLocalPath() : null;

        if (string.IsNullOrWhiteSpace(destination))
        {
            return;
        }

        var exported = ViewModel.ExportGame(destination, options.Splash, options.Watermark);

        if (exported is not null)
        {
            try
            {
                System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
                {
                    FileName = exported,
                    UseShellExecute = true,
                });
            }
            catch
            {
                // Si el explorador falla, la ruta queda en la barra de estado.
            }
        }
    }

    private void OnSaveGame(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.SaveGame();

    private void OnThemeDark(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.SetTheme(Ollin.Editor.Styles.EditorThemeMode.Dark);

    private void OnThemeLight(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.SetTheme(Ollin.Editor.Styles.EditorThemeMode.Light);

    private void OnThemeOllin(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.SetTheme(Ollin.Editor.Styles.EditorThemeMode.Ollin);

    private void OnLoadGame(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.LoadGame();

    private void OnAddItem(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Selected?.AddItemFromText();

    private void OnRemoveItem(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Selected?.RemoveItemFromText();

    private void OnClearInventory(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Selected?.ClearInventory();

    private void OnApplyStat(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Selected?.ApplyStatFromText();

    private void OnClearStats(object? sender, Avalonia.Interactivity.RoutedEventArgs e) => ViewModel?.Selected?.ClearStats();

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

        if (!vm.IsPlaying && vm.ActiveCamera3D is not null)
        {
            if (e.Key is Key.D1 or Key.NumPad1)
            {
                ViewportHost.Viewport.SetGizmo3DMode(Ollin.Rendering.Gizmo3DMode.Move);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.D2 or Key.NumPad2)
            {
                ViewportHost.Viewport.SetGizmo3DMode(Ollin.Rendering.Gizmo3DMode.Rotate);
                e.Handled = true;
                return;
            }

            if (e.Key is Key.D3 or Key.NumPad3)
            {
                ViewportHost.Viewport.SetGizmo3DMode(Ollin.Rendering.Gizmo3DMode.Scale);
                e.Handled = true;
                return;
            }
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
        if (e.Key == Key.Escape)
        {
            vm.TogglePauseMenu();
            e.Handled = true;
            return;
        }

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
