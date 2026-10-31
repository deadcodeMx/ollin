using Ollin.Core.Hud;
using Ollin.Core.Saves;

namespace Ollin.Core.Model.Components;

/// <summary>Muestra una variable de partida en el HUD (contadores, flags, objetivos).</summary>
public sealed class VariableHud : Component, ITickable
{
    public string VariableName { get; set; } = "enemigos_muertos";

    public string Label { get; set; } = "Enemigos";

    public string Prefix { get; set; } = string.Empty;

    public string Suffix { get; set; } = string.Empty;

    public bool HideWhenEmpty { get; set; } = true;

    public void OnUpdate(float deltaTime)
    {
        var state = GameStateHost.Current;
        var raw = state?.Get(VariableName);

        if (string.IsNullOrWhiteSpace(raw) && HideWhenEmpty)
        {
            HudHost.SetLabel(Label, string.Empty);
            return;
        }

        HudHost.SetLabel(Label, $"{Prefix}{raw ?? "0"}{Suffix}");
    }

    public override Component CloneFor(Entity newOwner) => new VariableHud
    {
        Enabled = Enabled,
        VariableName = VariableName,
        Label = Label,
        Prefix = Prefix,
        Suffix = Suffix,
        HideWhenEmpty = HideWhenEmpty,
        Owner = newOwner,
    };
}
