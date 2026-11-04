using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Material de canvas con shader de fragmento GLSL ES 3.0.</summary>
public sealed class CanvasMaterial2D : Component
{
    public string FragmentSource { get; set; } = """
        #version 300 es
        precision mediump float;
        in vec2 vTexCoord;
        in vec4 vColor;
        uniform sampler2D uTexture;
        uniform vec4 uTint;
        out vec4 fragColor;
        void main() { fragColor = texture(uTexture, vTexCoord) * vColor * uTint; }
        """;

    public Vector4 Tint { get; set; } = Vector4.One;
    public float TimeScale { get; set; } = 1f;

    /// <summary>Uniformes vec4 por nombre.</summary>
    public Dictionary<string, Vector4> Parameters { get; set; } = new();

    public override Component CloneFor(Entity newOwner) => new CanvasMaterial2D
    {
        Enabled = Enabled, FragmentSource = FragmentSource, Tint = Tint, TimeScale = TimeScale, Parameters = new Dictionary<string, Vector4>(Parameters), Owner = newOwner,
    };
}
