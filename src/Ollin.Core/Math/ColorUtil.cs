using System.Globalization;
using System.Numerics;

namespace Ollin.Core.Math;

/// <summary>Utilidades de color: el editor trabaja con hexadecimal, el motor con Vector4.</summary>
public static class ColorUtil
{
    public static Vector4 FromHex(string hex, Vector4 fallback)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return fallback;
        }

        var text = hex.Trim().TrimStart('#');
        if (text.Length is not (6 or 8))
        {
            return fallback;
        }

        if (!uint.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out var value))
        {
            return fallback;
        }

        if (text.Length == 6)
        {
            value = (value << 8) | 0xFF;
        }

        return new Vector4(
            ((value >> 24) & 0xFF) / 255f,
            ((value >> 16) & 0xFF) / 255f,
            ((value >> 8) & 0xFF) / 255f,
            (value & 0xFF) / 255f);
    }

    public static string ToHex(Vector4 color)
    {
        static int Channel(float value) => (int)System.MathF.Round(System.Math.Clamp(value, 0f, 1f) * 255f);

        return $"#{Channel(color.X):X2}{Channel(color.Y):X2}{Channel(color.Z):X2}{Channel(color.W):X2}";
    }
}
