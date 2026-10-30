using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>Una pila de objetos dentro del inventario.</summary>
public sealed class InventorySlot
{
    public string ItemId { get; set; } = string.Empty;

    public int Quantity { get; set; }
}

/// <summary>Inventario con pilas y capacidad limitada.</summary>
public sealed class Inventory : Component
{
    public int Capacity { get; set; } = 12;

    public List<InventorySlot> Slots { get; set; } = new();

    [JsonIgnore]
    public int UsedSlots => Slots.Count;

    /// <summary>Añade objetos y devuelve cuántos entraron realmente.</summary>
    public int Add(string itemId, int quantity = 1, int maxStack = 99)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return 0;
        }

        var stackLimit = System.Math.Max(1, maxStack);
        var remaining = quantity;

        foreach (var slot in Slots)
        {
            if (remaining <= 0)
            {
                break;
            }

            if (!string.Equals(slot.ItemId, itemId, StringComparison.OrdinalIgnoreCase) || slot.Quantity >= stackLimit)
            {
                continue;
            }

            var amount = System.Math.Min(stackLimit - slot.Quantity, remaining);
            slot.Quantity += amount;
            remaining -= amount;
        }

        while (remaining > 0 && Slots.Count < Capacity)
        {
            var amount = System.Math.Min(stackLimit, remaining);
            Slots.Add(new InventorySlot { ItemId = itemId, Quantity = amount });
            remaining -= amount;
        }

        return quantity - remaining;
    }

    /// <summary>Quita objetos y devuelve cuántos se quitaron.</summary>
    public int Remove(string itemId, int quantity = 1)
    {
        if (string.IsNullOrWhiteSpace(itemId) || quantity <= 0)
        {
            return 0;
        }

        var remaining = quantity;

        for (var i = Slots.Count - 1; i >= 0 && remaining > 0; i--)
        {
            var slot = Slots[i];

            if (!string.Equals(slot.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var amount = System.Math.Min(slot.Quantity, remaining);
            slot.Quantity -= amount;
            remaining -= amount;

            if (slot.Quantity <= 0)
            {
                Slots.RemoveAt(i);
            }
        }

        return quantity - remaining;
    }

    public int Count(string itemId) =>
        string.IsNullOrWhiteSpace(itemId)
            ? 0
            : Slots.Where(slot => string.Equals(slot.ItemId, itemId, StringComparison.OrdinalIgnoreCase))
                   .Sum(slot => slot.Quantity);

    public bool Has(string itemId, int quantity = 1) => Count(itemId) >= quantity;

    public void Clear() => Slots.Clear();

    public override Component CloneFor(Entity newOwner) => new Inventory
    {
        Enabled = Enabled,
        Capacity = Capacity,
        Slots = Slots.Select(slot => new InventorySlot { ItemId = slot.ItemId, Quantity = slot.Quantity }).ToList(),
        Owner = newOwner,
    };
}
