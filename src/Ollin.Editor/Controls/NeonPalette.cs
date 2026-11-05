using Avalonia;
using Avalonia.Media;

namespace Ollin.Editor.Controls;

/// <summary>Paleta y pinceles cacheados del tema Obsidiana Neón.</summary>
public static class NeonPalette
{
    public static readonly Color Obsidian = Color.Parse("#0A0A0C");
    public static readonly Color Surface = Color.Parse("#0F0F14");
    public static readonly Color SurfaceRaised = Color.Parse("#15151D");
    public static readonly Color Text = Color.Parse("#F2EEF7");
    public static readonly Color TextMuted = Color.Parse("#A79FB5");
    public static readonly Color Violet = Color.Parse("#7B2CFF");
    public static readonly Color Magenta = Color.Parse("#FF2FA8");
    public static readonly Color Ember = Color.Parse("#FF6A3D");

    /// <summary>Degradado violeta → magenta → naranja, compartido por todos los paneles.</summary>
    public static readonly LinearGradientBrush BorderGradient = new()
    {
        StartPoint = new RelativePoint(0, 0, RelativeUnit.Relative),
        EndPoint = new RelativePoint(1, 1, RelativeUnit.Relative),
        GradientStops =
        {
            new GradientStop(Violet, 0),
            new GradientStop(Magenta, 0.52),
            new GradientStop(Ember, 1),
        },
    };

    /// <summary>Relleno glass: casi negro con alfa, para dejar ver el fondo.</summary>
    public static readonly IBrush GlassFill = new SolidColorBrush(Color.Parse("#B30F0F14"));
    public static readonly IBrush GlassFillStrong = new SolidColorBrush(Color.Parse("#D915151D"));
    public static readonly IBrush InnerHighlight = new SolidColorBrush(Color.Parse("#33FFFFFF"));
    public static readonly IBrush Shadow = new SolidColorBrush(Color.Parse("#66000000"));
}
