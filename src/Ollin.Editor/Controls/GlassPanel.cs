using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;

namespace Ollin.Editor.Controls;

/// <summary>
/// Panel glassmorphism con borde degradado violeta→magenta→naranja.
/// Dibuja relleno, hijo y borde en un solo Render; no usa blur por panel.
/// </summary>
public sealed class GlassPanel : Decorator
{
    public static readonly StyledProperty<IBrush?> BackgroundProperty =
        AvaloniaProperty.Register<GlassPanel, IBrush?>(nameof(Background), NeonPalette.GlassFill);

    public static readonly StyledProperty<IBrush?> BorderBrushProperty =
        AvaloniaProperty.Register<GlassPanel, IBrush?>(nameof(BorderBrush), NeonPalette.BorderGradient);

    public static readonly StyledProperty<Thickness> BorderThicknessProperty =
        AvaloniaProperty.Register<GlassPanel, Thickness>(nameof(BorderThickness), new Thickness(1));

    public static readonly StyledProperty<CornerRadius> CornerRadiusProperty =
        AvaloniaProperty.Register<GlassPanel, CornerRadius>(nameof(CornerRadius), new CornerRadius(12));

    public static readonly StyledProperty<Thickness> PaddingProperty =
        AvaloniaProperty.Register<GlassPanel, Thickness>(nameof(Padding), new Thickness(14));

    public static readonly StyledProperty<bool> GlowProperty =
        AvaloniaProperty.Register<GlassPanel, bool>(nameof(Glow), true);

    private IPen? _borderPen;
    private IPen? _glowPen;
    private double _cachedThickness = -1;
    private IBrush? _cachedBorder;

    static GlassPanel()
    {
        AffectsRender<GlassPanel>(BackgroundProperty, BorderBrushProperty, BorderThicknessProperty, CornerRadiusProperty, GlowProperty);
        AffectsMeasure<GlassPanel>(PaddingProperty);
        ClipToBoundsProperty.OverrideDefaultValue<GlassPanel>(true);
    }

    public IBrush? Background { get => GetValue(BackgroundProperty); set => SetValue(BackgroundProperty, value); }
    public IBrush? BorderBrush { get => GetValue(BorderBrushProperty); set => SetValue(BorderBrushProperty, value); }
    public Thickness BorderThickness { get => GetValue(BorderThicknessProperty); set => SetValue(BorderThicknessProperty, value); }
    public CornerRadius CornerRadius { get => GetValue(CornerRadiusProperty); set => SetValue(CornerRadiusProperty, value); }
    public Thickness Padding { get => GetValue(PaddingProperty); set => SetValue(PaddingProperty, value); }
    public bool Glow { get => GetValue(GlowProperty); set => SetValue(GlowProperty, value); }

    protected override Size MeasureOverride(Size availableSize)
    {
        var padding = Padding;
        var childSize = new Size(
            Math.Max(0, availableSize.Width - padding.Left - padding.Right),
            Math.Max(0, availableSize.Height - padding.Top - padding.Bottom));
        Child?.Measure(childSize);
        var desired = Child?.DesiredSize ?? default;
        return new Size(desired.Width + padding.Left + padding.Right, desired.Height + padding.Top + padding.Bottom);
    }

    protected override Size ArrangeOverride(Size finalSize)
    {
        var padding = Padding;
        Child?.Arrange(new Rect(
            padding.Left,
            padding.Top,
            Math.Max(0, finalSize.Width - padding.Left - padding.Right),
            Math.Max(0, finalSize.Height - padding.Top - padding.Bottom)));
        return finalSize;
    }

    public override void Render(DrawingContext context)
    {
        var bounds = new Rect(Bounds.Size);
        var thickness = Math.Max(0, BorderThickness.Left);
        var inset = thickness / 2;
        var radius = Math.Max(0, CornerRadius.TopLeft);
        var panel = new RoundedRect(bounds.Deflate(inset), radius, radius);

        // 1) relleno glass, debajo del contenido.
        context.DrawRectangle(Background ?? NeonPalette.GlassFill, null, panel);
        // 2) contenido del panel.
        base.Render(context);
        // 3) borde degradado encima del contenido.
        if (thickness > 0 && BorderBrush is not null)
        {
            if (_borderPen is null || Math.Abs(_cachedThickness - thickness) > 0.01 || !ReferenceEquals(_cachedBorder, BorderBrush))
            {
                _borderPen = new Pen(BorderBrush, thickness);
                _cachedThickness = thickness;
                _cachedBorder = BorderBrush;
            }
            context.DrawRectangle(null, _borderPen, panel);
        }
        // 4) brillo exterior opcional, un solo trazo extra (sin blur).
        if (Glow)
        {
            _glowPen ??= new Pen(new SolidColorBrush(Color.Parse("#4DFF2FA8")), 2);
            context.DrawRectangle(null, _glowPen, new RoundedRect(bounds.Deflate(1.5), radius + 1, radius + 1));
        }
    }
}
