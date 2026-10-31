using System.Text.Json.Serialization;
using Ollin.Core.Hud;
using Ollin.Core.Input;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Gestiona varias armas en la misma entidad. Las teclas 1/2/3 cambian de ranura
/// y el HUD muestra el arma activa y su munición.
/// </summary>
public sealed class WeaponGroup : Component, ITickable
{
    public int ActiveSlot { get; set; } = 1;

    public bool SwitchWithNumberKeys { get; set; } = true;

    public bool ShowOnHud { get; set; } = true;

    [JsonIgnore]
    public Weapon? ActiveWeapon
    {
        get
        {
            if (Owner is null)
            {
                return null;
            }

            foreach (var component in Owner.Components)
            {
                if (component is Weapon weapon && weapon.Enabled && weapon.Slot == ActiveSlot)
                {
                    return weapon;
                }
            }

            return null;
        }
    }

    public void OnUpdate(float deltaTime)
    {
        if (Owner is null)
        {
            return;
        }

        if (SwitchWithNumberKeys)
        {
            if (InputState.WasKeyPressed(KeyCode.D1)) ActiveSlot = 1;
            if (InputState.WasKeyPressed(KeyCode.D2)) ActiveSlot = 2;
            if (InputState.WasKeyPressed(KeyCode.D3)) ActiveSlot = 3;
        }

        if (!ShowOnHud)
        {
            return;
        }

        var weapon = ActiveWeapon;

        if (weapon is null)
        {
            HudHost.SetLabel("Arma", string.Empty);
            HudHost.SetLabel("Munición", string.Empty);
            return;
        }

        HudHost.SetLabel("Arma", weapon.Name);
        HudHost.SetLabel("Munición", weapon.Ammo < 0 ? "∞" : $"{weapon.Ammo}/{weapon.ReserveAmmo switch { < 0 => "∞", _ => weapon.ReserveAmmo.ToString() }}");
    }

    public override Component CloneFor(Entity newOwner) => new WeaponGroup
    {
        Enabled = Enabled,
        ActiveSlot = ActiveSlot,
        SwitchWithNumberKeys = SwitchWithNumberKeys,
        ShowOnHud = ShowOnHud,
        Owner = newOwner,
    };
}
