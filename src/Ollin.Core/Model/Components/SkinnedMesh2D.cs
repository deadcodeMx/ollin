using System.Numerics;

namespace Ollin.Core.Model.Components;

/// <summary>Malla 2D con pesos por hueso; se deforma con Skeleton2D.</summary>
public sealed class SkinnedMesh2D : Component
{
    public List<Vector2> Vertices { get; set; } = new();
    public List<int> Triangles { get; set; } = new();
    public List<int> BoneA { get; set; } = new();
    public List<int> BoneB { get; set; } = new();
    public List<float> Weight { get; set; } = new();
    public Vector4 Color { get; set; } = Vector4.One;
    public bool Visible { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new SkinnedMesh2D
    {
        Enabled = Enabled, Vertices = new List<Vector2>(Vertices), Triangles = new List<int>(Triangles),
        BoneA = new List<int>(BoneA), BoneB = new List<int>(BoneB), Weight = new List<float>(Weight),
        Color = Color, Visible = Visible, Owner = newOwner,
    };
}
