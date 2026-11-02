using System.Runtime.InteropServices;

namespace Ollin.Core.Input;

/// <summary>Gamepad mediante XInput (Windows). Si no hay mando, todo queda a cero.</summary>
public static class GamepadInput
{
    private const uint ErrorSuccess = 0;
    private static readonly HashSet<string> Down = new();
    private static readonly HashSet<string> Previous = new();

    private static float _leftX, _leftY, _rightX, _rightY, _leftTrigger, _rightTrigger;
    private static bool _polled;

    public static bool IsDown(string control) => Down.Contains(control);

    public static bool WasPressed(string control) => Down.Contains(control) && !Previous.Contains(control);

    public static bool WasReleased(string control) => !Down.Contains(control) && Previous.Contains(control);

    public static float Axis(string control) => control switch
    {
        "LeftStickX" => _leftX,
        "LeftStickY" => _leftY,
        "RightStickX" => _rightX,
        "RightStickY" => _rightY,
        "TriggerLeft" => _leftTrigger,
        "TriggerRight" => _rightTrigger,
        _ => 0f,
    };

    public static void Poll()
    {
        _polled = true;
        Previous.Clear();
        foreach (var control in Down) Previous.Add(control);
        Down.Clear();
        _leftX = _leftY = _rightX = _rightY = _leftTrigger = _rightTrigger = 0f;

        try
        {
            if (XInputGetState(0, out var state) != ErrorSuccess) return;
            var pad = state.Gamepad;
            Add(pad.Buttons, 0x1000, "A");
            Add(pad.Buttons, 0x2000, "B");
            Add(pad.Buttons, 0x4000, "X");
            Add(pad.Buttons, 0x8000, "Y");
            Add(pad.Buttons, 0x0010, "Start");
            Add(pad.Buttons, 0x0020, "Back");
            Add(pad.Buttons, 0x0100, "ShoulderLeft");
            Add(pad.Buttons, 0x0200, "ShoulderRight");
            Add(pad.Buttons, 0x0001, "DPadUp");
            Add(pad.Buttons, 0x0002, "DPadDown");
            Add(pad.Buttons, 0x0004, "DPadLeft");
            Add(pad.Buttons, 0x0008, "DPadRight");
            Add(pad.Buttons, 0x0040, "ThumbLeft");
            Add(pad.Buttons, 0x0080, "ThumbRight");

            _leftX = Normalize(pad.ThumbLX);
            _leftY = Normalize(pad.ThumbLY);
            _rightX = Normalize(pad.ThumbRX);
            _rightY = Normalize(pad.ThumbRY);
            _leftTrigger = pad.LeftTrigger / 255f;
            _rightTrigger = pad.RightTrigger / 255f;
        }
        catch
        {
            // Sin XInput o sin mando: el teclado sigue funcionando.
        }
    }

    public static void Reset()
    {
        Down.Clear();
        Previous.Clear();
        _leftX = _leftY = _rightX = _rightY = _leftTrigger = _rightTrigger = 0f;
        _polled = false;
    }

    private static void Add(ushort buttons, ushort mask, string name)
    {
        if ((buttons & mask) != 0) Down.Add(name);
    }

    private static float Normalize(short value) => value >= 0 ? value / 32767f : value / 32768f;

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputState
    {
        public uint PacketNumber;
        public XInputGamepad Gamepad;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct XInputGamepad
    {
        public ushort Buttons;
        public byte LeftTrigger;
        public byte RightTrigger;
        public short ThumbLX;
        public short ThumbLY;
        public short ThumbRX;
        public short ThumbRY;
    }

    [DllImport("xinput1_4.dll")]
    private static extern uint XInputGetState(uint index, out XInputState state);
}
