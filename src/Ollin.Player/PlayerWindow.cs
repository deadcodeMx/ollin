using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using System.IO;
using System.Text.Json;
using Ollin.Core.Audio;
using Ollin.Core.Model.Components;
using Ollin.Core.Model;
using Avalonia.Controls.Primitives;
using Ollin.Core.Input;
using Ollin.Core.Projects;
using Ollin.Editor.Controls;
using Ollin.Editor.ViewModels;

namespace Ollin.Player;

/// <summary>Ventana del juego: pantalla de inicio con el logo y el viewport.</summary>
public sealed class PlayerWindow : Window
{
    private readonly GameManifest _manifest;
    private readonly MainViewModel _viewModel;
    private readonly ViewportSurface _surface;
    private readonly Grid _splash;
    private readonly Grid _pauseOverlay;
    private bool _started;
    private bool _paused;
    private TextBlock? _saveStatus;
    private Slider? _volumeSlider;
    private Slider? _sensitivitySlider;
    private CheckBox? _fullscreenCheck;
    private StackPanel? _optionsPanel;
    private StackPanel? _mainPanel;
    private ComboBox? _slotCombo;
    private int _saveSlot = 1;
    private DispatcherTimer? _autosaveTimer;

    public PlayerWindow(LoadedGame game)
    {
        var manifest = game.Manifest;
        _manifest = manifest;
        Title = manifest.ProjectName;
        Width = 1280;
        Height = 720;
        MinWidth = 640;
        MinHeight = 360;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = Brushes.Black;

        _viewModel = new MainViewModel(initialize: false);
        Ollin.Core.Audio.Audio.Root = game.Root;
        _viewModel.OpenProjectLocation(game.Root, logToConsole: false, remember: false);

        if (game.ScriptsImage is { Length: > 0 })
        {
            _viewModel.LoadPrecompiledScripts(game.ScriptsImage);
        }

        _surface = new ViewportSurface
        {
            HorizontalAlignment = HorizontalAlignment.Stretch,
            VerticalAlignment = VerticalAlignment.Stretch,
        };
        _surface.Viewport.ViewModel = _viewModel;

        var content = new Grid();
        content.Children.Add(_surface);
        content.Children.Add(new PlayerHud { DataContext = _viewModel });
        var pauseButton = new Button
        {
            Content = "II  Pausa",
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Top,
            Margin = new Thickness(0, 14, 14, 0),
            Padding = new Thickness(12, 6),
            Opacity = 0.65,
        };
        pauseButton.Click += (_, _) => SetPaused(true);
        content.Children.Add(pauseButton);

        if (manifest.Watermark)
        {
            content.Children.Add(BuildWatermark());
        }

        _splash = BuildSplash();
        content.Children.Add(_splash);
        _pauseOverlay = BuildPauseOverlay();
        _pauseOverlay.IsVisible = false;
        content.Children.Add(_pauseOverlay);
        Content = content;
        LoadSettings();

        Opened += (_, _) =>
        {
            Activate();
            _surface.Focus();
        };

        // Entrada a nivel de ventana: funciona aunque el foco esté en otro control.
        AddHandler(KeyDownEvent, OnPlayerKeyDown, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        AddHandler(KeyUpEvent, OnPlayerKeyUp, Avalonia.Interactivity.RoutingStrategies.Tunnel);
        PointerMoved += OnPlayerPointerMoved;
        PointerPressed += OnPlayerPointerPressed;
        PointerReleased += OnPlayerPointerReleased;

        if (manifest.Splash)
        {
            DispatcherTimer.RunOnce(StartGame, TimeSpan.FromSeconds(2.4));
        }
        else
        {
            DispatcherTimer.RunOnce(StartGame, TimeSpan.FromMilliseconds(120));
        }
    }

    private Grid BuildSplash()
    {
        var grid = new Grid { Background = new SolidColorBrush(Color.Parse("#0B0D12")) };

        var stack = new StackPanel
        {
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
            Spacing = 10,
        };

        try
        {
            stack.Children.Add(new Image
            {
                Source = new Bitmap(AssetLoader.Open(new Uri("avares://Ollin.Editor/Assets/ollin.png"))),
                Width = 180,
                Height = 180,
            });
        }
        catch
        {
            // Sin logo, queda el texto.
        }

        stack.Children.Add(new TextBlock
        {
            Text = "Ollin",
            FontSize = 42,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        stack.Children.Add(new TextBlock
        {
            Text = _manifest.ProjectName,
            FontSize = 16,
            Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
            HorizontalAlignment = HorizontalAlignment.Center,
        });

        stack.Children.Add(new TextBlock
        {
            Text = $"Ollin {typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "0.0.0"}",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        });

        stack.Children.Add(new TextBlock
        {
            Text = "Hecho con Ollin",
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#8B94A7")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 14, 0, 0),
        });

        grid.Children.Add(stack);
        return grid;
    }

    private Control BuildWatermark() => new TextBlock
    {
        Text = "Hecho con Ollin",
        FontSize = 11,
        Foreground = Brushes.White,
        Opacity = 0.4,
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        Margin = new Thickness(0, 0, 14, 10),
        IsHitTestVisible = false,
    };

    private void StartGame()
    {
        if (_started)
        {
            return;
        }

        _started = true;
        _splash.IsVisible = false;
        _viewModel.StartPlayback();
        Activate();
        _surface.Focus();
        _autosaveTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(45) };
        _autosaveTimer.Tick += (_, _) => Autosave();
        _autosaveTimer.Start();
    }

    private Grid BuildPauseOverlay()
    {
        var overlay = new Grid
        {
            Background = new SolidColorBrush(Color.Parse("#D905070B")),
            IsHitTestVisible = true,
        };

        var panel = new Border
        {
            Background = new SolidColorBrush(Color.Parse("#151922")),
            BorderBrush = new SolidColorBrush(Color.Parse("#E9A23B")),
            BorderThickness = new Thickness(1),
            CornerRadius = new CornerRadius(10),
            Padding = new Thickness(26, 22),
            Width = 360,
            HorizontalAlignment = HorizontalAlignment.Center,
            VerticalAlignment = VerticalAlignment.Center,
        };

        var root = new StackPanel { Spacing = 8 };
        panel.Child = root;

        root.Children.Add(new TextBlock
        {
            Text = "Pausa",
            FontSize = 24,
            FontWeight = FontWeight.Bold,
            Foreground = new SolidColorBrush(Color.Parse("#E9A23B")),
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 0, 0, 6),
        });

        _mainPanel = new StackPanel { Spacing = 8 };
        _mainPanel.Children.Add(MakeButton("Continuar", () => SetPaused(false)));
        _slotCombo = new ComboBox
        {
            ItemsSource = new[] { "Ranura 1", "Ranura 2", "Ranura 3" },
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            FontSize = 12,
        };
        _slotCombo.SelectionChanged += (_, _) =>
        {
            _saveSlot = Math.Clamp(_slotCombo.SelectedIndex + 1, 1, 3);
            if (_saveStatus is not null) _saveStatus.Text = $"Ranura {_saveSlot} seleccionada";
        };
        _mainPanel.Children.Add(_slotCombo);
        _mainPanel.Children.Add(MakeButton("Guardar partida", SaveToFile));
        _mainPanel.Children.Add(MakeButton("Cargar partida", LoadFromFile));
        _mainPanel.Children.Add(MakeButton("Opciones", () =>
        {
            _mainPanel.IsVisible = false;
            _optionsPanel!.IsVisible = true;
        }));
        _mainPanel.Children.Add(MakeButton("Salir del juego", Close));
        root.Children.Add(_mainPanel);

        _saveStatus = new TextBlock
        {
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.Parse("#AAB4C4")),
            TextWrapping = TextWrapping.Wrap,
            HorizontalAlignment = HorizontalAlignment.Center,
            Margin = new Thickness(0, 6, 0, 0),
        };
        root.Children.Add(_saveStatus);

        _optionsPanel = new StackPanel { Spacing = 7, IsVisible = false };
        _optionsPanel.Children.Add(new TextBlock
        {
            Text = "Volumen",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
        });
        _volumeSlider = new Slider { Minimum = 0, Maximum = 1, Value = Audio.MasterVolume, Width = 290 };
        _volumeSlider.PropertyChanged += (_, args) =>
        {
            if (args.Property == Slider.ValueProperty)
            {
                Audio.MasterVolume = (float)_volumeSlider.Value;
                SaveSettings();
            }
        };
        _optionsPanel.Children.Add(_volumeSlider);

        _optionsPanel.Children.Add(new TextBlock
        {
            Text = "Sensibilidad del ratón",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
            Margin = new Thickness(0, 6, 0, 0),
        });
        _sensitivitySlider = new Slider { Minimum = 0.0004, Maximum = 0.006, Value = 0.0022, Width = 290 };
        _sensitivitySlider.PropertyChanged += (_, args) =>
        {
            if (args.Property == Slider.ValueProperty)
            {
                ApplySensitivity((float)_sensitivitySlider.Value);
                SaveSettings();
            }
        };
        _optionsPanel.Children.Add(_sensitivitySlider);

        _fullscreenCheck = new CheckBox
        {
            Content = "Pantalla completa",
            FontSize = 12,
            Foreground = new SolidColorBrush(Color.Parse("#E7EAF0")),
            Margin = new Thickness(0, 6, 0, 0),
        };
        _fullscreenCheck.IsCheckedChanged += (_, _) =>
        {
            WindowState = _fullscreenCheck.IsChecked == true ? WindowState.FullScreen : WindowState.Normal;
            SaveSettings();
        };
        _optionsPanel.Children.Add(_fullscreenCheck);

        _optionsPanel.Children.Add(MakeButton("Volver", () =>
        {
            _optionsPanel.IsVisible = false;
            _mainPanel.IsVisible = true;
        }));
        root.Children.Add(_optionsPanel);

        overlay.Children.Add(panel);
        return overlay;
    }

