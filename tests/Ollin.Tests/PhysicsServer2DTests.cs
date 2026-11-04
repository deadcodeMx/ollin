using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public sealed class PhysicsServer2DTests
{
    [Fact]
    public void AreaEmiteEntradaYSalida()
    {
        var scene = new Scene();
        var areaEntity = scene.Create("Area", Vector2.Zero);
        var area = areaEntity.AddComponent(new Area2D { Size = new Vector2(2f, 2f) });
        var bodyEntity = scene.Create("Body", new Vector2(0.5f, 0f));
        bodyEntity.AddComponent(new BoxCollider { Size = Vector2.One });
        var entered = 0;
        var exited = 0;
        area.BodyEntered += _ => entered++;
        area.BodyExited += _ => exited++;
        var server = new PhysicsServer2D();

        server.Step(scene);
        bodyEntity.Transform.Position = new Vector2(10f, 0f);
        server.Step(scene);

        Assert.Equal(1, entered);
        Assert.Equal(1, exited);
        Assert.Empty(area.Overlapping);
    }
}
