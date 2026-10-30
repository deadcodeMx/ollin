using Ollin.Core.Dialogue;

namespace Ollin.Scripting;

/// <summary>Diálogos para scripts: iniciar, consultar y avanzar.</summary>
public static class Dialogue
{
    public static bool IsActive => DialogueHost.IsActive;

    /// <summary>Inicia el diálogo `dialogs/<id>.json`.</summary>
    public static bool Play(string id) => DialogueHost.Play(id);

    public static void Advance() => DialogueHost.Advance();
}