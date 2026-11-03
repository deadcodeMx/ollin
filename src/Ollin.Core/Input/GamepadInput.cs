using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Ollin.Core.Input;

/// <summary>Gamepad mediante XInput (Windows). Si no hay mando, todo queda a cero.</summary>
public static class GamepadInput
{
    private const uint ErrorSuccess = 0;
    private static readonly HashSet<string> Down = new();
    private static readonly HashSet<string> Previous = new();

    private static float _leftX, _leftY, _rightX, _rightY, _leftTrigger, _rightTrigger;
    private static readonly ConcurrentQueue<JsEvent> LinuxEvents = new();
    private static Thread? _linuxThread;
    private static float _linuxAxisX, _linuxAxisY, _linuxRightX, _linuxRightY;

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

    private static void PollLinux()
    {
        EnsureLinuxReader();
        while (LinuxEvents.TryDequeue(out var evt))
        {
            if ((evt.Type & 0x80) != 0) continue;
            if ((evt.Type & 0x01) != 0)
            {
                var name = evt.Number switch
                {
                    0 => "A", 1 => "B", 2 => "X", 3 => "Y",
                    4 => "ShoulderLeft", 5 => "ShoulderRight",
                    6 => "Back", 7 => "Start", 8 => "Guide",
                    _ => null,
                };
                if (name is not null && evt.Value != 0) Down.Add(name);
            }
            else if ((evt.Type & 0x02) != 0)
            {
                var value = evt.Value / 32767f;
                switch (evt.Number)
                {
                    case 0: _leftX = value; break;
                    case 1: _leftY = -value; break;
                    case 2: _rightX = value; break;
                    case 3: _rightY = -value; break;
                    case 6: if (value < -0.5f) Down.Add("DPadLeft"); else if (value > 0.5f) Down.Add("DPadRight"); break;
                    case 7: if (value < -0.5f) Down.Add("DPadUp"); else if (value > 0.5f) Down.Add("DPadDown"); break;
                }
            }
        }

        void EnsureLinuxReader()
        {
            if (_linuxThread is not null) return;
            _linuxThread = new Thread(() =>
            {
                try
                {
                    var path = File.Exists("/dev/input/js0") ? "/dev/input/js0"
                        : Directory.Exists("/dev/input") ? Directory.EnumerateFiles("/dev/input", "js*").FirstOrDefault() : null;
                    if (path is null) return;
                    using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                    var buffer = new byte[8];
                    while (true)
                    {
                        var read = stream.Read(buffer, 0, 8);
                        if (read < 8) break;
                        var time = BitConverter.ToUInt32(buffer, 0);
                        var value = BitConverter.ToInt16(buffer, 4);
                        LinuxEvents.Enqueue(new JsEvent(time, value, buffer[6], buffer[7]));
                    }
                }
                catch { }
            }) { IsBackground = true, Name = "Ollin Linux gamepad" };
            _linuxThread.Start();
        }
    }

    private readonly record struct JsEvent(uint Time, short Value, byte Type, byte Number);
    public static void Reset()
    {
        Down.Clear();
        Previous.Clear();
        _leftX = _leftY = _rightX = _rightY = _leftTrigger = _rightTrigger = 0f;
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
