using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Media;

namespace Ollin.Editor.Controls;

/// <summary>TextBlock con marcado simple: [b], [i], [color=#RRGGBB].</summary>
public sealed class RichTextBlock : TextBlock
{
    public static readonly StyledProperty<string?> MarkupProperty =
        AvaloniaProperty.Register<RichTextBlock, string?>(nameof(Markup));

    public string? Markup
    {
        get => GetValue(MarkupProperty);
        set => SetValue(MarkupProperty, value);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == MarkupProperty) Rebuild(change.NewValue as string);
    }

    private void Rebuild(string? markup)
    {
        Inlines?.Clear();
        if (string.IsNullOrEmpty(markup)) return;
        var bold = false;
        var italic = false;
        var color = (IBrush?)null;
        var index = 0;
        foreach (System.Text.RegularExpressions.Match match in System.Text.RegularExpressions.Regex.Matches(markup, @"\[(/?)(b|i|color)(?:=([^\]]+))?\]"))
        {
            if (match.Index > index) AddRun(markup[index..match.Index], bold, italic, color);
            var closing = match.Groups[1].Value == "/";
            var tag = match.Groups[2].Value;
            if (tag == "b") bold = !closing;
            else if (tag == "i") italic = !closing;
            else if (tag == "color")
            {
                if (closing) color = null;
                else if (Color.TryParse(match.Groups[3].Value, out var parsed)) color = new SolidColorBrush(parsed);
            }
            index = match.Index + match.Length;
        }
        if (index < markup.Length) AddRun(markup[index..], bold, italic, color);
    }

    private void AddRun(string text, bool bold, bool italic, IBrush? color)
    {
        if (string.IsNullOrEmpty(text)) return;
        Inline inline = new Run(text);
        if (bold || italic)
        {
            var span = new Span();
            if (bold) span.FontWeight = FontWeight.Bold;
            if (italic) span.FontStyle = FontStyle.Italic;
            span.Inlines.Add(new Run(text));
            inline = span;
        }
        if (color is not null) inline.Foreground = color;
        Inlines!.Add(inline);
    }
}
