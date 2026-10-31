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

namespace Ollin.Editor.Views;

/// <summary>Editor de grafos de nodos por entidad.</summary>
public sealed class NodeGraphWindow : Window
{
    private readonly MainViewModel _viewModel;
    private readonly Entity _entity;
    private readonly NodeGraph _graph;
    private readonly Canvas _canvas = new();
    private readonly LayoutTransformControl _canvasTransform = new();
    private readonly NodeGraphHistory _history = new();
    private readonly List<GraphIssue> _issues = new();
    private readonly TextBlock _zoomText = new() { FontSize = 11, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(6, 0) };
    private readonly DispatcherTimer _debugTimer;
    private GraphSnapshot? _lastCommitted;
    private GraphNode? _clipboard;
    private Guid? _activeNodeId;
    private double _zoom = 1.0;
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

    public NodeGraphWindow(MainViewModel viewModel, Entity entity)
    {
        _viewModel = viewModel;
        _entity = entity;
        _graph = entity.GetComponent<NodeGraph>()!;
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
        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id);
        _debugTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(120) };
        _debugTimer.Tick += (_, _) => PollDebug();
        _debugTimer.Start();
        Closed += (_, _) => _debugTimer.Stop();
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
        _canvasTransform.Child = _canvas;
        _canvasTransform.LayoutTransform = new ScaleTransform(_zoom, _zoom);

        var toolbar = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
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
        toolbar.Children.Add(_zoomText);
        UpdateZoomText();

        var scroller = new ScrollViewer
        {
            Content = _canvasTransform,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };
        scroller.PointerWheelChanged += (_, e) =>
        {
            if (e.KeyModifiers.HasFlag(KeyModifiers.Control))
            {
                ZoomBy(e.Delta.Y > 0 ? 0.1 : -0.1);
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
        _zoom = Math.Clamp(zoom, 0.35, 2.0);
        _canvasTransform.LayoutTransform = new ScaleTransform(_zoom, _zoom);
        UpdateZoomText();
    }

    private void UpdateZoomText() => _zoomText.Text = $"{_zoom * 100:0}%";

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

    private void MarkChanged()
    {
        if (_lastCommitted is not null)
        {
            _history.Record(_lastCommitted);
        }

        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id);
        _viewModel.MarkDirty("grafo-nodos");
    }
    private void Undo()
    {
        if (!_history.Undo(_graph, _selected?.Id, out var selectedId)) return;
        _selected = selectedId is { } id ? _graph.Find(id) : null;
        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id);
        _viewModel.MarkDirty("grafo-nodos");
        Render();
    }

    private void Redo()
    {
        if (!_history.Redo(_graph, _selected?.Id, out var selectedId)) return;
        _selected = selectedId is { } id ? _graph.Find(id) : null;
        _lastCommitted = GraphEditing.Capture(_graph, _selected?.Id);
        _viewModel.MarkDirty("grafo-nodos");
        Render();
    }
    private void CopySelected()
    {
        if (_selected is null) return;
        _clipboard = GraphEditing.CloneNode(_selected, keepLinks: false);
        _status.Text = $"Nodo copiado: {KindLabel(_selected)}";
    }

    private void Paste()
    {
        if (_clipboard is null) return;
        var copy = GraphEditing.Paste(_clipboard);
        _graph.Nodes.Add(copy);
        _selected = copy;
        MarkChanged();
        Render();
    }

    private void DeleteSelected()
    {
        if (_selected is null) return;
        _graph.RemoveNode(_selected.Id);
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
        else if (control && e.Key == Key.C) { CopySelected(); e.Handled = true; }
        else if (control && e.Key == Key.V) { Paste(); e.Handled = true; }
        else if (e.Key == Key.Delete) { DeleteSelected(); e.Handled = true; }
    }

    private void PollDebug()
    {
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


    private void Render()
    {
        Validate();
        _canvas.Children.Clear();

        foreach (var node in _graph.Nodes)
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

        foreach (var node in _graph.Nodes)
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
            StrokeThickness = 2,
        };
        _canvas.Children.Add(line);
    }

    private Control BuildNodeCard(GraphNode node)
    {
        var kindColor = node.Kind switch
        {
            GraphNodeKind.Event => "#E9A23B",
            GraphNodeKind.Condition => "#5B9CE0",
            _ => "#47B8A8",
        };

        var issue = _issues.FirstOrDefault(item => item.Node?.Id == node.Id);
        var active = _viewModel.IsPlaying && _activeNodeId == node.Id;
        var borderColor = active ? "#53D6FF" : issue is { IsError: true } ? "#E05B5B" : issue is not null ? "#E9C46A" : node == _selected ? "#FFFFFF" : kindColor;
        var card = new Border
        {
            Width = 200,
            Background = new SolidColorBrush(Color.Parse("#1A1F29")),
            BorderBrush = new SolidColorBrush(Color.Parse(borderColor)),
            BorderThickness = new Thickness(active ? 3 : node == _selected ? 2 : 1),
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
            Text = KindLabel(node),
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
        else
        {
            ports.Children.Add(PortButton("Conectar →", node, true));
        }

        content.Children.Add(ports);
        card.Child = content;
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
            Render();
        };
        return button;
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
            }

            _linkSource = null;
            _selected = node;
            MarkChanged();
            Render();
            e.Handled = true;
            return;
        }

        _selected = node;
        _dragCard = card;
        _dragNode = node;
        _dragStart = e.GetPosition(_canvas);
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
        node.X = (float)Math.Max(0, _dragX + position.X - _dragStart.X);
        node.Y = (float)Math.Max(0, _dragY + position.Y - _dragStart.Y);
        Canvas.SetLeft(card, node.X);
        Canvas.SetTop(card, node.Y);
        e.Handled = true;
    }

    private void OnNodeReleased(GraphNode node, Border card, PointerReleasedEventArgs e)
    {
        if (_dragNode == node)
        {
            _dragNode = null;
            _dragCard = null;
            e.Pointer.Capture(null);
            MarkChanged();
            Render();
        }
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
                _inspector.Children.Add(TextInput(node.Expression, value => { node.Expression = value; MarkChanged(); }));
                _inspector.Children.Add(new TextBlock
                {
                    Text = "Ej: contador >= 3 && tiene(\"llave\")",
                    FontSize = 10,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                });
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
                _inspector.Children.Add(TextInput(node.Expression, value => { node.Expression = value; MarkChanged(); }));
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
        AddValidationPanel();
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
        _ => "ACCIÓN",
    };

    private static string NodeSummary(GraphNode node)
    {
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

}