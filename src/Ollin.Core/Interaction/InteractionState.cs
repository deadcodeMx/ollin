namespace Ollin.Core.Interaction;

/// <summary>
/// Estado de la interacción del frame actual: qué aviso mostrar y sobre qué
/// entidad. El runtime lo actualiza y el editor lo pinta en el viewport.
/// </summary>
public static class InteractionState
{
    public static string? Prompt { get; private set; }

    public static Guid? TargetId { get; private set; }

    public static bool HasPrompt => !string.IsNullOrWhiteSpace(Prompt);

    public static void Set(string? prompt, Guid? targetId)
    {
        Prompt = prompt;
        TargetId = targetId;
    }

    public static void Reset() => Set(null, null);
}
