using System.Text.Json;

namespace Ollin.Core.Dialogue;

/// <summary>Archivo de diálogo (`dialogs/nombre.json`).</summary>
public sealed class DialogueFile
{
    public string Id { get; set; } = string.Empty;

    /// <summary>Hablante por defecto si una línea no lo indica.</summary>
    public string? Speaker { get; set; }

    public List<DialogueLine> Lines { get; set; } = new();
}

public sealed class DialogueLine
{
    public string? Speaker { get; set; }

    public string Text { get; set; } = string.Empty;
}

/// <summary>
/// Carga los diálogos del proyecto desde `dialogs/`. Los identificadores son el
/// nombre del archivo sin extensión (o el campo `id` si está presente).
/// </summary>
public sealed class DialogueDatabase
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        ReadCommentHandling = JsonCommentHandling.Skip,
        AllowTrailingCommas = true,
    };

    private readonly Dictionary<string, DialogueFile> _dialogues = new(StringComparer.OrdinalIgnoreCase);

    public DialogueDatabase(string root)
    {
        Root = root;
        Reload();
    }

    public string Root { get; }

    public int Count => _dialogues.Count;

    public IReadOnlyCollection<string> Ids => _dialogues.Keys;

    public void Reload()
    {
        _dialogues.Clear();
        var folder = Path.Combine(Root, "dialogs");

        if (!Directory.Exists(folder))
        {
            return;
        }

        foreach (var file in Directory.EnumerateFiles(folder, "*.json", SearchOption.AllDirectories))
        {
            try
            {
                var dialogue = JsonSerializer.Deserialize<DialogueFile>(File.ReadAllText(file), Options);
                if (dialogue is null || dialogue.Lines.Count == 0)
                {
                    continue;
                }

                var id = string.IsNullOrWhiteSpace(dialogue.Id)
                    ? Path.GetFileNameWithoutExtension(file)
                    : dialogue.Id;

                dialogue.Id = id;
                _dialogues[id] = dialogue;
            }
            catch
            {
                // Un diálogo mal formado no debe impedir cargar el resto.
            }
        }
    }

    public bool TryGet(string? id, out DialogueFile dialogue)
    {
        dialogue = null!;

        if (string.IsNullOrWhiteSpace(id))
        {
            return false;
        }

        return _dialogues.TryGetValue(id, out dialogue!);
    }

    public DialogueFile? Get(string? id) => TryGet(id, out var dialogue) ? dialogue : null;
}
