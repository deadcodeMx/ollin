using System.Runtime.InteropServices;
using Avalonia.Controls;
using Avalonia.Input;
using Ollin.Core.Input;

namespace Ollin.Editor.Controls;

/// <summary>
/// Captura el ratón en Windows: oculta el cursor, lo fija al centro del viewport,
/// limita el cursor a la ventana y entrega el delta a <see cref="MouseLook"/>.
/// </summary>
public static class WindowsMouseLock
{
    private static Window? _window;
    private static IntPtr _handle;
    private static int _centerX;
    private static int _centerY;
    private static bool _enabled;
    private static bool _skipNextDelta;

    public static bool IsEnabled => _enabled;

    public static void Enable(Window window)
    {
        if (_enabled && ReferenceEquals(_window, window))
        {
            return;
        }

        Disable();

        if (!window.IsActive)
        {
            return;
        }

        try
        {
            _handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;

            if (_handle == IntPtr.Zero || !GetClientRect(_handle, out var client))
            {
                return;
            }

            var origin = new POINT { X = 0, Y = 0 };

            if (!ClientToScreen(_handle, ref origin))
            {
                return;
            }

            _centerX = origin.X + (client.Right - client.Left) / 2;
            _centerY = origin.Y + (client.Bottom - client.Top) / 2;

            var clip = new RECT
            {
                Left = origin.X,
                Top = origin.Y,
                Right = origin.X + client.Right - client.Left,
                Bottom = origin.Y + client.Bottom - client.Top,
            };

            window.Cursor = new Cursor(StandardCursorType.None);
            ClipCursorRect(ref clip);
            SetCursorPos(_centerX, _centerY);
            _window = window;
            _enabled = true;
            _skipNextDelta = true;
            MouseLook.IsLocked = true;
            MouseLook.Reset();
        }
        catch
        {
            Disable();
        }
    }

    public static void Update(Window window)
    {
        if (!_enabled || !ReferenceEquals(_window, window))
        {
            return;
        }

        if (!window.IsActive)
        {
            Disable();
            return;
        }

        try
        {
            if (_skipNextDelta)
            {
                _skipNextDelta = false;
                SetCursorPos(_centerX, _centerY);
                return;
            }

            if (!GetCursorPos(out var cursor))
            {
                return;
            }

            var dx = cursor.X - _centerX;
            var dy = cursor.Y - _centerY;

            // Al recuperar el foco el puntero puede estar lejos: se recentra sin girar.
            if (Math.Abs(dx) < 400 && Math.Abs(dy) < 400)
            {
                MouseLook.AddDelta(new System.Numerics.Vector2(dx, dy));
            }

            SetCursorPos(_centerX, _centerY);
        }
        catch
        {
            Disable();
        }
    }

    public static void Disable()
    {
        if (_window is not null)
        {
            try
            {
                ClipCursorNone(IntPtr.Zero);
                _window.Cursor = new Cursor(StandardCursorType.Arrow);
            }
            catch
            {
                // La ventana pudo cerrarse.
            }
        }

        _window = null;
        _handle = IntPtr.Zero;
        _enabled = false;
        MouseLook.Disable();
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [DllImport("user32.dll")]
    private static extern bool GetClientRect(IntPtr hWnd, out RECT rect);

    [DllImport("user32.dll")]
    private static extern bool ClientToScreen(IntPtr hWnd, ref POINT point);

    [DllImport("user32.dll")]
    private static extern bool GetCursorPos(out POINT point);

    [DllImport("user32.dll")]
    private static extern bool SetCursorPos(int x, int y);

    [DllImport("user32.dll", EntryPoint = "ClipCursor")]
    private static extern bool ClipCursorRect(ref RECT rect);

    [DllImport("user32.dll", EntryPoint = "ClipCursor")]
    private static extern bool ClipCursorNone(IntPtr rect);

    public static void Reset()
    {
        Disable();
    }
}
