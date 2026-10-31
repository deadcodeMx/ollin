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
    private bool _painting;
    private bool _gizmoDragging;
    private bool _gizmo3DDragging;
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

        if (Viewport.ViewModel is { IsPlaying: true })
        {
            Viewport.UpdatePointerInput(position);
            Viewport.UpdateMouseButton(0, point.Properties.IsLeftButtonPressed);
            Viewport.UpdateMouseButton(1, point.Properties.IsRightButtonPressed);
            Viewport.UpdateMouseButton(2, point.Properties.IsMiddleButtonPressed);
            e.Handled = true;
            return;
        }

        if (Viewport.ViewModel is { IsPaintingTiles: true })
        {
            _painting = true;
            _capturedPointer = e.Pointer;
            e.Pointer.Capture(this);
            Viewport.PaintAt(position);
            e.Handled = true;
            return;
        }

        if (Viewport.ViewModel?.ActiveCamera3D is not null)
        {
            if (point.Properties.IsLeftButtonPressed)
            {
                // Primero intenta arrastrar el gizmo de la selección actual; si no,
                // selecciona la entidad bajo el puntero.
                if (Viewport.Begin3DDrag(position))
                {
                    _gizmo3DDragging = true;
                    _capturedPointer = e.Pointer;
                    e.Pointer.Capture(this);
                }
                else
                {
                    Viewport.Select3DAt(position);
                }

                e.Handled = true;
            }

            return;
        }

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
            var handle = Viewport.HitGizmo(position);
            if (handle != GizmoHandle.None && Viewport.BeginGizmo(handle, position))
            {
                _gizmoDragging = true;
                _capturedPointer = e.Pointer;
                e.Pointer.Capture(this);
                e.Handled = true;
                return;
            }

            Viewport.SelectAt(position);
            e.Handled = true;
        }
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (Viewport.ViewModel is { IsPlaying: true })
        {
            var current = e.GetCurrentPoint(this);
            Viewport.UpdatePointerInput(current.Position);
            Viewport.UpdateMouseButton(0, current.Properties.IsLeftButtonPressed);
            Viewport.UpdateMouseButton(1, current.Properties.IsRightButtonPressed);
            Viewport.UpdateMouseButton(2, current.Properties.IsMiddleButtonPressed);
            e.Handled = true;
            return;
        }

        if (_gizmo3DDragging)
        {
            Viewport.Update3DDrag(e.GetPosition(this), e.KeyModifiers.HasFlag(KeyModifiers.Control));
            e.Handled = true;
            return;
        }

        if (_gizmoDragging)
        {
            var snap = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            Viewport.UpdateGizmo(e.GetPosition(this), snap);
            e.Handled = true;
            return;
        }

        if (_painting)
        {
            Viewport.PaintAt(e.GetPosition(this));
            e.Handled = true;
            return;
        }

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

        if (Viewport.ViewModel is { IsPlaying: true })
        {
            Viewport.UpdateMouseButton(0, false);
            Viewport.UpdateMouseButton(1, false);
            Viewport.UpdateMouseButton(2, false);
            e.Handled = true;
            return;
        }

        if (_painting)
        {
            _painting = false;
            _capturedPointer?.Capture(null);
            _capturedPointer = null;
            e.Handled = true;
            return;
        }

        if (_gizmoDragging)
        {
            _gizmoDragging = false;
            Viewport.EndGizmo();
            _capturedPointer?.Capture(null);
            _capturedPointer = null;
            e.Handled = true;
            return;
        }

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

        if (Viewport.ViewModel is { IsPlaying: true })
        {
            e.Handled = true;
            return;
        }

        Viewport.ZoomAtPoint(e.GetPosition(this), e.Delta.Y);
        e.Handled = true;
    }
}
