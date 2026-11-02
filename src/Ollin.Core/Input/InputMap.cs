using System.Numerics;

namespace Ollin.Core.Input;

/// <summary>Acción lógica configurable, por ejemplo "jump" o "move_left".</summary>
public sealed class InputActionDefinition
{
    public string Name { get; set; } = "action";

    public List<InputBinding> Bindings { get; set; } = new();
}

/// <summary>Binding de una acción: tecla, botón de mando o eje de mando.</summary>
public sealed class InputBinding
{
    public string Device { get; set; } = "key";

    public string Control { get; set; } = "A";

    /// <summary>Para ejes: 1 o -1 según la dirección.</summary>
    public int Direction { get; set; } = 1;

    public override string ToString() => Device switch
    {
        "key" => $"key:{Control}",
        "gamepad" => $"pad:{Control}",
        "axis" => $"axis:{Control}{(Direction < 0 ? "-" : "+")}",
        _ => Device,
    };
}

/// <summary>Mapa de acciones del proyecto. Se guarda dentro de project.ollin.</summary>
public static class InputMap
{
    private static List<InputActionDefinition> _actions = CreateDefaults().ToList();

    public static IReadOnlyList<InputActionDefinition> Actions => _actions;

    public static void Set(IEnumerable<InputActionDefinition>? actions)
    {
        _actions = actions?.ToList() ?? CreateDefaults().ToList();
        if (_actions.Count == 0) _actions = CreateDefaults();
    }

    public static List<InputActionDefinition> CreateDefaults() => new List<InputActionDefinition>
    {
        Action("move_left", Key("A"), Key("Left"), Pad("DPadLeft"), Axis("LeftStickX", -1)),
        Action("move_right", Key("D"), Key("Right"), Pad("DPadRight"), Axis("LeftStickX", 1)),
        Action("move_up", Key("W"), Key("Up"), Pad("DPadUp"), Axis("LeftStickY", 1)),
        Action("move_down", Key("S"), Key("Down"), Pad("DPadDown"), Axis("LeftStickY", -1)),
        Action("jump", Key("Space"), Key("W"), Key("Up"), Pad("A")),
        Action("interact", Key("E"), Pad("X")),
        Action("attack", Key("LeftControl"), Pad("X")),
        Action("pause", Key("Escape"), Pad("Start")),
    };

    public static bool IsDown(string action)
    {
        var definition = _actions.FirstOrDefault(item => item.Name.Equals(action, StringComparison.OrdinalIgnoreCase));
        return definition is not null && definition.Bindings.Any(IsBindingDown);
    }

    public static bool WasPressed(string action)
    {
        var definition = _actions.FirstOrDefault(item => item.Name.Equals(action, StringComparison.OrdinalIgnoreCase));
        return definition is not null && definition.Bindings.Any(IsBindingPressed);
    }

    public static bool WasReleased(string action)
    {
        var definition = _actions.FirstOrDefault(item => item.Name.Equals(action, StringComparison.OrdinalIgnoreCase));
        return definition is not null && definition.Bindings.Any(IsBindingReleased);
    }

    private static bool IsBindingDown(InputBinding binding) => binding.Device switch
    {
        "key" => Enum.TryParse<KeyCode>(binding.Control, true, out var key) && InputState.IsKeyDown(key),
        "gamepad" => GamepadInput.IsDown(binding.Control),
        "axis" => GamepadInput.Axis(binding.Control) * MathF.Sign(binding.Direction) >= 0.5f,
        _ => false,
    };

    private static bool IsBindingPressed(InputBinding binding) => binding.Device switch
    {
        "key" => Enum.TryParse<KeyCode>(binding.Control, true, out var key) && InputState.WasKeyPressed(key),
        "gamepad" => GamepadInput.WasPressed(binding.Control),
        "axis" => IsBindingDown(binding),
        _ => false,
    };

    private static bool IsBindingReleased(InputBinding binding) => binding.Device switch
    {
        "key" => Enum.TryParse<KeyCode>(binding.Control, true, out var key) && InputState.WasKeyReleased(key),
        "gamepad" => GamepadInput.WasReleased(binding.Control),
        "axis" => false,
        _ => false,
    };

    private static InputActionDefinition Action(string name, params InputBinding[] bindings) => new()
    {
        Name = name,
        Bindings = bindings.ToList(),
    };

    private static InputBinding Key(string control) => new() { Device = "key", Control = control };
    private static InputBinding Pad(string control) => new() { Device = "gamepad", Control = control };
    private static InputBinding Axis(string control, int direction) => new() { Device = "axis", Control = control, Direction = direction };
}
