using System.Collections.ObjectModel;
using System.Numerics;
using Avalonia.Media;
using Ollin.Core.Diagnostics;
using Ollin.Core.Hud;
using Ollin.Core.Saves;

namespace Ollin.Editor.ViewModels;

/// <summary>HUD de la partida, menú de pausa y pantalla de fin de partida.</summary>
public sealed partial class MainViewModel
{
    private string _hudSignature = string.Empty;
    private bool _isHudVisible;
    private bool _isPauseMenuVisible;
    private bool _isGameOverVisible;
    private string _gameOverText = string.Empty;
    private double _masterVolume = Core.Audio.Audio.MasterVolume;

    public ObservableCollection<HudBarView> HudBars { get; } = new();

    public ObservableCollection<HudLabelView> HudLabels { get; } = new();

    public ObservableCollection<HudMessageView> HudMessages { get; } = new();

    public bool IsHudVisible
    {
        get => _isHudVisible;
        private set
        {
            if (_isHudVisible == value) return;
            _isHudVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsPauseMenuVisible
    {
        get => _isPauseMenuVisible;
        private set
        {
            if (_isPauseMenuVisible == value) return;
            _isPauseMenuVisible = value;
            OnPropertyChanged();
        }
    }

    public bool IsGameOverVisible
    {
        get => _isGameOverVisible;
        private set
        {
            if (_isGameOverVisible == value) return;
            _isGameOverVisible = value;
            OnPropertyChanged();
        }
    }

    public string GameOverText
    {
        get => _gameOverText;
        private set
        {
            if (_gameOverText == value) return;
            _gameOverText = value;
            OnPropertyChanged();
        }
    }

    public double MasterVolume
    {
        get => _masterVolume;
        set
        {
            var clamped = System.Math.Clamp(value, 0d, 1d);

            if (System.Math.Abs(_masterVolume - clamped) < 0.0001d)
            {
                return;
            }

            _masterVolume = clamped;
            Core.Audio.Audio.MasterVolume = (float)clamped;
            OnPropertyChanged();
        }
    }

    /// <summary>Copia el HUD del runtime a las colecciones de la interfaz.</summary>
    private void RefreshHud()
    {
        var hud = _runtime.Hud;
        var signature = BuildHudSignature(hud);

        if (signature != _hudSignature)
        {
            _hudSignature = signature;

            HudBars.Clear();
            foreach (var bar in hud.Bars)
            {
                HudBars.Add(new HudBarView(
                    bar.Name,
                    $"{System.Math.Round(bar.Value)} / {System.Math.Round(bar.Max)}",
                    bar.Fraction,
                    ToBrush(bar.Color)));
            }

            HudLabels.Clear();
            foreach (var label in hud.Labels)
            {
                HudLabels.Add(new HudLabelView(label.Name, label.Text));
            }

            HudMessages.Clear();
            foreach (var message in hud.Messages)
            {
                HudMessages.Add(new HudMessageView(message.Text));
            }

            IsHudVisible = IsPlaying &&
                           (HudBars.Count > 0 || HudLabels.Count > 0 || HudMessages.Count > 0);
        }

        if (hud.IsGameOver && !IsGameOverVisible)
        {
            IsGameOverVisible = true;
            IsPauseMenuVisible = false;
            GameOverText = hud.GameOverText ?? "Fin del juego";

            if (!_runtime.IsPaused)
            {
                _runtime.TogglePause();
            }

            NotifyPlayState();
        }
    }

    private static string BuildHudSignature(HudModel hud)
    {
        var parts = new List<string>(hud.Bars.Count + hud.Labels.Count + hud.Messages.Count);

        foreach (var bar in hud.Bars)
        {
            parts.Add($"{bar.Name}:{bar.Value}:{bar.Max}");
        }

        foreach (var label in hud.Labels)
        {
            parts.Add($"{label.Name}:{label.Text}");
        }

        foreach (var message in hud.Messages)
        {
            parts.Add(message.Text);
        }

        parts.Add(hud.IsGameOver ? "go" : "-");
        return string.Join("|", parts);
    }

    private static IBrush ToBrush(Vector4 color)
    {
        return new SolidColorBrush(Color.FromArgb(
            (byte)(System.Math.Clamp(color.W, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(color.X, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(color.Y, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(color.Z, 0f, 1f) * 255)));
    }

    /// <summary>Abre o cierra el menú de pausa (tecla Escape en reproducción).</summary>
    public void TogglePauseMenu()
    {
        if (!IsPlaying || IsGameOverVisible)
        {
            return;
        }

        IsPauseMenuVisible = !IsPauseMenuVisible;

        if (IsPauseMenuVisible && !_runtime.IsPaused)
        {
            _runtime.TogglePause();
        }
        else if (!IsPauseMenuVisible && _runtime.IsPaused)
        {
            _runtime.TogglePause();
        }

        NotifyPlayState();
    }

    /// <summary>Continúa la partida desde el menú de pausa.</summary>
    public void ResumeFromMenu()
    {
        if (IsPauseMenuVisible)
        {
            TogglePauseMenu();
        }
    }

    /// <summary>Reinicia la escena actual desde el principio.</summary>
    public void RestartPlay()
    {
        if (!IsPlaying)
        {
            return;
        }

        StopPlay();
        StartPlay();
    }

    /// <summary>Detiene la reproducción desde un menú.</summary>
    public void StopFromMenu() => StopPlay();

    /// <summary>Guarda la partida actual (posición, componentes y estado).</summary>
    public void SaveGame()
    {
        if (!IsPlaying)
        {
            StatusMessage = "Reproduce la escena para guardar la partida";
            return;
        }

        try
        {
            var root = Project?.RootPath ?? ProjectRoot;
            var path = SaveService.DefaultPath(root);
            SaveService.Save(Scene, path, ScenePath, "Partida", _runtime.GameState);
            StatusMessage = "Partida guardada";
            Log.Info($"Partida guardada: {path}", "Guardado");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar la partida: {exception.Message}", "Guardado");
            StatusMessage = "Error al guardar la partida";
        }
    }

    /// <summary>Carga la última partida guardada y continúa jugando.</summary>
    public void LoadGame()
    {
        try
        {
            var root = Project?.RootPath ?? ProjectRoot;
            var path = SaveService.DefaultPath(root);

            if (!File.Exists(path))
            {
                StatusMessage = "No hay ninguna partida guardada";
                return;
            }

            var save = SaveService.Load(path);

            if (IsPlaying)
            {
                StopPlay();
            }

            LoadScene(save.Scene!, save.ScenePath);
            _runtime.GameState.CopyFrom(save.State);
            StartPlay(keepState: true);
            StatusMessage = "Partida cargada";
            Log.Info($"Partida cargada: {path}", "Guardado");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo cargar la partida: {exception.Message}", "Guardado");
            StatusMessage = "Error al cargar la partida";
        }
    }

    /// <summary>Guarda la partida en una ruta concreta (juego exportado).</summary>
    public void SaveGameTo(string path)
    {
        if (!IsPlaying)
        {
            StatusMessage = "Reproduce la escena para guardar la partida";
            return;
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path) ?? ".");
            SaveService.Save(Scene, path, ScenePath, "Partida", _runtime.GameState);
            StatusMessage = "Partida guardada";
            Log.Info($"Partida guardada: {path}", "Guardado");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar la partida: {exception.Message}", "Guardado");
            StatusMessage = "Error al guardar la partida";
        }
    }

    /// <summary>Carga una partida desde una ruta concreta y continúa jugando.</summary>
    public void LoadGameFrom(string path)
    {
        try
        {
            if (!File.Exists(path))
            {
                StatusMessage = "No hay ninguna partida guardada";
                return;
            }

            var save = SaveService.Load(path);

            if (IsPlaying)
            {
                StopPlay();
            }

            LoadScene(save.Scene!, save.ScenePath);
            _runtime.GameState.CopyFrom(save.State);
            StartPlay(keepState: true);
            StatusMessage = "Partida cargada";
            Log.Info($"Partida cargada: {path}", "Guardado");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo cargar la partida: {exception.Message}", "Guardado");
            StatusMessage = "Error al cargar la partida";
        }
    }

    private void ResetHudState()
    {
        _hudSignature = string.Empty;
        HudBars.Clear();
        HudLabels.Clear();
        HudMessages.Clear();
        IsHudVisible = false;
        IsPauseMenuVisible = false;
        IsGameOverVisible = false;
        GameOverText = string.Empty;
    }
}
