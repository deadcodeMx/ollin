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
/// <summary>Elemento de UI runtime expuesto a Avalonia.</summary>
public sealed class UiElementView
{
    public UiElementView(Ollin.Core.Model.Components.UiElement element, Action<string> invoke, Ollin.Core.Model.Components.UiTheme? theme = null)
    {
        Id = element.Id;
        Kind = element.Kind;
        Text = element.Text;
        X = element.X;
        Y = element.Y;
        Width = element.Width;
        Height = element.Height;
        FontSize = element.FontSize;
        FontFamily = new Avalonia.Media.FontFamily(string.IsNullOrWhiteSpace(element.FontFamily) ? (theme?.FontFamily ?? "Segoe UI") : element.FontFamily);
        var textColor = element.Color.W <= 0f ? (theme?.TextColor ?? element.Color) : element.Color;
        var panelColor = element.Kind == Ollin.Core.Model.Components.UiElementKind.Button
            ? (theme?.ButtonColor ?? element.Color)
            : (theme?.PanelColor ?? element.Color);
        Anchor = element.Anchor;
        Visible = element.Visible;
        Action = element.Action ?? element.Id;
        Focusable = element.Focusable || element.Kind == Ollin.Core.Model.Components.UiElementKind.Button;
        AccessibilityLabel = string.IsNullOrWhiteSpace(element.AccessibilityLabel) ? element.Text : element.AccessibilityLabel;
        ListText = string.Join(Environment.NewLine, element.Items);
        Value = element.Max <= 0 ? 0 : System.Math.Clamp(element.Value / element.Max, 0, 1);
        Command = new UiActionCommand(() => invoke(element.Action ?? element.Id));
        Brush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(
            (byte)(System.Math.Clamp(panelColor.W, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(panelColor.X, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(panelColor.Y, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(panelColor.Z, 0f, 1f) * 255)));
        TextBrush = new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.FromArgb(
            (byte)(System.Math.Clamp(textColor.W, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(textColor.X, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(textColor.Y, 0f, 1f) * 255),
            (byte)(System.Math.Clamp(textColor.Z, 0f, 1f) * 255)));
    }

    public string Id { get; }
    public Ollin.Core.Model.Components.UiElementKind Kind { get; }
    public string Text { get; }
    public float X { get; }
    public float Y { get; }
    public float Width { get; }
    public float Height { get; }
    public float FontSize { get; }
    public Avalonia.Media.FontFamily FontFamily { get; }
    public Ollin.Core.Model.Components.UiAnchor Anchor { get; }
    public Avalonia.Layout.HorizontalAlignment HorizontalAlignment => Anchor switch
    {
        Ollin.Core.Model.Components.UiAnchor.TopCenter or Ollin.Core.Model.Components.UiAnchor.Center or Ollin.Core.Model.Components.UiAnchor.BottomCenter => Avalonia.Layout.HorizontalAlignment.Center,
        Ollin.Core.Model.Components.UiAnchor.TopRight or Ollin.Core.Model.Components.UiAnchor.MiddleRight or Ollin.Core.Model.Components.UiAnchor.BottomRight => Avalonia.Layout.HorizontalAlignment.Right,
        _ => Avalonia.Layout.HorizontalAlignment.Left,
    };
    public Avalonia.Layout.VerticalAlignment VerticalAlignment => Anchor switch
    {
        Ollin.Core.Model.Components.UiAnchor.MiddleLeft or Ollin.Core.Model.Components.UiAnchor.Center or Ollin.Core.Model.Components.UiAnchor.MiddleRight => Avalonia.Layout.VerticalAlignment.Center,
        Ollin.Core.Model.Components.UiAnchor.BottomLeft or Ollin.Core.Model.Components.UiAnchor.BottomCenter or Ollin.Core.Model.Components.UiAnchor.BottomRight => Avalonia.Layout.VerticalAlignment.Bottom,
        _ => Avalonia.Layout.VerticalAlignment.Top,
    };
    public Avalonia.Thickness Margin => new Avalonia.Thickness(X, Y, 0, 0);
    public bool Visible { get; }
    public string Action { get; }
    public bool Focusable { get; }
    public string AccessibilityLabel { get; }
    public string ListText { get; }
    public bool IsList => Kind == Ollin.Core.Model.Components.UiElementKind.List;
    public bool IsFocused { get; set; }
    public double Value { get; }
    public System.Windows.Input.ICommand Command { get; }
    public Avalonia.Media.IBrush Brush { get; }
    public Avalonia.Media.IBrush FocusBrush => new Avalonia.Media.SolidColorBrush(Avalonia.Media.Color.Parse("#E9A23B"));
    public Avalonia.Media.IBrush FocusBorder => IsFocused ? FocusBrush : new Avalonia.Media.SolidColorBrush(Avalonia.Media.Colors.Transparent);
    public Avalonia.Media.IBrush TextBrush { get; }
    public bool IsPanel => Kind is Ollin.Core.Model.Components.UiElementKind.Panel or Ollin.Core.Model.Components.UiElementKind.Container;
    public bool IsLabel => Kind == Ollin.Core.Model.Components.UiElementKind.Label || Kind == Ollin.Core.Model.Components.UiElementKind.Panel;
    public bool IsButton => Kind == Ollin.Core.Model.Components.UiElementKind.Button;
    public bool IsBar => Kind == Ollin.Core.Model.Components.UiElementKind.Bar;
}

public sealed class UiActionCommand : System.Windows.Input.ICommand
{
    private readonly Action _action;
    public UiActionCommand(Action action) => _action = action;
    public event EventHandler? CanExecuteChanged { add { } remove { } }
    public bool CanExecute(object? parameter) => true;
    public void Execute(object? parameter) => _action();
}