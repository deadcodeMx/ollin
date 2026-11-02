using System.Text.Json;
using Ollin.Core.Scenes;

namespace Ollin.Core.Saves;

/// <summary>Gestión de las tres ranuras de guardado y el autoguardado.</summary>
public sealed class SaveSlotService
{
    public SaveSlotService(string? rootPath = null)
    {
        RootPath = string.IsNullOrWhiteSpace(rootPath)
            ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Ollin", "Saves")
            : rootPath;
    }

    public string RootPath { get; }

    public string GetPath(string gameName, int slot, bool autosave = false)
    {
        Directory.CreateDirectory(RootPath);
        var safe = string.Concat((gameName ?? string.Empty).Where(char.IsLetterOrDigit));

        if (safe.Length == 0)
        {
            safe = "juego";
        }

        slot = System.Math.Clamp(slot, 1, 3);
        return Path.Combine(RootPath, $"{safe}-slot{slot}{(autosave ? "-auto" : string.Empty)}.json");
    }

    public void Save(string gameName, int slot, Scene scene, GameState state, string? scenePath = null, bool autosave = false)
    {
        SaveService.Save(scene, GetPath(gameName, slot, autosave), scenePath, autosave ? "Autoguardado" : "Partida", state);
    }

    public GameSave? TryLoad(string gameName, int slot, bool autosave = false)
    {
        var path = GetPath(gameName, slot, autosave);
        return File.Exists(path) ? SaveService.Load(path) : null;
    }

    public bool Exists(string gameName, int slot, bool autosave = false) =>
        File.Exists(GetPath(gameName, slot, autosave));
}

/// <summary>Reloj de autoguardado independiente de la UI, fácil de probar.</summary>
public sealed class AutosaveScheduler
{
    public AutosaveScheduler(double intervalSeconds = 45d)
    {
        IntervalSeconds = System.Math.Max(1d, intervalSeconds);
    }

    public double IntervalSeconds { get; set; }

    public double Elapsed { get; private set; }

    public bool Tick(double deltaSeconds)
    {
        if (deltaSeconds <= 0d)
        {
            return false;
        }

        Elapsed += deltaSeconds;

        if (Elapsed < IntervalSeconds)
        {
            return false;
        }

        Elapsed = 0d;
        return true;
    }

    public void Reset() => Elapsed = 0d;
}

/// <summary>Ajustes del jugador exportado.</summary>
public sealed class PlayerSettings
{
    public float Volume { get; set; } = 0.8f;
    public float MusicVolume { get; set; } = 1f;
    public float EffectsVolume { get; set; } = 1f;

    public float SensitivityX { get; set; } = 0.0022f;

    public float SensitivityY { get; set; } = 0.0022f;

    public bool InvertY { get; set; }

    public float Smoothing { get; set; }

    public bool Fullscreen { get; set; }

    public void Normalize()
    {
        Volume = System.Math.Clamp(Volume, 0f, 1f);
        MusicVolume = System.Math.Clamp(MusicVolume, 0f, 1f);
        EffectsVolume = System.Math.Clamp(EffectsVolume, 0f, 1f);
        SensitivityX = System.Math.Clamp(SensitivityX, 0.0004f, 0.006f);
        SensitivityY = System.Math.Clamp(SensitivityY, 0.0004f, 0.006f);
        Smoothing = System.Math.Clamp(Smoothing, 0f, 0.95f);
    }

    public static PlayerSettings Load(string path)
    {
        var settings = File.Exists(path)
            ? JsonSerializer.Deserialize<PlayerSettings>(File.ReadAllText(path)) ?? new PlayerSettings()
            : new PlayerSettings();

        settings.Normalize();
        return settings;
    }

    public void Save(string path)
    {
        Normalize();
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path)) ?? ".");
        File.WriteAllText(path, JsonSerializer.Serialize(this));
    }
}
