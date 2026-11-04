namespace Ollin.Core.Dialogue;

/// <summary>
/// Puente entre el runtime y la API de scripting para los diálogos. El runtime
/// registra los manejadores al reproducir y los limpia al detener.
/// </summary>
public static class DialogueHost
{
    public static Func<string, bool>? PlayHandler { get; set; }

    public static Func<bool>? IsActiveHandler { get; set; }

    public static Action? AdvanceHandler { get; set; }

    public static bool Play(string id) => PlayHandler?.Invoke(id) ?? false;

    public static bool IsActive => IsActiveHandler?.Invoke() ?? false;

    public static void Advance() => AdvanceHandler?.Invoke();

    public static void Clear()
    {
        PlayHandler = null;
        IsActiveHandler = null;
        AdvanceHandler = null;
    }
}
