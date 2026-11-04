using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Ollin.Core.Input;
using Ollin.Core.Projects;

namespace Ollin.Editor.Views;

/// <summary>Editor del Input Map del proyecto.</summary>
public sealed class InputMapWindow : Window
{
    private readonly ProjectInfo _project;
    private readonly StackPanel _rows = new() { Spacing = 6 };
    private readonly TextBlock _error;
    private TextBox? _captureBox;
    private TextBox? _capturePadBox;
    private DispatcherTimer? _padTimer;

    public InputMapWindow(ProjectInfo project)
    {
        _project = project;
        Title = "Mapa de entrada";
        Width = 620;
        Height = 560;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Background = new SolidColorBrush(Color.Parse("#151922"));
        Foreground = new SolidColorBrush(Color.Parse("#E7EAF0"));

        _error = new TextBlock
        {
            FontSize = 11.5,
            Foreground = new SolidColorBrush(Color.Parse("#E06C75")),
            TextWrapping = TextWrapping.Wrap,
            IsVisible = false,
        };

        var add = new Button { Content = "Añadir acción" };
        add.Click += (_, _) => AddRow(new InputActionDefinition { Name = "nueva_accion" });
        var reset = new Button { Content = "Restablecer" };
        reset.Click += (_, _) => { _rows.Children.Clear(); foreach (var action in InputMap.CreateDefaults()) AddRow(action); };
        var save = new Button
        {
            Content = "Guardar",
            Background = new SolidColorBrush(Color.Parse("#E9A23B")),
            Foreground = new SolidColorBrush(Color.Parse("#1A1206")),
        };
        save.Click += (_, _) => Save();
        var cancel = new Button { Content = "Cancelar" };
        cancel.Click += (_, _) => Close(false);

        foreach (var action in project.InputActions) AddRow(action);
        KeyDown += OnCaptureKey;

        Content = new StackPanel
        {
            Margin = new Thickness(20),
            Spacing = 8,
            Children =
            {
                new TextBlock
                {
                    Text = "Mapa de entrada",
                    FontSize = 20,
                    FontWeight = FontWeight.Bold,
                    Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
                },
                new TextBlock
                {
                    Text = "Bindings: key:A, key:Left, pad:DPadLeft, axis:LeftStickX-",
                    FontSize = 11,
                    TextWrapping = TextWrapping.Wrap,
                    Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
                },
                new ScrollViewer { Content = _rows, Height = 380 },
                _error,
                new StackPanel
                {
                    Orientation = Orientation.Horizontal,
                    Spacing = 8,
                    Children = { reset, add, cancel, save },
                },
            },
        };
    }

    private void AddRow(InputActionDefinition action)
    {
        var name = new TextBox { Text = action.Name, Watermark = "acción" };
        var bindings = new TextBox
        {
            Text = string.Join(", ", action.Bindings.Select(binding => binding.ToString())),
            Watermark = "key:A, pad:A",
        };
        var remove = new Button { Content = "✕", Width = 28, Height = 24, Padding = new Thickness(0) };

        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("180,*,Auto") };
        row.Children.Add(name);
        Grid.SetColumn(bindings, 1);
        row.Children.Add(bindings);
        Grid.SetColumn(remove, 2);
        row.Children.Add(remove);

        var panel = new StackPanel { Spacing = 3, Tag = (name, bindings) };
        panel.Children.Add(row);
        remove.Click += (_, _) => _rows.Children.Remove(panel);
        _rows.Children.Add(panel);
    }
    private void StartPadCapture(TextBox box)
    {
        _capturePadBox = box;
        _error.Text = "Pulsa un botón del mando…";
        _error.IsVisible = true;
        _padTimer ??= new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(50) };
        _padTimer.Tick -= OnPadTick;
        _padTimer.Tick += OnPadTick;
        _padTimer.Start();
    }

    private void OnPadTick(object? sender, EventArgs e)
    {
        if (_capturePadBox is null) { _padTimer?.Stop(); return; }
        GamepadInput.Poll();
        foreach (var name in new[] { "A", "B", "X", "Y", "Start", "Back", "ShoulderLeft", "ShoulderRight", "DPadUp", "DPadDown", "DPadLeft", "DPadRight" })
        {
            if (!GamepadInput.WasPressed(name)) continue;
            _capturePadBox.Text = $"pad:{name}";
            _capturePadBox = null;
            _padTimer?.Stop();
            _error.Text = $"Binding: pad:{name}";
            break;
        }
    }

    private void OnCaptureKey(object? sender, Avalonia.Input.KeyEventArgs e)
    {
        if (_captureBox is null) return;
        var name = e.Key switch
        {
            Avalonia.Input.Key.LeftCtrl => "LeftControl",
            Avalonia.Input.Key.RightCtrl => "RightControl",
            Avalonia.Input.Key.LeftShift => "LeftShift",
            Avalonia.Input.Key.RightShift => "RightShift",
            _ => e.Key.ToString(),
        };
        _captureBox.Text = $"key:{name}";
        _captureBox = null;
        _error.Text = $"Binding: key:{name}";
        e.Handled = true;
    }

    private static Control Place(Control control, int column)
    {
        Grid.SetColumn(control, column);
        return control;
    }

    private void Save()
    {
        var result = new List<InputActionDefinition>();
        foreach (var child in _rows.Children.OfType<StackPanel>())
        {
            if (child.Tag is not ValueTuple<TextBox, TextBox> fields) continue;
            var action = new InputActionDefinition { Name = fields.Item1.Text?.Trim() ?? string.Empty };
            if (string.IsNullOrWhiteSpace(action.Name)) continue;
            foreach (var text in (fields.Item2.Text ?? string.Empty).Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                if (TryParse(text.Trim(), out var binding)) action.Bindings.Add(binding);
            }
            result.Add(action);
        }

        if (result.Count == 0)
        {
            _error.Text = "Añade al menos una acción.";
            _error.IsVisible = true;
            return;
        }

        _project.InputActions = result;
        ProjectService.Save(_project);
        InputMap.Set(result);
        _padTimer?.Stop();
        Close(true);
    }

    private static bool TryParse(string text, out InputBinding binding)
    {
        binding = new InputBinding();
        if (text.StartsWith("key:", StringComparison.OrdinalIgnoreCase))
        {
            binding.Device = "key"; binding.Control = text[4..].Trim(); return binding.Control.Length > 0;
        }
        if (text.StartsWith("pad:", StringComparison.OrdinalIgnoreCase))
        {
            binding.Device = "gamepad"; binding.Control = text[4..].Trim(); return binding.Control.Length > 0;
        }
        if (text.StartsWith("axis:", StringComparison.OrdinalIgnoreCase))
        {
            binding.Device = "axis";
            var control = text[5..].Trim();
            binding.Direction = control.EndsWith("-", StringComparison.Ordinal) ? -1 : 1;
            binding.Control = control.TrimEnd('+', '-');
            return binding.Control.Length > 0;
        }
        binding.Device = "key"; binding.Control = text; return text.Length > 0;
    }
}
