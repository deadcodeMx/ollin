using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Ollin.Core.Model.Components;

namespace Ollin.Editor.Controls;

/// <summary>Vista previa de timeline: tracks, curvas y keyframes.</summary>
public sealed class AnimationTimelinePreview : Control
{
    private AnimationTimeline? _clip;

    public AnimationTimeline? Clip
    {
        get => _clip;
        set
        {
            _clip = value;
            InvalidateVisual();
        }
    }

    public override void Render(DrawingContext context)
    {
        base.Render(context);
        var bounds = new Rect(Bounds.Size);
        context.FillRectangle(new SolidColorBrush(Color.Parse("#0B0F18")), bounds);

        if (_clip is null || _clip.Tracks.Count == 0)
        {
            DrawText(context, "Sin clips para previsualizar", new Point(12, 12), Color.Parse("#93A4BC"));
            return;
        }

        var left = 10d;
        var top = 18d;
        var width = Math.Max(20, bounds.Width - 20);
        var height = Math.Max(20, bounds.Height - 28);
        var duration = Math.Max(0.01f, _clip.Duration);
        var rowHeight = Math.Max(18d, height / _clip.Tracks.Count);
        var gridPen = new Pen(new SolidColorBrush(Color.Parse("#263044")), 1);
        for (var i = 0; i <= 10; i++)
        {
            var x = left + width * i / 10d;
            context.DrawLine(gridPen, new Point(x, top), new Point(x, top + height));
        }

        for (var trackIndex = 0; trackIndex < _clip.Tracks.Count; trackIndex++)
        {
            var track = _clip.Tracks[trackIndex];
            var y = top + rowHeight * trackIndex + rowHeight / 2d;
            var pen = new Pen(new SolidColorBrush(Color.Parse("#E9A23B")), 1.5);
            var points = track.Keys.OrderBy(key => key.Time).Select(key =>
            {
                var t = Math.Clamp(key.Time / duration, 0f, 1f);
                var value = track.Property == "rotation" ? key.Number / 180f : key.Value.Y;
                var normalized = Math.Clamp(0.5f - value * 0.25f, 0f, 1f);
                return new Point(left + width * t, top + rowHeight * trackIndex + rowHeight * normalized);
            }).ToList();

            for (var i = 1; i < points.Count; i++)
                context.DrawLine(pen, points[i - 1], points[i]);
            foreach (var point in points)
            {
                context.DrawEllipse(new SolidColorBrush(Color.Parse("#F2F6FF")), null, point, 3.5, 3.5);
            }

            var label = string.IsNullOrWhiteSpace(track.Property) ? $"track {trackIndex}" : track.Property;
            DrawText(context, $"{label} ({track.Keys.Count})", new Point(12, top + rowHeight * trackIndex + 2), Color.Parse("#B8C0D0"));
        }
    }

    private static void DrawText(DrawingContext context, string text, Point origin, Color color)
    {
        var formatted = new FormattedText(text, System.Globalization.CultureInfo.InvariantCulture,
            FlowDirection.LeftToRight, new Typeface("Segoe UI"), 10, new SolidColorBrush(color));
        context.DrawText(formatted, origin);
    }
}
