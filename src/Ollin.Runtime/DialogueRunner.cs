using Ollin.Core.Dialogue;

namespace Ollin.Runtime;

/// <summary>
/// Reproduce un diálogo línea a línea. No dibuja nada: el editor lee
/// <see cref="Speaker"/> y <see cref="Text"/> para mostrarlos.
/// </summary>
public sealed class DialogueRunner
{
    public bool IsActive { get; private set; }

    public DialogueFile? Dialogue { get; private set; }

    public int Index { get; private set; }

    public int LineCount => Dialogue?.Lines.Count ?? 0;

    public DialogueLine? Current =>
        IsActive && Dialogue is not null && Index >= 0 && Index < Dialogue.Lines.Count
            ? Dialogue.Lines[Index]
            : null;

    public string Speaker => Current?.Speaker ?? Dialogue?.Speaker ?? string.Empty;

    public string Text => Current?.Text ?? string.Empty;

    /// <summary>Se dispara al terminar la última línea.</summary>
    public event Action? Finished;

    public bool Start(DialogueFile dialogue)
    {
        Dialogue = dialogue;
        Index = 0;
        IsActive = dialogue.Lines.Count > 0;
        return IsActive;
    }

    public void Advance()
    {
        if (!IsActive)
        {
            return;
        }

        Index++;

        if (Dialogue is null || Index >= Dialogue.Lines.Count)
        {
            IsActive = false;
            Dialogue = null;
            Index = 0;
            Finished?.Invoke();
        }
    }

    public void Stop()
    {
        IsActive = false;
        Dialogue = null;
        Index = 0;
    }
}
