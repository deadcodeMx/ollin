using Avalonia.Media;

namespace Ollin.Editor.ViewModels;

/// <summary>Barra del HUD ya preparada para la interfaz.</summary>
public sealed class HudBarView
{
    public HudBarView(string name, string valueText, double fraction, IBrush brush)
    {
        Name = name;
        ValueText = valueText;
        Fraction = fraction;
        Brush = brush;
    }

    public string Name { get; }

    public string ValueText { get; }

    public double Fraction { get; }

    public IBrush Brush { get; }
}

/// <summary>Etiqueta del HUD.</summary>
public sealed class HudLabelView
{
    public HudLabelView(string name, string text)
    {
        Name = name;
        Text = text;
    }

    public string Name { get; }

    public string Text { get; }

    public string Display => string.IsNullOrWhiteSpace(Name) ? Text : $"{Name}: {Text}";
}

/// <summary>Mensaje temporal del HUD.</summary>
public sealed class HudMessageView
{
    public HudMessageView(string text) => Text = text;

    public string Text { get; }
}
/// <summary>Slot visible del inventario.</summary>
public sealed class InventorySlotView
{
    public InventorySlotView(string itemId, int quantity)
    {
        ItemId = itemId;
        Quantity = quantity;
        Initial = string.IsNullOrWhiteSpace(itemId) ? "?" : itemId[..1].ToUpperInvariant();
    }

    public string ItemId { get; }
    public int Quantity { get; }
    public string Initial { get; }
    public string QuantityText => Quantity > 1 ? $"x{Quantity}" : string.Empty;
}
/// <summary>Ranura de equipo visible.</summary>
public sealed class EquipmentSlotView
{
    public EquipmentSlotView(string slot, string itemId) { Slot = slot; ItemId = itemId; }
    public string Slot { get; }
    public string ItemId { get; }
    public string Display => $"{Slot}: {ItemId}";
}