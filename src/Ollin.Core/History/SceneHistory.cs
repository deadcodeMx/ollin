using Ollin.Core.Serialization;
using Ollin.Core.Scenes;

namespace Ollin.Core.History;

/// <summary>Una entrada del historial: instantánea serializada y etiqueta legible.</summary>
public sealed record HistoryEntry(string Label, string Json);

/// <summary>
/// Historial de estados de una escena basado en instantáneas JSON.
/// <para>
/// <see cref="Record"/> guarda el estado actual y fija el nuevo, de modo que
/// <see cref="Undo"/> devuelve exactamente lo que había antes de la edición.
/// Las ediciones continuas (arrastrar un número) se agrupan con
/// <see cref="ReplaceCurrent"/>.
/// </para>
/// </summary>
public sealed class SceneHistory
{
    private readonly List<HistoryEntry> _undo = new();
    private readonly List<HistoryEntry> _redo = new();

    public SceneHistory(int limit = 60)
    {
        Limit = System.Math.Max(1, limit);
    }

    public int Limit { get; }

    public HistoryEntry? Current { get; private set; }

    public bool CanUndo => _undo.Count > 0;

    public bool CanRedo => _redo.Count > 0;

    public string? UndoLabel => _undo.Count > 0 ? _undo[^1].Label : null;

    public string? RedoLabel => _redo.Count > 0 ? _redo[^1].Label : null;

    public int UndoCount => _undo.Count;

    public int RedoCount => _redo.Count;

    /// <summary>Fija el estado inicial sin crear pasos de deshacer.</summary>
    public void Initialize(Scene scene)
    {
        Current = new HistoryEntry("Inicio", SceneSerializer.Serialize(scene));
        _undo.Clear();
        _redo.Clear();
    }

    /// <summary>Registra una edición ya aplicada a la escena.</summary>
    public void Record(Scene scene, string label) => Record(SceneSerializer.Serialize(scene), label);

    public void Record(string json, string label)
    {
        if (Current is not null)
        {
            _undo.Add(Current);
            if (_undo.Count > Limit)
            {
                _undo.RemoveAt(0);
            }
        }

        Current = new HistoryEntry(label, json);
        _redo.Clear();
    }

    /// <summary>Actualiza el estado actual sin crear un paso nuevo (coalescencia).</summary>
    public void ReplaceCurrent(string json)
    {
        if (Current is null)
        {
            Current = new HistoryEntry("Inicio", json);
            return;
        }

        Current = Current with { Json = json };
    }

    /// <summary>Deshace la última edición y devuelve el estado anterior.</summary>
    public HistoryEntry? Undo()
    {
        if (_undo.Count == 0 || Current is null)
        {
            return null;
        }

        var previous = _undo[^1];
        _undo.RemoveAt(_undo.Count - 1);
        _redo.Add(Current);
        Current = previous;
        return Current;
    }

    public HistoryEntry? Redo()
    {
        if (_redo.Count == 0 || Current is null)
        {
            return null;
        }

        var next = _redo[^1];
        _redo.RemoveAt(_redo.Count - 1);
        _undo.Add(Current);
        Current = next;
        return Current;
    }

    public void Clear()
    {
        _undo.Clear();
        _redo.Clear();
        Current = null;
    }
}
