using Avalonia.Media;
using AvaloniaEdit.CodeCompletion;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;

namespace Ollin.Editor.Views;

/// <summary>Entrada del autocompletado que se muestra en el editor de scripts.</summary>
internal sealed class OllinCompletionData : ICompletionData
{
    public OllinCompletionData(string text, string description)
    {
        Text = text;
        Description = description;
    }

    public IImage? Image => null;

    public string Text { get; }

    public string Label => Text;

    public string InsertText => Text;

    public object Content => Text;

    public object? Description { get; }

    public double Priority => 0;

    public void Complete(TextArea textArea, ISegment completionSegment, EventArgs insertionRequestEventArgs)
    {
        textArea.Document.Replace(completionSegment, Text);
    }
}
