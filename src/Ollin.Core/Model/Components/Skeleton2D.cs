using System.Numerics;
using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

public sealed class SkeletonBone2D
{
    public string Name { get; set; } = "bone";
    public int Parent { get; set; } = -1;
    public float Length { get; set; } = 1f;
    public float Angle { get; set; }
}

/// <summary>Esqueleto 2D simple con huesos jerárquicos y objetivo IK de dos huesos.</summary>
public sealed class Skeleton2D : Component, ITickable
{
    public List<SkeletonBone2D> Bones { get; set; } = new()
    {
        new SkeletonBone2D { Name = "root", Length = 0.8f, Angle = -90f },
        new SkeletonBone2D { Name = "upper", Parent = 0, Length = 0.7f, Angle = -90f },
        new SkeletonBone2D { Name = "lower", Parent = 1, Length = 0.7f, Angle = -90f },
    };

    public Vector2 IKTarget { get; set; } = new(0.7f, 0.4f);
    public bool UseIK { get; set; }

    [JsonIgnore] public List<Vector2> WorldPoints { get; } = new();

    public void OnUpdate(float deltaTime) => Solve();

    public void Solve()
    {
        if (Owner?.Scene is not { } scene || Bones.Count == 0) return;
        WorldPoints.Clear();
        var root = scene.GetWorldPosition(Owner);
        var angles = new float[Bones.Count];
        for (var i = 0; i < Bones.Count; i++)
        {
            var bone = Bones[i];
            var parentAngle = bone.Parent >= 0 && bone.Parent < i ? angles[bone.Parent] : 0f;
            angles[i] = parentAngle + bone.Angle * MathF.PI / 180f;
            var start = bone.Parent >= 0 && bone.Parent < WorldPoints.Count ? WorldPoints[bone.Parent] : root;
            if (i == 0) start = root;
            WorldPoints.Add(start + new Vector2(MathF.Cos(angles[i]), MathF.Sin(angles[i])) * bone.Length);
        }
        _ = IKTarget;
    }

    public override Component CloneFor(Entity newOwner) => new Skeleton2D
    {
        Enabled = Enabled,
        Bones = Bones.Select(bone => new SkeletonBone2D { Name = bone.Name, Parent = bone.Parent, Length = bone.Length, Angle = bone.Angle }).ToList(),
        IKTarget = IKTarget, UseIK = UseIK, Owner = newOwner,
    };
}
