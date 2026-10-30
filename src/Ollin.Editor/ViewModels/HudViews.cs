using Avalonia.Media;

namespace Ollin.Editor.ViewModels;

/// <summary>Barra del HUD ya preparada para la interfaz.</summary>
public sealed class HudBarView
{
    public HudBarView(string name, string valueText, double fraction, IBrush brush)
    {
        Name = name;
        ValueText = valueText;
        Fraction = fraction;
        Brush = brush;
    }

    public string Name { get; }

    public string ValueText { get; }

    public double Fraction { get; }

    public IBrush Brush { get; }
}

/// <summary>Etiqueta del HUD.</summary>
public sealed class HudLabelView
{
    public HudLabelView(string name, string text)
    {
        Name = name;
        Text = text;
    }

    public string Name { get; }

    public string Text { get; }

    public string Display => string.IsNullOrWhiteSpace(Name) ? Text : $"{Name}: {Text}";
}

/// <summary>Mensaje temporal del HUD.</summary>
public sealed class HudMessageView
{
    public HudMessageView(string text) => Text = text;

    public string Text { get; }
}
