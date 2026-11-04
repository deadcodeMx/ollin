using System.Numerics;

namespace Ollin.Core.Model.Components;

public sealed class Mesh2DData
{
    public List<Vector2> Vertices { get; set; } = new();
    public List<int> Triangles { get; set; } = new();
    public Vector4 Color { get; set; } = Vector4.One;
}

/// <summary>Malla 2D estática en espacio local.</summary>
public sealed class Mesh2D : Component
{
    public Mesh2DData Mesh { get; set; } = new();
    public bool Visible { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new Mesh2D
    {
        Enabled = Enabled, Visible = Visible, Owner = newOwner,
        Mesh = new Mesh2DData { Vertices = new(Mesh.Vertices), Triangles = new(Mesh.Triangles), Color = Mesh.Color },
    };
}

/// <summary>Instancias de una misma malla en varios offsets.</summary>
public sealed class MultiMesh2D : Component
{
    public Mesh2DData Mesh { get; set; } = new();
    public List<Vector2> Offsets { get; set; } = new();
    public float Scale { get; set; } = 1f;
    public bool Visible { get; set; } = true;

    public override Component CloneFor(Entity newOwner) => new MultiMesh2D
    {
        Enabled = Enabled, Visible = Visible, Scale = Scale, Offsets = new(Offsets), Owner = newOwner,
        Mesh = new Mesh2DData { Vertices = new(Mesh.Vertices), Triangles = new(Mesh.Triangles), Color = Mesh.Color },
    };
}
