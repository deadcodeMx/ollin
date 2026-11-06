using Ollin.Core.Model.Components;
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
    private string _inventorySignature = string.Empty;
    private bool _inventoryOpen;

    public ObservableCollection<InventorySlotView> InventoryItems { get; } = new();
    public ObservableCollection<EquipmentSlotView> EquipmentItems { get; } = new();
    public ObservableCollection<UiElementView> UiElements { get; } = new();
    private string _uiSignature = string.Empty;
    public bool IsUiVisible { get; private set; }
    public string? FocusedUiId { get; private set; }

    public bool IsInventoryVisible => _inventoryOpen && IsPlaying;

    public string ProfilerText => $"{Ollin.Core.Diagnostics.Profiler.Fps:0} FPS · {Ollin.Core.Diagnostics.Profiler.FrameMs:0.0} ms · {Ollin.Core.Diagnostics.Profiler.DrawCalls} draws · {Scene.Entities.Count} entidades";

    public void ToggleInventory()
    {
        if (!IsPlaying || IsGameOverVisible) return;
        _inventoryOpen = !_inventoryOpen;
        OnPropertyChanged(nameof(IsInventoryVisible));
    }

    public void CloseInventory()
    {
        if (!_inventoryOpen) return;
        _inventoryOpen = false;
        OnPropertyChanged(nameof(IsInventoryVisible));
    }
    private bool _isHudVisible;
    private bool _isPauseMenuVisible;
    private bool _isGameOverVisible;
    private string _gameOverText = string.Empty;
    private double _masterVolume = Core.Audio.Audio.MasterVolume;
    private double _transitionOpacity;
    private IBrush _transitionBrush = new SolidColorBrush(Colors.Black);

    public double TransitionOpacity
    {
        get => _transitionOpacity;
        private set { if (System.Math.Abs(_transitionOpacity - value) < 0.001d) return; _transitionOpacity = value; OnPropertyChanged(); }
    }

    public IBrush TransitionBrush
    {
        get => _transitionBrush;
        private set { _transitionBrush = value; OnPropertyChanged(); }
    }

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
        OnPropertyChanged(nameof(ProfilerText));
        var hud = _runtime.Hud;
        TransitionOpacity = hud.TransitionAlpha;
        TransitionBrush = ToBrush(hud.TransitionColor);
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

    public void FocusUi(int direction)
    {
        if (!IsUiVisible) return;
        var focusable = UiElements.Where(view => view.Focusable).ToList();
        if (focusable.Count == 0) return;
        var index = focusable.FindIndex(view => view.IsFocused);
        index = index < 0 ? 0 : (index + direction + focusable.Count) % focusable.Count;
        FocusedUiId = focusable[index].Id;
        foreach (var view in UiElements) view.IsFocused = view.Id == FocusedUiId;
        OnPropertyChanged(nameof(UiElements));
    }

    public void ActivateFocusedUi()
    {
        var focused = UiElements.FirstOrDefault(view => view.IsFocused && view.Focusable);
        if (focused is not null) InvokeUiAction(focused.Action);
    }

    private void FocusFirstUi()
    {
        var first = UiElements.FirstOrDefault(view => view.Focusable);
        if (first is null) return;
        FocusedUiId = first.Id;
        first.IsFocused = true;
    }

    public void InvokeUiAction(string action)
    {
        _runtime.GameState.SetFlag($"ui.{action}", true);
        Ollin.Scripting.ScriptSignals.Emit($"ui.{action}", null);
        StatusMessage = $"UI: {action}";
    }

    public void RefreshUi()
    {
        var canvas = _runtime.PlayerUi;
        var flattened = canvas?.Flatten() ?? new List<UiElement>();
        var signature = canvas is null ? "none" : string.Join("|", flattened.Select(element =>
            $"{element.Id}:{element.Kind}:{element.Text}:{element.X}:{element.Y}:{element.Visible}:{element.Value}:{element.Max}:{element.ParentId}"));
        IsUiVisible = canvas is { Visible: true } && IsPlaying;
        if (signature == _uiSignature) return;
        _uiSignature = signature;
        UiElements.Clear();
        if (canvas is not null)
            foreach (var element in flattened)
            {
                var view = new UiElementView(element, InvokeUiAction, canvas.Theme);
                view.IsFocused = view.Focusable && view.Id == FocusedUiId;
                UiElements.Add(view);
            }
            if (UiElements.All(view => !view.IsFocused)) FocusFirstUi();
        OnPropertyChanged(nameof(IsUiVisible));
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

    private SaveSlotService Slots => new(Path.Combine(Project?.RootPath ?? ProjectRoot, "saves"));

    public bool HasSaveSlot(int slot) => IsPlaying && Slots.Exists(Project?.Name ?? "Ollin", slot);

    /// <summary>Guarda la partida actual (posición, componentes y estado).</summary>
    public void SaveGame() => SaveGameSlot(1);

    public void SaveGameSlot(int slot)
    {
        if (!IsPlaying)
        {
            StatusMessage = "Reproduce la escena para guardar la partida";
            return;
        }

        try
        {
            var game = Project?.Name ?? "Ollin";
            Slots.Save(game, slot, Scene, _runtime.GameState, ScenePath);
            StatusMessage = $"Partida guardada en ranura {slot}";
            Log.Info($"Partida guardada en ranura {slot}", "Guardado");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo guardar la partida: {exception.Message}", "Guardado");
            StatusMessage = "Error al guardar la partida";
        }
    }

    public void LoadGameSlot(int slot)
    {
        try
        {
            var game = Project?.Name ?? "Ollin";
            var save = Slots.TryLoad(game, slot);
            if (save is null)
            {
                StatusMessage = $"La ranura {slot} está vacía";
                return;
            }

            if (IsPlaying) StopPlay();
            LoadScene(save.Scene!, save.ScenePath);
            _runtime.GameState.CopyFrom(save.State);
            StartPlay(keepState: true);
            StatusMessage = $"Partida cargada de la ranura {slot}";
            Log.Info($"Partida cargada de ranura {slot}", "Guardado");
        }
        catch (Exception exception)
        {
            Log.Error($"No se pudo cargar la partida: {exception.Message}", "Guardado");
            StatusMessage = "Error al cargar la partida";
        }
    }


    /// <summary>Carga la última partida guardada y continúa jugando.</summary>
    public void LoadGame() => LoadGameSlot(1);

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
        TransitionOpacity = 0d;
        InventoryItems.Clear();
        EquipmentItems.Clear();
        _inventoryOpen = false;
        _inventorySignature = string.Empty;
        IsPauseMenuVisible = false;
        IsGameOverVisible = false;
        GameOverText = string.Empty;
    }
}
