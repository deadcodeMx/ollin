using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;

namespace Ollin.Editor.Controls;

/// <summary>
/// Superficie de entrada del viewport. Un control sin fondo no participa en el
/// hit-test de Avalonia, así que este borde transparente es quien recibe los
/// eventos de puntero y se los pasa al <see cref="ViewportControl"/>.
/// </summary>
public sealed class ViewportSurface : Border
{
    private bool _panning;
    private Point _lastPointer;
    private IPointer? _capturedPointer;

    public ViewportSurface()
    {
        Background = Brushes.Transparent;
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Arrow);
        Viewport = new ViewportControl { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
        Child = Viewport;
    }

    public ViewportControl Viewport { get; }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetCurrentPoint(this);
        var position = point.Position;
        var wantsPan = point.Properties.IsMiddleButtonPressed ||
                       (point.Properties.IsLeftButtonPressed && e.KeyModifiers.HasFlag(KeyModifiers.Alt));

        if (wantsPan)
        {
            _panning = true;
            _lastPointer = position;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(this);
            e.Handled = true;
            return;
        }

        if (point.Properties.IsLeftButtonPressed)
        {
            Viewport.SelectAt(position);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);
        if (!_panning)
        {
            return;
        }

        var position = e.GetPosition(this);
        var delta = position - _lastPointer;
        _lastPointer = position;
        Viewport.PanByDelta(delta.X, delta.Y);
        e.Handled = true;
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        base.OnPointerReleased(e);
        if (!_panning)
        {
            return;
        }

        _panning = false;
        _capturedPointer?.Capture(null);
        _capturedPointer = null;
        e.Handled = true;
    }

    protected override void OnPointerWheelChanged(PointerWheelEventArgs e)
    {
        base.OnPointerWheelChanged(e);
        Viewport.ZoomAtPoint(e.GetPosition(this), e.Delta.Y);
        e.Handled = true;
    }
}
