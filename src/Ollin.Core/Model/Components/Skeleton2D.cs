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

    /// <summary>1 o -1 para elegir el lado del codo.</summary>
    public int BendDirection { get; set; } = 1;

    [JsonIgnore] public List<Vector2> WorldPoints { get; } = new();
    [JsonIgnore] public List<Vector2> BindPoints { get; } = new();

    public void OnUpdate(float deltaTime) => Solve();

    public void Solve()
    {
        if (Owner?.Scene is not { } scene || Bones.Count == 0) return;
        WorldPoints.Clear();
        var root = scene.GetWorldPosition(Owner);
        var angles = new float[Bones.Count];
        var bindAngles = new float[Bones.Count];
        for (var i = 0; i < Bones.Count; i++)
        {
            var bone = Bones[i];
            var parentAngle = bone.Parent >= 0 && bone.Parent < i ? angles[bone.Parent] : 0f;
            angles[i] = parentAngle + bone.Angle * MathF.PI / 180f;
            var start = bone.Parent >= 0 && bone.Parent < WorldPoints.Count ? WorldPoints[bone.Parent] : root;
            if (i == 0) start = root;
            WorldPoints.Add(start + new Vector2(MathF.Cos(angles[i]), MathF.Sin(angles[i])) * bone.Length);

            var bindParent = bone.Parent >= 0 && bone.Parent < i ? bindAngles[bone.Parent] : 0f;
            bindAngles[i] = bindParent + bone.Angle * MathF.PI / 180f;
            var bindStart = bone.Parent >= 0 && bone.Parent < BindPoints.Count ? BindPoints[bone.Parent] : root;
            if (i == 0) bindStart = root;
            BindPoints.Add(bindStart + new Vector2(MathF.Cos(bindAngles[i]), MathF.Sin(bindAngles[i])) * bone.Length);
        }        if (UseIK && Bones.Count >= 3) SolveTwoBone(root);
    }

    private void SolveTwoBone(Vector2 root)
    {
        var upper = Bones[1];
        var lower = Bones[2];
        var rootEnd = WorldPoints.Count > 0 ? WorldPoints[0] : root;
        var target = root + IKTarget;
        var a = MathF.Max(0.01f, upper.Length);
        var b = MathF.Max(0.01f, lower.Length);
        var offset = target - rootEnd;
        var distance = System.Math.Clamp(offset.Length(), MathF.Abs(a - b) + 0.01f, a + b - 0.01f);
        var baseAngle = MathF.Atan2(offset.Y, offset.X);
        var cosine = System.Math.Clamp((a * a + distance * distance - b * b) / (2f * a * distance), -1f, 1f);
        var shoulder = MathF.Acos(cosine);
        var upperAngle = baseAngle + MathF.Sign(BendDirection) * shoulder;
        var upperEnd = rootEnd + new Vector2(MathF.Cos(upperAngle), MathF.Sin(upperAngle)) * a;
        WorldPoints[1] = upperEnd;
        WorldPoints[2] = upperEnd + Vector2.Normalize(target - upperEnd) * b;

        for (var i = 3; i < WorldPoints.Count; i++)
        {
            var direction = WorldPoints[i] - WorldPoints[i - 1];
            var offsetFromParent = direction.LengthSquared() > 0.0001f ? Vector2.Normalize(direction) * Bones[i].Length : Vector2.Zero;
            WorldPoints[i] = WorldPoints[i - 1] + offsetFromParent;
        }
    }

    public override Component CloneFor(Entity newOwner) => new Skeleton2D
    {
        Enabled = Enabled,
        Bones = Bones.Select(bone => new SkeletonBone2D { Name = bone.Name, Parent = bone.Parent, Length = bone.Length, Angle = bone.Angle }).ToList(),
        IKTarget = IKTarget, UseIK = UseIK, BendDirection = BendDirection, Owner = newOwner,
    };
}
