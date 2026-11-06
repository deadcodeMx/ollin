using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media;
using Avalonia.Controls.Shapes;

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
    private Point _lastPointer;
    private IPointer? _capturedPointer;
    private readonly Canvas _overlay = new() { IsHitTestVisible = false };
    private Rectangle? _marquee;
    private Point _marqueeStart;
    private bool _marqueeAdditive;
    private bool _marqueeSelecting;

    public ViewportSurface()
    {
        Background = Brushes.Transparent;
        ClipToBounds = true;
        Focusable = true;
        Cursor = new Cursor(StandardCursorType.Arrow);
        Viewport = new ViewportControl { HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Stretch };
        var grid = new Grid();
        grid.Children.Add(Viewport);
        grid.Children.Add(_overlay);
        Child = grid;
    }

    public ViewportControl Viewport { get; }

    protected override void OnPointerPressed(PointerPressedEventArgs e)
    {
        base.OnPointerPressed(e);
        Focus();

        var point = e.GetCurrentPoint(this);
        var position = point.Position;

        if (Viewport.ViewModel is { IsPlaying: true } && Viewport.GameInputEnabled)
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
            if (Viewport.ViewModel?.TileFillMode == true)
            {
                Viewport.FillAt(position);
            }
            else
            {
                Viewport.PaintAt(position);
            }

            e.Handled = true;
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

            var additive = e.KeyModifiers.HasFlag(KeyModifiers.Control);
            if (Viewport.HitTestEntity(position, out _))
            {
                Viewport.SelectAt(position, additive);
            }
            else
            {
                BeginMarquee(position, additive);
                _capturedPointer = e.Pointer;
                e.Pointer.Capture(this);
            }

            e.Handled = true;
        }
    }

    private void BeginMarquee(Point position, bool additive)
    {
        _marqueeSelecting = true;
        _marqueeAdditive = additive;
        _marqueeStart = position;
        _marquee = new Rectangle
        {
            Fill = new SolidColorBrush(Color.Parse("#332E86FF")),
            Stroke = new SolidColorBrush(Color.Parse("#53D6FF")),
            StrokeThickness = 1,
            IsHitTestVisible = false,
        };
        Canvas.SetLeft(_marquee, position.X);
        Canvas.SetTop(_marquee, position.Y);
        _overlay.Children.Add(_marquee);
    }

    private void UpdateMarquee(Point position)
    {
        if (_marquee is null) return;

        var left = Math.Min(_marqueeStart.X, position.X);
        var top = Math.Min(_marqueeStart.Y, position.Y);
        var width = Math.Abs(position.X - _marqueeStart.X);
        var height = Math.Abs(position.Y - _marqueeStart.Y);
        Canvas.SetLeft(_marquee, left);
        Canvas.SetTop(_marquee, top);
        _marquee.Width = width;
        _marquee.Height = height;
    }

    private void EndMarquee(Point position)
    {
        var left = Math.Min(_marqueeStart.X, position.X);
        var top = Math.Min(_marqueeStart.Y, position.Y);
        var rect = new Rect(left, top, Math.Abs(position.X - _marqueeStart.X), Math.Abs(position.Y - _marqueeStart.Y));

        if (_marquee is not null)
        {
            _overlay.Children.Remove(_marquee);
            _marquee = null;
        }

        _marqueeSelecting = false;

        if (rect.Width < 4 && rect.Height < 4)
        {
            if (!_marqueeAdditive) Viewport.ViewModel?.ClearSelection();
            return;
        }

        Viewport.SelectInRect(rect, _marqueeAdditive);
    }

    protected override void OnPointerMoved(PointerEventArgs e)
    {
        base.OnPointerMoved(e);

        if (Viewport.ViewModel is { IsPlaying: true } && Viewport.GameInputEnabled)
        {
            var current = e.GetCurrentPoint(this);
            Viewport.UpdatePointerInput(current.Position);
            Viewport.UpdateMouseButton(0, current.Properties.IsLeftButtonPressed);
            Viewport.UpdateMouseButton(1, current.Properties.IsRightButtonPressed);
            Viewport.UpdateMouseButton(2, current.Properties.IsMiddleButtonPressed);
            e.Handled = true;
            return;
        }

        if (_marqueeSelecting)
        {
            UpdateMarquee(e.GetPosition(this));
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
            if (Viewport.ViewModel?.TileFillMode != true)
            {
                Viewport.PaintAt(e.GetPosition(this));
            }
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

        if (Viewport.ViewModel is { IsPlaying: true } && Viewport.GameInputEnabled)
        {
            Viewport.UpdateMouseButton(0, false);
            Viewport.UpdateMouseButton(1, false);
            Viewport.UpdateMouseButton(2, false);
            e.Handled = true;
            return;
        }

        if (_marqueeSelecting)
        {
            EndMarquee(e.GetPosition(this));
            _capturedPointer?.Capture(null);
            _capturedPointer = null;
            e.Handled = true;
            return;
        }

        if (_painting)
        {
            _painting = false;
            if (Viewport.ViewModel?.TileFillMode != true) Viewport.CommitTileStroke();
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

        if (Viewport.ViewModel is { IsPlaying: true } && Viewport.GameInputEnabled)
        {
            e.Handled = true;
            return;
        }

        Viewport.ZoomAtPoint(e.GetPosition(this), e.Delta.Y);
        e.Handled = true;
    }
}
