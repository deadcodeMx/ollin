using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;

namespace Ollin.Editor.Styles;

public enum EditorThemeMode
{
    Dark,
    Light,
    Ollin,
}

/// <summary>Paletas de la interfaz: oscura, clara y basada en el logo.</summary>
public static class EditorTheme
{
    public static EditorThemeMode Current { get; private set; } = EditorThemeMode.Dark;

    public static void Apply(EditorThemeMode mode)
    {
        Current = mode;
        var palette = Palette(mode);

        if (Application.Current is { } app)
        {
            foreach (var (key, color) in palette)
            {
                app.Resources[key] = new SolidColorBrush(Color.Parse(color));
            }

            app.RequestedThemeVariant = mode == EditorThemeMode.Light ? ThemeVariant.Light : ThemeVariant.Dark;
        }
    }

    public static EditorThemeMode Parse(string? value) => value switch
    {
        "Light" => EditorThemeMode.Light,
        "Ollin" => EditorThemeMode.Ollin,
        _ => EditorThemeMode.Dark,
    };

    private static Dictionary<string, string> Palette(EditorThemeMode mode) => mode switch
    {
        EditorThemeMode.Light => new Dictionary<string, string>
        {
            ["Ollin.Bg"] = "#F2F4F8",
            ["Ollin.Panel"] = "#FFFFFF",
            ["Ollin.PanelAlt"] = "#E9EDF4",
            ["Ollin.Border"] = "#D2D8E3",
            ["Ollin.Text"] = "#1B2230",
            ["Ollin.TextDim"] = "#5B6577",
            ["Ollin.Accent"] = "#C97612",
            ["Ollin.AccentDim"] = "#E7B36B",
            ["Ollin.AccentHover"] = "#D9821F",
            ["Ollin.AccentText"] = "#1A1206",
            ["Ollin.Jade"] = "#2A8F82",
            ["Ollin.Danger"] = "#C0392B",
            ["Ollin.ViewportBg"] = "#E9EDF4",
            ["Ollin.Hover"] = "#DDE3EC",
            ["Ollin.Pressed"] = "#CFD6E2",
            ["Ollin.Neutral"] = "#4A5466",
        },
        EditorThemeMode.Ollin => new Dictionary<string, string>
        {
            ["Ollin.Bg"] = "#140F1C",
            ["Ollin.Panel"] = "#1E1629",
            ["Ollin.PanelAlt"] = "#2A1F38",
            ["Ollin.Border"] = "#3D2C50",
            ["Ollin.Text"] = "#F4ECFF",
            ["Ollin.TextDim"] = "#BBA8D6",
            ["Ollin.Accent"] = "#FF8A4C",
            ["Ollin.AccentDim"] = "#9A5A3A",
            ["Ollin.AccentHover"] = "#FF9D68",
            ["Ollin.AccentText"] = "#1A0E08",
            ["Ollin.Jade"] = "#6FD3C7",
            ["Ollin.Danger"] = "#E85D75",
            ["Ollin.ViewportBg"] = "#100B18",
            ["Ollin.Hover"] = "#332744",
            ["Ollin.Pressed"] = "#3E3052",
            ["Ollin.Neutral"] = "#CDB9E6",
        },
        _ => new Dictionary<string, string>
        {
            ["Ollin.Bg"] = "#0E1015",
            ["Ollin.Panel"] = "#151922",
            ["Ollin.PanelAlt"] = "#1B202B",
            ["Ollin.Border"] = "#252B39",
            ["Ollin.Text"] = "#E7EAF0",
            ["Ollin.TextDim"] = "#8B94A7",
            ["Ollin.Accent"] = "#E9A23B",
            ["Ollin.AccentDim"] = "#8A6428",
            ["Ollin.AccentHover"] = "#F2B457",
            ["Ollin.AccentText"] = "#1A1206",
            ["Ollin.Jade"] = "#47B8A8",
            ["Ollin.Danger"] = "#E05B5B",
            ["Ollin.ViewportBg"] = "#0B0D12",
            ["Ollin.Hover"] = "#232936",
            ["Ollin.Pressed"] = "#2B3242",
            ["Ollin.Neutral"] = "#C7D0DE",
        },
    };
}
