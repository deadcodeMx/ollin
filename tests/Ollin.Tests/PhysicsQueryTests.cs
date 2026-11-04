using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Physics;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class PhysicsQueryTests
{
    [Fact]
    public void RaycastImpactaElPrimerCollider()
    {
        var scene = new Scene();
        var wall = scene.Create("Muro", new Vector2(5f, 0f));
        wall.AddComponent(new BoxCollider { Size = Vector2.One });
        var near = scene.Create("Caja", new Vector2(2f, 0f));
        near.AddComponent(new BoxCollider { Size = Vector2.One });

        var hit = PhysicsQuery.Raycast(scene, Vector2.Zero, Vector2.UnitX, 10f);

        Assert.NotNull(hit);
        Assert.Equal("Caja", hit!.Value.Entity.Name);
        Assert.InRange(hit.Value.Point.X, 1.4f, 1.6f);
    }

    [Fact]
    public void RaycastRespetaIgnoreTags()
    {
        var scene = new Scene();
        var wall = scene.Create("Muro", new Vector2(2f, 0f));
        wall.Tags.Add("projectile");
        wall.AddComponent(new BoxCollider { Size = Vector2.One, IgnoreTags = new() { "player" } });

        var hit = PhysicsQuery.Raycast(scene, Vector2.Zero, Vector2.UnitX, 10f, ignore: scene.Create("Player", Vector2.Zero));

        Assert.NotNull(hit);
        Assert.Equal("Muro", hit!.Value.Entity.Name);
    }
}
