using System.Numerics;

namespace Ollin.Core.Diagnostics;

public enum DebugShape
{
    Line,
    Rect,
}

/// <summary>Una figura de depuración en coordenadas de mundo.</summary>
public readonly record struct DebugCommand(DebugShape Shape, Vector2 A, Vector2 B, Vector4 Color);

/// <summary>
/// Dibujo de depuración para scripts: líneas y cajas que se pintan sobre la escena
/// y se limpian al final de cada cuadro.
/// </summary>
public static class DebugDraw
{
    private static readonly object Gate = new();
    private static readonly List<DebugCommand> Commands = new();

    /// <summary>Figuras del cuadro actual (copia segura para el hilo de render).</summary>
    public static DebugCommand[] Snapshot()
    {
        lock (Gate)
        {
            return Commands.ToArray();
        }
    }

    public static void Line(Vector2 from, Vector2 to, Vector4? color = null)
    {
        lock (Gate)
        {
            Commands.Add(new DebugCommand(DebugShape.Line, from, to, color ?? new Vector4(0.3f, 0.9f, 1f, 1f)));
        }
    }

    public static void Rect(Vector2 center, Vector2 size, Vector4? color = null)
    {
        var half = size * 0.5f;
        var tl = center + new Vector2(-half.X, half.Y);
        var tr = center + new Vector2(half.X, half.Y);
        var br = center + new Vector2(half.X, -half.Y);
        var bl = center + new Vector2(-half.X, -half.Y);
        var c = color ?? new Vector4(0.3f, 0.9f, 1f, 1f);

        lock (Gate)
        {
            Commands.Add(new DebugCommand(DebugShape.Line, tl, tr, c));
            Commands.Add(new DebugCommand(DebugShape.Line, tr, br, c));
            Commands.Add(new DebugCommand(DebugShape.Line, br, bl, c));
            Commands.Add(new DebugCommand(DebugShape.Line, bl, tl, c));
        }
    }

    public static void WriteString(Vector2 position, string text, Vector4? color = null)
    {
        // Sin fuente de texto en el viewport: se registra en consola para no mentir.
        Log.Info($"[debug] {text}", "DebugDraw");
    }

    public static void Clear()
    {
        lock (Gate)
        {
            Commands.Clear();
        }
    }
}
