using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Ollin.Core.Saves;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Editor.ViewModels;
using Ollin.Editor.Controls;
using Ollin.Core.Scripting;

namespace Ollin.Editor.Views;

/// <summary>Editor de grafos de nodos por entidad.</summary>
public sealed class NodeGraphWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Entity _entity;
    private readonly NodeGraph _graph;
    private readonly Canvas _canvas = new();
    private readonly LayoutTransformControl _canvasTransform = new();
    private readonly NodeGraphHistory _history;
    private readonly GraphHistoryStore _historyStore = new();
    private readonly string _historyKey;
    private readonly List<GraphIssue> _issues = new();
    private readonly TextBlock _zoomText = new() { FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) };
    private readonly DispatcherTimer _debugTimer;
    private GraphSnapshot? _lastCommitted;
    private List<GraphNode> _clipboard = new();
    private readonly HashSet<Guid> _selectedIds = new();
    private readonly Dictionary<Guid, Border> _nodeCards = new();
    private readonly Dictionary<Guid, (double X, double Y)> _dragPositions = new();
    private Guid? _activeNodeId;
    private string _lastSignature = string.Empty;
    private double _zoom = 1.0;
    private ScrollViewer? _scroller;
    private TextBox? _searchBox;
    private string _searchText = string.Empty;
    private bool _panning;
    private Point _panStart;
    private Vector _panOffset;
    private readonly StackPanel _inspector = new() { Spacing = 6, Margin = new Thickness(10) };
    private readonly TextBlock _status = new() { FontSize = 11, Foreground = new SolidColorBrush(Color.Parse("#AAB4C4")), Margin = new Thickness(8) };
    private GraphNode? _selected;
    private GraphNode? _linkSource;
    private bool _linkTrue = true;
    private Border? _dragCard;
    private GraphNode? _dragNode;
    private Point _dragStart;
    private double _dragX;
    private double _dragY;
    private Border? _marquee;
    private Point _marqueeStart;
    private bool _marqueeSelecting;
    private bool _marqueeAdd;
    private HashSet<Guid> _marqueeBase = new();
    private readonly List<Line> _guideLines = new();
    private bool _snapNodes = true;
    private double _snapSize = 10d;

    public NodeGraphWindow(MainViewModel viewModel, Entity entity)
    {
        _viewModel = viewModel;
        _entity = entity;
        _graph = entity.GetComponent<NodeGraph>()!;
        _history = _graph.History;
        _historyKey = $"{_viewModel.ScenePath ?? "sin-guardar"}|{entity.Id}";
        _historyStore.Load(_historyKey, entity.Id, _history, _graph.Breakpoints);
        Title = $"Grafo de nodos — {entity.Name}";
        Width = 1220;
        Height = 760;
        MinWidth = 900;
        MinHeight = 560;
        Background = new SolidColorBrush(Color.Parse("#0B0D12"));

        var root = new Grid
        {
            ColumnDefinitions = new ColumnDefinitions("220,*,280"),
            RowDefinitions = new RowDefinitions("*,Auto"),
        };

        var palette = BuildPalette();
        var canvasArea = BuildCanvasArea();
        var inspectorArea = BuildInspectorArea();
        Grid.SetColumn(palette, 0);
        Grid.SetColumn(canvasArea, 1);
        Grid.SetColumn(inspectorArea, 2);
        root.Children.Add(palette);
        root.Children.Add(canvasArea);
        root.Children.Add(inspectorArea);
        Grid.SetColumn(_status, 1);
        Grid.SetRow(_status, 1);
        root.Children.Add(_status);

        Content = root;
        Render();
        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id, _selectedIds);
        _debugTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _debugTimer.Tick += (_, _) => PollDebug();
        _debugTimer.Start();
        GraphDebugger.BreakpointHit += OnBreakpointHit;
        Closed += (_, _) =>
        {
            _debugTimer.Stop();
            _historyStore.Save(_historyKey, entity.Id, _history, _graph.Breakpoints);
            GraphDebugger.BreakpointHit -= OnBreakpointHit;
            WindowsMouseLock.Suspended = false;
        };
        Activated += (_, _) =>
        {
            WindowsMouseLock.Suspended = true;
            WindowsMouseLock.Disable();
        };
        Deactivated += (_, _) => WindowsMouseLock.Suspended = false;
        AddHandler(KeyDownEvent, OnGraphKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
    }

    private Control BuildPalette()
    {
        var panel = new StackPanel { Spacing = 5, Margin = new Thickness(10) };
        panel.Children.Add(new TextBlock
        {
            Text = "PALETA",
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
            Margin = new Thickness(0, 4, 0, 4),
        });

        AddPalette(panel, "Evento: iniciar", () => AddNode(GraphNodeKind.Event, GraphEventKind.OnStart));
        AddPalette(panel, "Evento: cada tiempo", () => AddNode(GraphNodeKind.Event, GraphEventKind.OnTimer));
        AddPalette(panel, "Evento: entrar", () => AddNode(GraphNodeKind.Event, GraphEventKind.OnPlayerEnter));
        AddPalette(panel, "Evento: pulsar E", () => AddNode(GraphNodeKind.Event, GraphEventKind.OnInteract));
        AddPalette(panel, "Evento: morir", () => AddNode(GraphNodeKind.Event, GraphEventKind.OnPlayerDeath));
        AddPalette(panel, "Condición: tiene objeto", () => AddCondition(GraphConditionKind.HasItem));
        AddPalette(panel, "Condición: vida baja", () => AddCondition(GraphConditionKind.HealthBelow));
        AddPalette(panel, "Acción: mensaje", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Message));
        AddPalette(panel, "Acción: abrir puerta", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.OpenDoor));
        AddPalette(panel, "Acción: activar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Activate));
        AddPalette(panel, "Acción: desactivar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Deactivate));
        AddPalette(panel, "Acción: generar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Spawn));
        AddPalette(panel, "Acción: checkpoint", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Checkpoint));
        AddPalette(panel, "Acción: ganar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Win));
        AddPalette(panel, "Acción: dañar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Damage));
        AddPalette(panel, "Acción: curar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Heal));
        AddPalette(panel, "Acción: teletransportar", () => AddNode(GraphNodeKind.Action, GraphEventKind.OnStart, ActionKind.Teleport));
        AddPalette(panel, "Comentario", AddComment);
        AddPalette(panel, "Grupo", AddGroup);

        return new ScrollViewer { Content = panel };
    }

    private void AddPalette(StackPanel panel, string label, Action action)
    {
        var button = new Button
        {
            Content = label,
            FontSize = 11,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left,
        };
        button.Click += (_, _) => action();
        panel.Children.Add(button);
    }

    private Control BuildCanvasArea()
    {
        _canvas.Width = 2400;
        _canvas.Height = 1600;
        _canvas.Background = new SolidColorBrush(Color.Parse("#10131A"));
        _canvas.PointerPressed += (_, e) => BeginMarquee(e);
        _canvas.PointerMoved += (_, e) => UpdateMarquee(e);
        _canvas.PointerReleased += (_, e) => EndMarquee(e);
        _canvasTransform.Child = _canvas;
        _canvasTransform.LayoutTransform = new ScaleTransform(_zoom, _zoom);

        var toolbar = new WrapPanel
        {
            Orientation = Orientation.Horizontal,
            Margin = new Thickness(4, 2),
        };
        toolbar.Children.Add(ToolButton("Deshacer", Undo));
        toolbar.Children.Add(ToolButton("Rehacer", Redo));
        toolbar.Children.Add(ToolButton("Copiar", CopySelected));
        toolbar.Children.Add(ToolButton("Pegar", Paste));
        toolbar.Children.Add(ToolButton("Eliminar", DeleteSelected));
        toolbar.Children.Add(ToolButton("−", () => ZoomBy(-0.1)));
        toolbar.Children.Add(ToolButton("100%", () => SetZoom(1.0)));
        toolbar.Children.Add(ToolButton("+", () => ZoomBy(0.1)));
        toolbar.Children.Add(ToolButton("Ajustar", () => SetZoom(0.75)));
        toolbar.Children.Add(ToolButton("Ordenar", AutoLayout));
        toolbar.Children.Add(ToolButton("Paso", StepGraph));
        toolbar.Children.Add(ToolButton("Distribuir H", () => Distribute(true)));
        toolbar.Children.Add(ToolButton("Distribuir V", () => Distribute(false)));
        var snapCheck = new CheckBox { Content = "Rejilla", IsChecked = true, FontSize = 10.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(8, 0, 0, 0) };
        snapCheck.IsCheckedChanged += (_, _) => _snapNodes = snapCheck.IsChecked == true;
        toolbar.Children.Add(snapCheck);
        var snapSize = new NumericUpDown { Value = 10, Minimum = 2, Maximum = 50, Increment = 1, Width = 70, FontSize = 10.5, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 0, 0) };
        snapSize.ValueChanged += (_, _) => { if (snapSize.Value is { } value) _snapSize = (double)value; };
        toolbar.Children.Add(snapSize);
        _searchBox = new TextBox { Watermark = "Buscar nodo…", Width = 140, FontSize = 10.5, Margin = new Thickness(8, 0, 0, 0) };
        _searchBox.TextChanged += (_, _) =>
        {
            _searchText = _searchBox.Text ?? string.Empty;
            Render();
        };
        toolbar.Children.Add(_searchBox);
        toolbar.Children.Add(_zoomText);
        UpdateZoomText();

        var scroller = new ScrollViewer
        {
            Content = _canvasTransform,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
        _scroller = scroller;
        scroller.PointerWheelChanged += (_, e) =>
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ZoomAt(_zoom + (e.Delta.Y > 0 ? 0.1 : -0.1), e.GetPosition(scroller));
                e.Handled = true;
            }
        };
        scroller.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(scroller).Properties.IsMiddleButtonPressed)
            {
                _panning = true;
                _panStart = e.GetPosition(scroller);
                _panOffset = scroller.Offset;
                e.Pointer.Capture(scroller);
                e.Handled = true;
            }
        };
        scroller.PointerMoved += (_, e) =>
        {
            if (!_panning || e.Pointer.Captured != scroller) return;
            var delta = e.GetPosition(scroller) - _panStart;
            scroller.Offset = new Vector(
                Math.Max(0, _panOffset.X - delta.X),
                Math.Max(0, _panOffset.Y - delta.Y));
            e.Handled = true;
        };
        scroller.PointerReleased += (_, e) =>
        {
            if (_panning)
            {
                _panning = false;
                e.Pointer.Capture(null);
                e.Handled = true;
            }
        };

        var dock = new DockPanel { LastChildFill = true };
        DockPanel.SetDock(toolbar, Dock.Top);
        dock.Children.Add(toolbar);
        dock.Children.Add(scroller);
        return dock;
    }

    private static Button ToolButton(string text, Action action)
    {
        var button = new Button { Content = text, FontSize = 10.5, Padding = new Thickness(7, 3) };
        button.Click += (_, _) => action();
        return button;
    }

    private void ZoomBy(double delta) => SetZoom(_zoom + delta);

    private void SetZoom(double zoom)
    {
        var anchor = _scroller is null
            ? new Point(0, 0)
            : new Point(_scroller.Bounds.Width / 2, _scroller.Bounds.Height / 2);
        ZoomAt(zoom, anchor);
    }

    private void ZoomAt(double zoom, Point anchor)
    {
        var next = Math.Clamp(zoom, 0.35, 2.0);

        if (Math.Abs(next - _zoom) < 0.0001)
        {
            return;
        }

        if (_scroller is null)
        {
            _zoom = next;
            _canvasTransform.LayoutTransform = new ScaleTransform(_zoom, _zoom);
            UpdateZoomText();
            return;
        }

        var old = _zoom;
        var logical = new Point(
            (_scroller.Offset.X + anchor.X) / old,
            (_scroller.Offset.Y + anchor.Y) / old);
        _zoom = next;
        _canvasTransform.LayoutTransform = new ScaleTransform(_zoom, _zoom);

        Dispatcher.UIThread.Post(() =>
        {
            _scroller.Offset = new Vector(
                Math.Max(0, logical.X * _zoom - anchor.X),
                Math.Max(0, logical.Y * _zoom - anchor.Y));
            UpdateZoomText();
        }, DispatcherPriority.Background);
    }

    private void UpdateZoomText() => _zoomText.Text = $"{_zoom * 100:0}%";

    private void StepGraph()
    {
        if (_viewModel.StepGraph(_graph))
        {
            _activeNodeId = _graph.LastNodeId;
            Render();
        }
    }

    private void Distribute(bool horizontal)
    {
        var selected = _graph.Nodes
            .Where(node => _selectedIds.Contains(node.Id))
            .OrderBy(node => horizontal ? node.X : node.Y)
            .ToList();

        if (selected.Count < 3)
        {
            return;
        }

        var first = horizontal ? selected[0].X : selected[0].Y;
        var last = horizontal ? selected[^1].X : selected[^1].Y;
        var step = (last - first) / (selected.Count - 1);

        for (var i = 1; i < selected.Count - 1; i++)
        {
            if (horizontal) selected[i].X = first + step * i;
            else selected[i].Y = first + step * i;
        }

        MarkChanged();
        Render();
    }

    private void AutoLayout()
    {
        var events = _graph.Nodes.Where(node => node.Kind == GraphNodeKind.Event).ToList();
        var placed = new HashSet<Guid>();
        var column = 0;
        var row = 0;

        foreach (var start in events)
        {
            var queue = new Queue<(GraphNode Node, int Depth)>();
            queue.Enqueue((start, 0));

            while (queue.Count > 0)
            {
                var (node, depth) = queue.Dequeue();

                if (!placed.Add(node.Id))
                {
                    continue;
                }

                node.X = 40 + depth * 260;
                node.Y = 40 + row * 140;
                row++;

                foreach (var id in new[] { node.Next, node.TrueNext, node.FalseNext }.Where(id => id is not null))
                {
                    if (_graph.Find(id) is { } next && !placed.Contains(next.Id))
                    {
                        queue.Enqueue((next, depth + 1));
                    }
                }
            }

            column++;
        }

        foreach (var node in _graph.Nodes.Where(node => !placed.Contains(node.Id)))
        {
            node.X = 40 + column * 260;
            node.Y = 40 + row * 140;
            row++;
        }

        MarkChanged();
        Render();
    }

    private Control BuildInspectorArea()
    {
        return new Border
        {
            Background = new SolidColorBrush(Color.Parse("#151922")),
            BorderBrush = new SolidColorBrush(Color.Parse("#252B36")),
            BorderThickness = new Thickness(1, 0, 0, 0),
            Child = new ScrollViewer { Content = _inspector },
        };
    }

    private void AddNode(GraphNodeKind kind, GraphEventKind eventKind, ActionKind action = ActionKind.Message)
    {
        var node = _graph.AddNode(kind, 70 + _graph.Nodes.Count * 24, 70 + _graph.Nodes.Count * 22);
        node.Event = eventKind;
        node.Action = action;
        _selected = node;
        MarkChanged();
        Render();
    }

    private void AddCondition(GraphConditionKind condition)
    {
        var node = _graph.AddNode(GraphNodeKind.Condition, 70 + _graph.Nodes.Count * 24, 70 + _graph.Nodes.Count * 22);
        node.Condition = condition;
        _selected = node;
        MarkChanged();
        Render();
    }

    private void AddComment()
    {
        var node = _graph.AddNode(GraphNodeKind.Comment, 70 + _graph.Nodes.Count * 20, 70 + _graph.Nodes.Count * 20);
        node.Message = "Comentario";
        _selected = node;
        _selectedIds.Clear();
        _selectedIds.Add(node.Id);
        MarkChanged();
        Render();
    }

    private void AddGroup()
    {
        var node = _graph.AddNode(GraphNodeKind.Group, 60 + _graph.Nodes.Count * 16, 60 + _graph.Nodes.Count * 16);
        node.Message = "Grupo";
        _selected = node;
        _selectedIds.Clear();
        _selectedIds.Add(node.Id);
        MarkChanged();
        Render();
    }

    private void MarkChanged()
    {
        if (_lastCommitted is not null)
        {
            _history.Record(_lastCommitted);
        }

        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id, _selectedIds);
        _viewModel.MarkDirty("grafo-nodos");
    }
    private void Undo()
    {
        if (!_history.Undo(_graph, _selected?.Id, out var selectedId)) return;
        _selected = selectedId is { } restoredId ? _graph.Find(restoredId) : null;
        _selectedIds.Clear();
        foreach (var selectionId in _history.LastRestored?.SelectedIds ?? new List<Guid>()) _selectedIds.Add(selectionId);
        if (_selected is not null) _selectedIds.Add(_selected.Id);
        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id, _selectedIds);
        _viewModel.MarkDirty("grafo-nodos");
        Render();
    }

    private void Redo()
    {
        if (!_history.Redo(_graph, _selected?.Id, out var selectedId)) return;
        _selected = selectedId is { } restoredId ? _graph.Find(restoredId) : null;
        _selectedIds.Clear();
        foreach (var selectionId in _history.LastRestored?.SelectedIds ?? new List<Guid>()) _selectedIds.Add(selectionId);
        if (_selected is not null) _selectedIds.Add(_selected.Id);
        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id, _selectedIds);
        _viewModel.MarkDirty("grafo-nodos");
        Render();
    }
    private void CopySelected()
    {
        var nodes = _graph.Nodes.Where(node => _selectedIds.Contains(node.Id)).ToList();

        if (nodes.Count == 0 && _selected is not null)
        {
            nodes.Add(_selected);
        }

        if (nodes.Count == 0) return;
        _clipboard = nodes.Select(node => GraphEditing.CloneNode(node, keepLinks: true)).ToList();
        _status.Text = nodes.Count == 1 ? "Nodo copiado" : $"{nodes.Count} nodos copiados";
    }

    private void Paste()
    {
        if (_clipboard.Count == 0) return;
        var map = new Dictionary<Guid, Guid>();
        var copies = _clipboard.Select(node =>
        {
            var copy = GraphEditing.CloneNode(node, keepLinks: true);
            copy.Id = Guid.NewGuid();
            copy.X += 30;
            copy.Y += 30;
            map[node.Id] = copy.Id;
            return copy;
        }).ToList();

        foreach (var copy in copies)
        {
            copy.Next = copy.Next is { } next ? (map.TryGetValue(next, out var mappedNext) ? mappedNext : (_graph.Find(next) is not null ? next : null)) : null;
            copy.TrueNext = copy.TrueNext is { } yes ? (map.TryGetValue(yes, out var mappedYes) ? mappedYes : (_graph.Find(yes) is not null ? yes : null)) : null;
            copy.FalseNext = copy.FalseNext is { } no ? (map.TryGetValue(no, out var mappedNo) ? mappedNo : (_graph.Find(no) is not null ? no : null)) : null;
            _graph.Nodes.Add(copy);
        }

        _selectedIds.Clear();
        foreach (var copy in copies) _selectedIds.Add(copy.Id);
        _selected = copies.FirstOrDefault();
        MarkChanged();
        Render();
    }

    private void DeleteSelected()
    {
        var ids = _selectedIds.Count > 0
            ? _selectedIds.ToList()
            : _selected is null ? new List<Guid>() : new List<Guid> { _selected.Id };

        if (ids.Count == 0) return;
        foreach (var id in ids) _graph.RemoveNode(id);
        _selectedIds.Clear();
        _selected = null;
        _linkSource = null;
        MarkChanged();
        Render();
    }

    private void OnGraphKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Source is TextBox) return;
        var control = e.KeyModifiers.HasFlag(KeyModifiers.Control);

        if (control && e.Key == Key.Z) { Undo(); e.Handled = true; }
        else if (control && e.Key == Key.Y) { Redo(); e.Handled = true; }
        else if (control && e.Key == Key.A)
        {
            _selectedIds.Clear();
            foreach (var node in _graph.Nodes) _selectedIds.Add(node.Id);
            _selected = _graph.Nodes.FirstOrDefault();
            Render();
            e.Handled = true;
        }
        else if (control && e.Key == Key.C) { CopySelected(); e.Handled = true; }
        else if (control && e.Key == Key.V) { Paste(); e.Handled = true; }
        else if (e.Key == Key.Delete) { DeleteSelected(); e.Handled = true; }
    }

    private void PollDebug()
    {
        if (_dragNode is null && _lastSignature.Length > 0 && _lastSignature != GraphSignature())
        {
            _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id, _selectedIds);
            if (_selected is not null && _graph.Find(_selected.Id) is null) _selected = null;
            Render();
            return;
        }

        if (!_viewModel.IsPlaying)
        {
            if (_activeNodeId is not null)
            {
                _activeNodeId = null;
                Render();
            }
            return;
        }

        var id = _graph.LastNodeId;

        if (id != _activeNodeId)
        {
            _activeNodeId = id;
            Render();
        }
    }

    private void Validate()
    {
        _issues.Clear();
        _issues.AddRange(GraphValidator.Validate(_graph, _viewModel.Scene));
    }
    private void AddValidationPanel()
    {
        _inspector.Children.Add(new TextBlock
        {
            Text = "VALIDACIÓN",
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
            Margin = new Thickness(0, 12, 0, 0),
        });

        if (_issues.Count == 0)
        {
            _inspector.Children.Add(new TextBlock
            {
                Text = "Sin problemas detectados.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.Parse("#47B8A8")),
                TextWrapping = TextWrapping.Wrap,
            });
        }
        else
        {
            foreach (var issue in _issues.Take(8))
            {
                _inspector.Children.Add(new TextBlock
                {
                    Text = (issue.IsError ? "• " : "⚠ ") + issue.Message,
                    FontSize = 10.5,
                    Foreground = new SolidColorBrush(Color.Parse(issue.IsError ? "#E05B5B" : "#E9C46A")),
                    TextWrapping = TextWrapping.Wrap,
                });
            }
        }

        if (_viewModel.IsPlaying)
        {
            _inspector.Children.Add(new TextBlock
            {
                Text = "DEPURACIÓN",
                FontSize = 10,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#53D6FF")),
                Margin = new Thickness(0, 10, 0, 0),
            });

            _inspector.Children.Add(new TextBlock
            {
                Text = _graph.LastNodeId is { } active && _graph.Find(active) is { } activeNode
                    ? $"Activo: {KindLabel(activeNode)} · {NodeSummary(activeNode)}"
                    : "Sin ejecución todavía.",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.Parse("#D6DCE8")),
                TextWrapping = TextWrapping.Wrap,
            });

            if (_graph.ExecutionHistory.Count > 0)
            {
                _inspector.Children.Add(new TextBlock
                {
                    Text = "Últimos nodos",
                    FontSize = 10,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                    Margin = new Thickness(0, 6, 0, 0),
                });

                foreach (var historyId in _graph.ExecutionHistory.TakeLast(5).Reverse())
                {
                    if (_graph.Find(historyId) is not { } historyNode) continue;

                    _inspector.Children.Add(new TextBlock
                    {
                        Text = $"  {KindLabel(historyNode)} · {NodeSummary(historyNode)}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#D6DCE8")),
                        TextWrapping = TextWrapping.Wrap,
                    });
                }
            }

            if (GameStateHost.Current is { } state)
            {
                foreach (var (key, value) in state.Values.Take(8))
                {
                    _inspector.Children.Add(new TextBlock
                    {
                        Text = $"  {key} = {value}",
                        FontSize = 10,
                        Foreground = new SolidColorBrush(Color.Parse("#AAB4C4")),
                    });
                }
            }
        }
    }


    private string GraphSignature()
    {
        var builder = new System.Text.StringBuilder();

        foreach (var node in _graph.Nodes.OrderBy(node => node.Id))
        {
            builder.Append(node.Id).Append('|')
                .Append((int)node.Kind).Append('|')
                .Append((int)node.Event).Append('|')
                .Append((int)node.Condition).Append('|')
                .Append((int)node.Action).Append('|')
                .Append(node.X).Append('|').Append(node.Y).Append('|')
                .Append(node.TargetName).Append('|').Append(node.Message).Append('|')
                .Append(node.ItemId).Append('|').Append(node.VariableName).Append('|')
                .Append(node.Expression).Append('|').Append(node.GroupWidth).Append('|')
                .Append(node.GroupHeight).Append('|').Append(node.Next).Append('|')
                .Append(node.TrueNext).Append('|').Append(node.FalseNext).Append(';');
        }

        return builder.ToString();
    }

    private void OnBreakpointHit(NodeGraph graph, Guid nodeId)
    {
        if (!ReferenceEquals(graph, _graph))
        {
            return;
        }

        Dispatcher.UIThread.Post(() =>
        {
            _activeNodeId = nodeId;

            if (_viewModel.IsPlaying && !_viewModel.IsPaused)
            {
                _viewModel.TogglePause();
            }

            Render();
        }, DispatcherPriority.Background);
    }

    private void ToggleBreakpoint(GraphNode node)
    {
        if (!_graph.Breakpoints.Remove(node.Id))
        {
            _graph.Breakpoints.Add(node.Id);
        }

        Render();
    }

    private bool IsHidden(GraphNode node)
    {
        return _graph.Nodes.Any(group =>
            group.Kind == GraphNodeKind.Group && group.Collapsed && group.Id != node.Id &&
            node.X >= group.X && node.X <= group.X + group.GroupWidth &&
            node.Y >= group.Y && node.Y <= group.Y + group.GroupHeight);
    }

    private void Render()
    {
        Validate();
        _canvas.Children.Clear();
        _nodeCards.Clear();

        foreach (var node in _graph.Nodes.Where(node => !IsHidden(node)))
        {
            if (node.Next is { } next)
            {
                DrawLink(node, next, "next");
            }

            if (node.Kind == GraphNodeKind.Condition)
            {
                if (node.TrueNext is { } yes) DrawLink(node, yes, "true");
                if (node.FalseNext is { } no) DrawLink(node, no, "false");
            }
        }

        foreach (var node in _graph.Nodes.Where(node => node.Kind == GraphNodeKind.Group && !IsHidden(node)))
        {
            _canvas.Children.Add(BuildNodeCard(node));
        }

        foreach (var node in _graph.Nodes.Where(node => node.Kind != GraphNodeKind.Group && !IsHidden(node)))
        {
            _canvas.Children.Add(BuildNodeCard(node));
        }

        RenderInspector();
        var errors = _issues.Count(issue => issue.IsError);
        var warnings = _issues.Count - errors;
        _status.Text = _linkSource is null
            ? $"Selecciona un nodo. Usa Conectar y el encabezado destino. Validación: {errors} errores, {warnings} avisos."
            : "Selecciona el nodo destino para conectar la salida.";
        Title = $"Grafo de nodos — {_entity.Name} ({_graph.Nodes.Count} nodos)";
        _lastSignature = GraphSignature();
    }

    private void DrawLink(GraphNode source, Guid targetId, string port)
    {
        var target = _graph.Find(targetId);

        if (target is null)
        {
            return;
        }

        var yOffset = port switch { "true" => 92, "false" => 116, _ => 52 };
        var color = port switch
        {
            "true" => Color.Parse("#47B8A8"),
            "false" => Color.Parse("#E05B5B"),
            _ => Color.Parse("#E9A23B"),
        };

        var line = new Line
        {
            StartPoint = new Point(source.X + 200, source.Y + yOffset),
            EndPoint = new Point(target.X, target.Y + 24),
            Stroke = new SolidColorBrush(color),
            StrokeThickness = 3,
            Tag = new LinkRef(source.Id, port),
        };
        line.ContextMenu = ConnectionMenu(source.Id, port);
        _canvas.Children.Add(line);
    }

    private Control BuildNodeCard(GraphNode node)
    {
        if (node.Kind == GraphNodeKind.Group)
        {
            var group = new Border
            {
                Width = node.GroupWidth,
                Height = node.Collapsed ? 40 : node.GroupHeight,
                Background = new SolidColorBrush(Color.Parse("#30151B24")),
                BorderBrush = new SolidColorBrush(Color.Parse(_selectedIds.Contains(node.Id) ? "#FFFFFF" : "#8B94A7")),
                BorderThickness = new Thickness(_selectedIds.Contains(node.Id) ? 2 : 1),
                CornerRadius = new CornerRadius(8),
            };
            Canvas.SetLeft(group, node.X);
            Canvas.SetTop(group, node.Y);
            var groupHeader = new TextBlock
            {
                Text = (node.Collapsed ? "▸ GRUPO · " : "▾ GRUPO · ") + node.Message,
                FontSize = 11,
                FontWeight = FontWeight.Bold,
                Foreground = new SolidColorBrush(Color.Parse("#AAB4C4")),
                Margin = new Thickness(10, 6),
            };
            group.Child = groupHeader;
            group.PointerPressed += (_, e) => OnNodePressed(node, group, e);
            group.PointerMoved += (_, e) => OnNodeMoved(node, group, e);
            group.PointerReleased += (_, e) => OnNodeReleased(node, group, e);
            _nodeCards[node.Id] = group;
            return group;
        }

        var kindColor = node.Kind switch
        {
            GraphNodeKind.Event => "#E9A23B",
            GraphNodeKind.Condition => "#5B9CE0",
            GraphNodeKind.Comment => "#8B94A7",
            _ => "#47B8A8",
        };

        var issue = _issues.FirstOrDefault(item => item.Node?.Id == node.Id);
        var active = _viewModel.IsPlaying && _activeNodeId == node.Id;
        var selected = _selectedIds.Contains(node.Id);
        var breakpoint = _graph.Breakpoints.Contains(node.Id);
        var match = !string.IsNullOrWhiteSpace(_searchText) &&
                    $"{KindLabel(node)} {NodeSummary(node)}".Contains(_searchText, StringComparison.OrdinalIgnoreCase);
        var borderColor = active ? "#53D6FF" : breakpoint ? "#E05BE0" : issue is { IsError: true } ? "#E05B5B" : issue is not null ? "#E9C46A" : match ? "#E9A23B" : selected ? "#FFFFFF" : kindColor;
        var card = new Border
        {
            Width = 200,
            Background = new SolidColorBrush(Color.Parse("#1A1F29")),
            BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
            BorderThickness = new Thickness(active ? 3 : selected ? 2 : 1),
            CornerRadius = new CornerRadius(6),
        };
        Canvas.SetLeft(card, node.X);
        Canvas.SetTop(card, node.Y);

        var content = new StackPanel { Spacing = 4 };
        var header = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#222833")),
            Padding = new Thickness(8, 5),
            CornerRadius = new CornerRadius(5, 5, 0, 0),
        };
        var headerText = new TextBlock
        {
            Text = breakpoint ? KindLabel(node) + "  ●" : KindLabel(node),
            FontSize = 11,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse(kindColor)),
        };
        header.Child = headerText;
        header.PointerPressed += (_, e) => OnNodePressed(node, card, e);
        header.PointerMoved += (_, e) => OnNodeMoved(node, card, e);
        header.PointerReleased += (_, e) => OnNodeReleased(node, card, e);
        content.Children.Add(header);

        content.Children.Add(new TextBlock
        {
            Text = NodeSummary(node),
            FontSize = 10.5,
            Foreground = new SolidColorBrush(Color.Parse("#D6DCE8")),
            TextWrapping = TextWrapping.Wrap,
            Margin = new Thickness(8, 2, 8, 0),
        });

        var ports = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            Margin = new Thickness(8, 4, 8, 8),
        };

        if (node.Kind == GraphNodeKind.Condition)
        {
            ports.Children.Add(PortButton("Sí →", node, true));
            ports.Children.Add(PortButton("No →", node, false));
        }
        else if (node.Kind != GraphNodeKind.Comment)
        {
            ports.Children.Add(PortButton("Conectar →", node, true));
        }

        content.Children.Add(ports);
        card.Child = content;
        _nodeCards[node.Id] = card;
        return card;
    }

    private Button PortButton(string text, GraphNode node, bool trueBranch)
    {
        var button = new Button
        {
            Content = text,
            FontSize = 10,
            Padding = new Thickness(6, 2),
        };
        button.Click += (_, _) =>
        {
            _linkSource = node;
            _linkTrue = trueBranch;
            _selected = node;
            _selectedIds.Clear();
            _selectedIds.Add(node.Id);
            Render();
        };
        var portName = node.Kind == GraphNodeKind.Condition ? (trueBranch ? "true" : "false") : "next";
        button.ContextMenu = ConnectionMenu(node.Id, portName);
        return button;
    }

    private void BeginMarquee(PointerPressedEventArgs e)
    {
        if (!e.GetCurrentPoint(_canvas).Properties.IsLeftButtonPressed || !ReferenceEquals(e.Source, _canvas))
        {
            return;
        }

        _marqueeSelecting = true;
        _marqueeAdd = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);
        _marqueeBase = _marqueeAdd ? new HashSet<Guid>(_selectedIds) : new HashSet<Guid>();
        _marqueeStart = e.GetPosition(_canvas);
        _marquee = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#332E86FF")),
            BorderBrush = new SolidColorBrush(Color.Parse("#53D6FF")),
            BorderThickness = new Thickness(1),
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(_marquee, _marqueeStart.X);
        Canvas.SetTop(_marquee, _marqueeStart.Y);
        _canvas.Children.Add(_marquee);
        e.Pointer.Capture(_canvas);
        e.Handled = true;
    }

    private void UpdateMarquee(PointerEventArgs e)
    {
        if (!_marqueeSelecting || _marquee is null || e.Pointer.Captured != _canvas)
        {
            return;
        }

        var current = e.GetPosition(_canvas);
        var left = Math.Max(0, Math.Min(_marqueeStart.X, current.X));
        var top = Math.Max(0, Math.Min(_marqueeStart.Y, current.Y));
        var width = Math.Abs(current.X - _marqueeStart.X);
        var height = Math.Abs(current.Y - _marqueeStart.Y);
        Canvas.SetLeft(_marquee, left);
        Canvas.SetTop(_marquee, top);
        _marquee.Width = width;
        _marquee.Height = height;
        e.Handled = true;
    }

    private void EndMarquee(PointerReleasedEventArgs e)
    {
        if (!_marqueeSelecting)
        {
            return;
        }

        var current = e.GetPosition(_canvas);
        var left = Math.Min(_marqueeStart.X, current.X);
        var right = Math.Max(_marqueeStart.X, current.X);
        var top = Math.Min(_marqueeStart.Y, current.Y);
        var bottom = Math.Max(_marqueeStart.Y, current.Y);

        _selectedIds.Clear();
        foreach (var id in _marqueeBase) _selectedIds.Add(id);

        var selection = new Rect(left, top, Math.Max(1, right - left), Math.Max(1, bottom - top));

        foreach (var node in _graph.Nodes.Where(node => !IsHidden(node)))
        {
            var size = NodeSize(node);
            var bounds = new Rect(node.X, node.Y, size.Width, size.Height);

            if (selection.Intersects(bounds))
            {
                _selectedIds.Add(node.Id);
            }
        }

        if (_marquee is not null)
        {
            _canvas.Children.Remove(_marquee);
            _marquee = null;
        }

        _selected = _graph.Nodes.FirstOrDefault(node => _selectedIds.Contains(node.Id));
        _marqueeSelecting = false;
        e.Pointer.Capture(null);
        Render();
        e.Handled = true;
    }

    private (double Width, double Height) NodeSize(GraphNode node)
    {
        if (node.Kind == GraphNodeKind.Group)
        {
            return (node.GroupWidth, node.GroupHeight);
        }

        if (_nodeCards.TryGetValue(node.Id, out var card) && card.Bounds.Width > 0 && card.Bounds.Height > 0)
        {
            return (card.Bounds.Width, card.Bounds.Height);
        }

        return (200d, 150d);
    }

    private void DrawGuides()
    {
        ClearGuides();
        var selected = _graph.Nodes.Where(node => _selectedIds.Contains(node.Id)).ToList();
        var others = _graph.Nodes.Where(node => !_selectedIds.Contains(node.Id)).ToList();
        var tolerance = 6f;

        foreach (var node in selected)
        {
            var size = NodeSize(node);

            foreach (var other in others)
            {
                var otherSize = NodeSize(other);
                var xAnchors = new[] { other.X, other.X + (float)otherSize.Width, other.X + (float)otherSize.Width / 2f };
                var yAnchors = new[] { other.Y, other.Y + (float)otherSize.Height, other.Y + (float)otherSize.Height / 2f };
                var nodeXAnchors = new[] { node.X, node.X + (float)size.Width, node.X + (float)size.Width / 2f };
                var nodeYAnchors = new[] { node.Y, node.Y + (float)size.Height, node.Y + (float)size.Height / 2f };

                foreach (var anchor in xAnchors)
                {
                    if (nodeXAnchors.Any(value => MathF.Abs(value - anchor) <= tolerance))
                    {
                        AddGuide(new Line
                        {
                            StartPoint = new Point(anchor, 0),
                            EndPoint = new Point(anchor, _canvas.Height),
                            Stroke = new SolidColorBrush(Color.Parse("#E9A23B")),
                            StrokeThickness = 1,
                        });
                        break;
                    }
                }

                foreach (var anchor in yAnchors)
                {
                    if (nodeYAnchors.Any(value => MathF.Abs(value - anchor) <= tolerance))
                    {
                        AddGuide(new Line
                        {
                            StartPoint = new Point(0, anchor),
                            EndPoint = new Point(_canvas.Width, anchor),
                            Stroke = new SolidColorBrush(Color.Parse("#E9A23B")),
                            StrokeThickness = 1,
                        });
                        break;
                    }
                }
            }
        }
    }

    private void AddGuide(Line line)
    {
        _guideLines.Add(line);
        _canvas.Children.Insert(0, line);
    }

    private void ClearGuides()
    {
        foreach (var line in _guideLines) _canvas.Children.Remove(line);
        _guideLines.Clear();
    }

    private void OnNodePressed(GraphNode node, Border card, PointerPressedEventArgs e)
    {
        if (_linkSource is not null && _linkSource != node)
        {
            if (_linkSource.Kind == GraphNodeKind.Condition)
            {
                if (_linkTrue) _linkSource.TrueNext = node.Id;
                else _linkSource.FalseNext = node.Id;
            }
            else
            {
                _linkSource.Next = node.Id;
                _linkSource.DisabledPorts.Remove("next");
            }

            _linkSource = null;
            _selected = node;
            _selectedIds.Clear();
            _selectedIds.Add(node.Id);
            MarkChanged();
            Render();
            e.Handled = true;
            return;
        }

        var additive = e.KeyModifiers.HasFlag(KeyModifiers.Control) || e.KeyModifiers.HasFlag(KeyModifiers.Shift);

        if (additive)
        {
            if (!_selectedIds.Add(node.Id))
            {
                _selectedIds.Remove(node.Id);

                if (_selected == node)
                {
                    _selected = _graph.Nodes.FirstOrDefault(item => _selectedIds.Contains(item.Id));
                }
            }
            else
            {
                _selected = node;
            }

            e.Handled = true;
            Render();
            return;
        }

        if (!_selectedIds.Contains(node.Id))
        {
            _selectedIds.Clear();
            _selectedIds.Add(node.Id);
        }

        _selected = node;
        _dragCard = card;
        _dragNode = node;
        _dragPositions.Clear();

        foreach (var id in _selectedIds)
        {
            if (_graph.Find(id) is { } selected)
            {
                _dragPositions[id] = (selected.X, selected.Y);
            }
        }

        if (node.Kind == GraphNodeKind.Group)
        {
            foreach (var member in _graph.Nodes.Where(member =>
                         member.Id != node.Id && member.Kind != GraphNodeKind.Group &&
                         member.X >= node.X && member.X <= node.X + node.GroupWidth &&
                         member.Y >= node.Y && member.Y <= node.Y + node.GroupHeight))
            {
                _dragPositions[member.Id] = (member.X, member.Y);
            }
        }

        _dragPositions[node.Id] = (node.X, node.Y);
        ClearGuides();

        _dragX = node.X;
        _dragY = node.Y;
        e.Pointer.Capture(card);
        RenderInspector();
        e.Handled = true;
    }

    private void OnNodeMoved(GraphNode node, Border card, PointerEventArgs e)
    {
        if (_dragNode != node || _dragCard is null || e.Pointer.Captured != _dragCard)
        {
            return;
        }

        var position = e.GetPosition(_canvas);
        var dx = position.X - _dragStart.X;
        var dy = position.Y - _dragStart.Y;

        foreach (var (id, start) in _dragPositions)
        {
            if (_graph.Find(id) is not { } selected)
            {
                continue;
            }

            selected.X = (float)Math.Max(0, start.X + dx);
            selected.Y = (float)Math.Max(0, start.Y + dy);

            if (_snapNodes)
            {
                selected.X = MathF.Round(selected.X / 10f) * 10f;
                selected.Y = MathF.Round(selected.Y / 10f) * 10f;
            }
if (_nodeCards.TryGetValue(id, out var selectedCard))
            {
                Canvas.SetLeft(selectedCard, selected.X);
                Canvas.SetTop(selectedCard, selected.Y);
            }
        }

        DrawGuides();
        e.Handled = true;
    }

    private void OnNodeReleased(GraphNode node, Border card, PointerReleasedEventArgs e)
    {
        if (_dragNode == node)
        {
            _dragNode = null;
            _dragCard = null;
            _dragPositions.Clear();
            ClearGuides();
            e.Pointer.Capture(null);
            MarkChanged();
            Render();
        }
    }
    private void AddExpressionError(string expression)
    {
        var error = ExpressionEvaluator.Validate(expression);

        if (error is null)
        {
            return;
        }

        _inspector.Children.Add(new TextBlock
        {
            Text = error,
            FontSize = 10,
            Foreground = new SolidColorBrush(Color.Parse("#E05B5B")),
            TextWrapping = TextWrapping.Wrap,
        });
    }

    private ContextMenu ConnectionMenu(Guid nodeId, string port)
    {
        var menu = new ContextMenu();
        var node = _graph.Find(nodeId);
        var targetId = node is null
            ? null
            : port switch
            {
                "true" => node.TrueNext,
                "false" => node.FalseNext,
                _ => node.Next,
            };

        if (targetId is { } id && _graph.Find(id) is { } target)
        {
            var go = new MenuItem { Header = "Ir al destino" };
            go.Click += (_, _) =>
            {
                _selected = target;
                _selectedIds.Clear();
                _selectedIds.Add(target.Id);
                Render();
            };
            menu.Items.Add(go);
        }

        if (node is not null)
        {
            var disabled = node.DisabledPorts.Contains(port);
            var toggle = new MenuItem { Header = disabled ? "Habilitar conexión" : "Deshabilitar conexión" };
            toggle.Click += (_, _) =>
            {
                if (!node.DisabledPorts.Remove(port)) node.DisabledPorts.Add(port);
                MarkChanged();
                Render();
            };
            menu.Items.Add(toggle);
        }

        var delete = new MenuItem { Header = "Eliminar conexión" };
        delete.Click += (_, _) => ClearConnection(nodeId, port);
        menu.Items.Add(delete);
        return menu;
    }
    private void ClearConnection(Guid nodeId, string port)
    {
        if (_graph.Find(nodeId) is not { } node)
        {
            return;
        }

        switch (port)
        {
            case "true": node.TrueNext = null; break;
            case "false": node.FalseNext = null; break;
            default: node.Next = null; break;
        }

        _linkSource = null;
        MarkChanged();
        Render();
    }

    private void RenderInspector()
    {
        _inspector.Children.Clear();
        _inspector.Children.Add(new TextBlock
        {
            Text = "NODO",
            FontSize = 10,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
        });

        if (_selectedIds.Count > 1)
        {
            _inspector.Children.Add(new TextBlock
            {
                Text = $"{_selectedIds.Count} nodos seleccionados · Ctrl+C/V o Supr",
                FontSize = 10.5,
                Foreground = new SolidColorBrush(Color.Parse("#53D6FF")),
                TextWrapping = TextWrapping.Wrap,
            });
        }

        if (_selected is null)
        {
            _inspector.Children.Add(new TextBlock
            {
                Text = "Selecciona un nodo del lienzo.",
                FontSize = 11,
                Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                TextWrapping = TextWrapping.Wrap,
            });
            AddValidationPanel();
            return;
        }

        var node = _selected;

        if (node.Kind == GraphNodeKind.Group)
        {
            _inspector.Children.Add(Label("Título del grupo"));
            _inspector.Children.Add(TextInput(node.Message, value => { node.Message = value; MarkChanged(); }));
            _inspector.Children.Add(Label("Ancho"));
            _inspector.Children.Add(Number(node.GroupWidth, 80, value => { node.GroupWidth = (float)value; MarkChanged(); Render(); }));
            _inspector.Children.Add(Label("Alto"));
            _inspector.Children.Add(Number(node.GroupHeight, 60, value => { node.GroupHeight = (float)value; MarkChanged(); Render(); }));
            _inspector.Children.Add(Check("Plegado", node.Collapsed, value => { node.Collapsed = value; MarkChanged(); Render(); }));
            _inspector.Children.Add(DeleteButton(node));
            _inspector.Children.Add(BreakpointButton(node));
            AddValidationPanel();
            return;
        }

        if (node.Kind == GraphNodeKind.Comment)
        {
            _inspector.Children.Add(Label("Comentario"));
            _inspector.Children.Add(TextInput(node.Message, value => { node.Message = value; MarkChanged(); }));
            _inspector.Children.Add(DeleteButton(node));
            _inspector.Children.Add(BreakpointButton(node));
            AddValidationPanel();
            return;
        }

        if (node.Kind == GraphNodeKind.Event)
        {
            _inspector.Children.Add(Label("Evento"));
            _inspector.Children.Add(Combo(
                new[] { "Al iniciar", "Cada cierto tiempo", "Al entrar", "Al pulsar E", "Al morir" },
                (int)node.Event,
                index => { node.Event = (GraphEventKind)index; MarkChanged(); Render(); }));

            if (node.Event is GraphEventKind.OnPlayerEnter or GraphEventKind.OnInteract)
            {
                _inspector.Children.Add(Label("Radio"));
                _inspector.Children.Add(Number(node.Radius, 0.1, value => { node.Radius = (float)value; MarkChanged(); }));
            }

            if (node.Event == GraphEventKind.OnTimer)
            {
                _inspector.Children.Add(Label("Intervalo (s)"));
                _inspector.Children.Add(Number(node.Interval, 0.25, value => { node.Interval = (float)value; MarkChanged(); }));
            }

            _inspector.Children.Add(Check("Solo una vez", node.Once, value => { node.Once = value; MarkChanged(); }));
        }
        else if (node.Kind == GraphNodeKind.Condition)
        {
            _inspector.Children.Add(Label("Condición"));
            _inspector.Children.Add(Combo(
                new[] { "Siempre", "Tiene objeto", "Vida por debajo", "Expresión" },
                (int)node.Condition,
                index => { node.Condition = (GraphConditionKind)index; MarkChanged(); Render(); }));

            if (node.Condition == GraphConditionKind.HasItem)
            {
                _inspector.Children.Add(Label("Objeto"));
                _inspector.Children.Add(TextInput(node.ItemId, value => { node.ItemId = value; MarkChanged(); }));
            }

            if (node.Condition == GraphConditionKind.HealthBelow)
            {
                _inspector.Children.Add(Label("Vida ≤"));
                _inspector.Children.Add(Number(node.Threshold, 0, value => { node.Threshold = (float)value; MarkChanged(); }));
            }

            if (node.Condition == GraphConditionKind.Expression)
            {
                _inspector.Children.Add(Label("Expresión"));
                _inspector.Children.Add(ExpressionInput(node.Expression, value => { node.Expression = value; MarkChanged(); }));
                _inspector.Children.Add(new TextBlock
                {
                    Text = "Ej: contador >= 3 && tiene(\"llave\")",
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                });
                AddExpressionError(node.Expression);
            }
        }
        else
        {
            _inspector.Children.Add(Label("Acción"));
            _inspector.Children.Add(Combo(
                new[] { "Mensaje", "Abrir puerta", "Activar", "Desactivar", "Generar", "Checkpoint", "Ganar", "Dañar", "Curar", "Teletransportar", "Fijar variable", "Sumar variable", "Alternar flag" },
                (int)node.Action,
                index => { node.Action = (ActionKind)index; MarkChanged(); Render(); }));

            _inspector.Children.Add(Label("Objetivo"));
            _inspector.Children.Add(TextInput(node.TargetName, value => { node.TargetName = value; MarkChanged(); }));

            _inspector.Children.Add(Label("Mensaje"));
            _inspector.Children.Add(TextInput(node.Message, value => { node.Message = value; MarkChanged(); }));

            _inspector.Children.Add(Label("Cantidad"));
            _inspector.Children.Add(Number(node.Amount, 0, value => { node.Amount = (float)value; MarkChanged(); }));

            if (node.Action is ActionKind.SetVariable or ActionKind.AddVariable or ActionKind.ToggleFlag)
            {
                _inspector.Children.Add(Label("Variable"));
                _inspector.Children.Add(TextInput(node.VariableName, value => { node.VariableName = value; MarkChanged(); }));
                _inspector.Children.Add(Label("Expresión / valor"));
                _inspector.Children.Add(ExpressionInput(node.Expression, value => { node.Expression = value; MarkChanged(); }));
                AddExpressionError(node.Expression);
                _inspector.Children.Add(new TextBlock
                {
                    Text = "contador + 1, true, 3.5…",
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                });
            }
        }

        var delete = new Button
        {
            Content = "Eliminar nodo",
            Margin = new Thickness(0, 12, 0, 0),
            HorizontalAlignment = HorizontalAlignment.Stretch,
        };
        delete.Click += (_, _) =>
        {
            _graph.RemoveNode(node.Id);
            _selected = null;
            MarkChanged();
            Render();
        };
        _inspector.Children.Add(delete);
        _inspector.Children.Add(BreakpointButton(node));
        AddValidationPanel();
    }

    private Button DeleteButton(GraphNode node)
    {
        var button = new Button { Content = "Eliminar nodo", HorizontalAlignment = HorizontalAlignment.Stretch, Margin = new Thickness(0, 12, 0, 0) };
        button.Click += (_, _) =>
        {
            _graph.RemoveNode(node.Id);
            _selected = null;
            _selectedIds.Clear();
            MarkChanged();
            Render();
        };
        return button;
    }

    private Button BreakpointButton(GraphNode node)
    {
        var active = _graph.Breakpoints.Contains(node.Id);
        var button = new Button
        {
            Content = active ? "Quitar breakpoint" : "Poner breakpoint",
            HorizontalAlignment = HorizontalAlignment.Stretch,
            Margin = new Thickness(0, 6, 0, 0),
        };
        button.Click += (_, _) => ToggleBreakpoint(node);
        return button;
    }

    private static TextBlock Label(string text) => new()
    {
        Text = text,
        FontSize = 10.5,
        Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
        Margin = new Thickness(0, 4, 0, 0),
    };

    private static ComboBox Combo(string[] items, int selected, Action<int> changed)
    {
        var combo = new ComboBox
        {
            ItemsSource = items,
            SelectedIndex = selected,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontSize = 11,
        };
        combo.SelectionChanged += (_, _) =>
        {
            if (combo.SelectedIndex >= 0) changed(combo.SelectedIndex);
        };
        return combo;
    }

    private static NumericUpDown Number(double value, double minimum, Action<double> changed)
    {
        var box = new NumericUpDown
        {
            Value = (decimal)value,
            Minimum = (decimal)minimum,
            Increment = 1,
            FormatString = "0.##",
            FontSize = 11,
        };
        box.ValueChanged += (_, _) =>
        {
            if (box.Value is { } current) changed((double)current);
        };
        return box;
    }

    private static TextBox ExpressionInput(string value, Action<string> changed)
    {
        var box = new TextBox { Text = value, FontSize = 11 };

        if (ExpressionEvaluator.Validate(value) is not null)
        {
            box.BorderBrush = new SolidColorBrush(Color.Parse("#E05B5B"));
            box.BorderThickness = new Thickness(2);
        }

        box.LostFocus += (_, _) => changed(box.Text ?? string.Empty);
        return box;
    }

    private static TextBox TextInput(string value, Action<string> changed)
    {
        var box = new TextBox { Text = value, FontSize = 11 };
        box.LostFocus += (_, _) => changed(box.Text ?? string.Empty);
        return box;
    }

    private static CheckBox Check(string text, bool value, Action<bool> changed)
    {
        var check = new CheckBox { Content = text, IsChecked = value, FontSize = 11 };
        check.IsCheckedChanged += (_, _) => changed(check.IsChecked == true);
        return check;
    }

    private static string KindLabel(GraphNode node) => node.Kind switch
    {
        GraphNodeKind.Event => "EVENTO",
        GraphNodeKind.Condition => "CONDICIÓN",
        GraphNodeKind.Comment => "COMENTARIO",
        GraphNodeKind.Group => "GRUPO",
        _ => "ACCIÓN",
    };

    private static string NodeSummary(GraphNode node)
    {
        if (node.Kind is GraphNodeKind.Comment or GraphNodeKind.Group)
        {
            return node.Message;
        }

        if (node.Kind == GraphNodeKind.Event)
        {
            return node.Event switch
            {
                GraphEventKind.OnStart => "Al iniciar la partida",
                GraphEventKind.OnTimer => $"Cada {node.Interval:0.##} s",
                GraphEventKind.OnPlayerEnter => $"Jugador entra (radio {node.Radius:0.##})",
                GraphEventKind.OnInteract => $"E cerca (radio {node.Radius:0.##})",
                _ => "Al morir el jugador",
            };
        }

        if (node.Kind == GraphNodeKind.Condition)
        {
            return node.Condition switch
            {
                GraphConditionKind.HasItem => $"¿Tiene «{node.ItemId}»?",
                GraphConditionKind.HealthBelow => $"¿Vida ≤ {node.Threshold:0.##}?",
                GraphConditionKind.Expression => $"¿{node.Expression}?",
                _ => "Siempre",
            };
        }

        var target = string.IsNullOrWhiteSpace(node.TargetName) ? "—" : node.TargetName;
        return node.Action switch
        {
            ActionKind.Message => node.Message,
            ActionKind.OpenDoor => $"Abrir {target}",
            ActionKind.Activate => $"Activar {target}",
            ActionKind.Deactivate => $"Desactivar {target}",
            ActionKind.Spawn => $"Generar en {target}",
            ActionKind.Checkpoint => "Guardar checkpoint",
            ActionKind.Win => "Ganar",
            ActionKind.Damage => $"Dañar {target} ({node.Amount:0.#})",
            ActionKind.Heal => $"Curar {target} ({node.Amount:0.#})",
            ActionKind.SetVariable => $"{node.VariableName} = {node.Expression}",
            ActionKind.AddVariable => $"{node.VariableName} += {node.Expression}",
            ActionKind.ToggleFlag => $"Alternar {node.VariableName}",
            _ => $"Teletransportar a {target}",
        };
    }
    private sealed record LinkRef(Guid NodeId, string Port);
}