    private Button MakeButton(string text, Action action)
    {
        var button = new Button
        {
            Content = text,
            HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Center,
            Height = 34,
        };
        button.Click += (_, _) => action();
        return button;
    }

    private void SetPaused(bool paused)
    {
        if (!_started || _paused == paused)
        {
            return;
        }

        _paused = paused;
        _pauseOverlay.IsVisible = paused;
        InputState.Reset();
        _viewModel.TogglePause();
        _surface.Focus();
    }

    private string SavePath
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ollin",
                "Saves");
            var name = string.Concat(_manifest.ProjectName.Where(char.IsLetterOrDigit));
            return Path.Combine(folder, $"{(name.Length == 0 ? "juego" : name)}-slot{_saveSlot}.json");
        }
    }

    private string SettingsPath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "Ollin",
        "player-settings.json");

    private string AutosavePath
    {
        get
        {
            var folder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "Ollin",
                "Saves");
            var name = string.Concat(_manifest.ProjectName.Where(char.IsLetterOrDigit));
            return Path.Combine(folder, $"{(name.Length == 0 ? "juego" : name)}-slot{_saveSlot}-auto.json");
        }
    }

    private void Autosave()
    {
        if (!_started || _paused || !_viewModel.IsPlaying)
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(AutosavePath) ?? ".");
            _viewModel.SaveGameTo(AutosavePath);
            Ollin.Core.Hud.HudHost.ShowMessage("Autoguardado", 1.5f);
        }
        catch
        {
            // El autoguardado no debe interrumpir la partida.
        }
    }
    private void SaveToFile()
    {
        Directory.CreateDirectory(Path.GetDirectoryName(SavePath) ?? ".");
        _viewModel.SaveGameTo(SavePath);

        if (_saveStatus is not null)
        {
            _saveStatus.Text = File.Exists(SavePath)
                ? $"Partida guardada en {SavePath}"
                : "No se pudo guardar la partida";
        }
    }

    private void LoadFromFile()
    {
        if (!File.Exists(SavePath))
        {
            if (_saveStatus is not null) _saveStatus.Text = "Todavía no hay una partida guardada.";
            return;
        }

        _viewModel.LoadGameFrom(SavePath);
        _paused = false;
        _pauseOverlay.IsVisible = false;
        _viewModel.StartPlayback();
        _surface.Focus();
    }

    private void ApplySensitivity(float value)
    {
        foreach (var entity in _viewModel.Scene.Entities)
        {
            foreach (var controller in entity.Components.OfType<FirstPersonController>())
            {
                controller.MouseSensitivity = value;
            }
        }
    }

    private sealed class PlayerSettings
    {
        public float Volume { get; set; } = 0.8f;
        public float Sensitivity { get; set; } = 0.0022f;
        public bool Fullscreen { get; set; }
    }

    private void LoadSettings()
    {
        try
        {
            var settings = File.Exists(SettingsPath)
                ? JsonSerializer.Deserialize<PlayerSettings>(File.ReadAllText(SettingsPath))
                : null;

            settings ??= new PlayerSettings();
            Audio.MasterVolume = settings.Volume;

            if (_volumeSlider is not null) _volumeSlider.Value = settings.Volume;
            if (_sensitivitySlider is not null) _sensitivitySlider.Value = settings.Sensitivity;
            if (_fullscreenCheck is not null) _fullscreenCheck.IsChecked = settings.Fullscreen;
            ApplySensitivity(settings.Sensitivity);

            if (settings.Fullscreen)
            {
                WindowState = WindowState.FullScreen;
            }
        }
        catch
        {
            // Configuración corrupta: se usan los valores por defecto.
        }
    }

    private void SaveSettings()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(SettingsPath) ?? ".");
            var settings = new PlayerSettings
            {
                Volume = _volumeSlider is null ? Audio.MasterVolume : (float)_volumeSlider.Value,
                Sensitivity = _sensitivitySlider is null ? 0.0022f : (float)_sensitivitySlider.Value,
                Fullscreen = _fullscreenCheck?.IsChecked == true,
            };
            File.WriteAllText(SettingsPath, JsonSerializer.Serialize(settings));
        }
        catch
        {
            // No es crítico si no se puede guardar la configuración.
        }
    }
    private void OnPlayerKeyDown(object? sender, KeyEventArgs e)
    {
        if (e.Key == Key.F11)
        {
            WindowState = WindowState == WindowState.FullScreen ? WindowState.Normal : WindowState.FullScreen;
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape)
        {
            SetPaused(!_paused);
            e.Handled = true;
            return;
        }

        if (_paused)
        {
            e.Handled = true;
            return;
        }

        if (_started && KeyMapping.ToKeyCode(e.Key) is { } code)
        {
            InputState.SetKey(code, true);
            e.Handled = true;
        }
    }

    private void OnPlayerPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_started && !_paused)
        {
            _surface.Viewport.UpdatePointerInput(e.GetPosition(_surface));
        }
    }

    private void OnPlayerPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (!_started || _paused)
        {
            return;
        }

        var point = e.GetCurrentPoint(_surface);
        _surface.Viewport.UpdatePointerInput(point.Position);
        _surface.Viewport.UpdateMouseButton(0, point.Properties.IsLeftButtonPressed);
        _surface.Viewport.UpdateMouseButton(1, point.Properties.IsRightButtonPressed);
        _surface.Viewport.UpdateMouseButton(2, point.Properties.IsMiddleButtonPressed);
    }

    private void OnPlayerPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        if (_started && !_paused)
        {
            _surface.Viewport.UpdateMouseButton(0, false);
            _surface.Viewport.UpdateMouseButton(1, false);
            _surface.Viewport.UpdateMouseButton(2, false);
        }
    }

    private void OnPlayerKeyUp(object? sender, KeyEventArgs e)
    {
        if (_started && KeyMapping.ToKeyCode(e.Key) is { } code)
        {
            InputState.SetKey(code, false);
            e.Handled = true;
        }
    }
}

/// <summary>Ventana de error si el juego no está bien empaquetado.</summary>
public sealed class ErrorWindow : Window
{
    public ErrorWindow(string message)
    {
        Title = "Ollin — Error";
        Width = 520;
        Height = 240;
        WindowStartupLocation = WindowStartupLocation.CenterScreen;
        Background = new SolidColorBrush(Color.Parse("#151922"));

        Content = new TextBlock
        {
            Text = message,
            Margin = new Thickness(24),
            TextWrapping = TextWrapping.Wrap,
            FontSize = 14,
            Foreground = Brushes.White,
            VerticalAlignment = VerticalAlignment.Center,
        };
    }
}
