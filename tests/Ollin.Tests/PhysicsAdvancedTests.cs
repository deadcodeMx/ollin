using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Physics;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public sealed class PhysicsAdvancedTests
{
    [Fact]
    public void LasCapasYMasmarasSeRespetanEnAmbosSentidos()
    {
        var a = new BoxCollider { Layer = 1, Mask = 1 << 2 };
        var b = new BoxCollider { Layer = 2, Mask = 1 << 1 };
        var c = new BoxCollider { Layer = 2, Mask = 1 << 3 };

        Assert.True(a.CanCollideWith(b));
        Assert.True(b.CanCollideWith(a));
        Assert.False(a.CanCollideWith(c));
    }

    [Fact]
    public void OverlapCircleDetectaPoligonoYBorde()
    {
        var scene = new Scene();
        var polygon = scene.Create("Poligono", new Vector2(3f, 0f));
        polygon.AddComponent(new PolygonCollider2D
        {
            Points = new List<Vector2>
            {
                new(-1f, 0f), new(0f, 1f), new(1f, 0f), new(0f, -1f),
            },
        });
        var edge = scene.Create("Borde", new Vector2(0f, 4f));
        edge.AddComponent(new EdgeCollider2D
        {
            Points = new List<Vector2> { new(-1f, 0f), new(1f, 0f) },
            Thickness = 0.2f,
        });

        Assert.Contains(polygon, PhysicsQuery.OverlapCircle(scene, new Vector2(3f, 0f), 0.2f));
        Assert.Contains(edge, PhysicsQuery.OverlapCircle(scene, new Vector2(0f, 4f), 0.2f));
        Assert.Empty(PhysicsQuery.OverlapCircle(scene, new Vector2(20f, 20f), 0.2f));
    }

    [Fact]
    public void OverlapBoxRespetaLaMascaraDeConsulta()
    {
        var scene = new Scene();
        var visible = scene.Create("Visible", Vector2.Zero);
        visible.AddComponent(new BoxCollider { Size = new Vector2(2f, 2f), Layer = 1 });
        var hidden = scene.Create("Oculta", Vector2.Zero);
        hidden.AddComponent(new BoxCollider { Size = new Vector2(2f, 2f), Layer = 2 });

        var hits = PhysicsQuery.OverlapBox(scene, Vector2.Zero, new Vector2(1f, 1f), layerMask: 1 << 1);

        Assert.Single(hits);
        Assert.Equal("Visible", hits[0].Name);
    }

    [Fact]
    public void RaycastAllDevuelveImpactosOrdenadosYShapeCastDetecta()
    {
        var scene = new Scene();
        for (var i = 1; i <= 3; i++)
        {
            var wall = scene.Create($"Muro {i}", new Vector2(i * 2f, 0f));
            wall.AddComponent(new BoxCollider { Size = new Vector2(0.5f, 2f) });
        }

        var hits = PhysicsQuery.RaycastAll(scene, Vector2.Zero, Vector2.UnitX, 10f);
        var cast = PhysicsQuery.ShapeCast(scene, Vector2.Zero, new Vector2(0.5f, 0.5f), Vector2.UnitX, 10f);

        Assert.Equal(3, hits.Count);
        Assert.True(hits[0].Distance < hits[1].Distance);
        Assert.NotNull(cast);
    }

    [Fact]
    public void DistanceJointYRodillaLimitanLaSeparacion()
    {
        var scene = new Scene();
        var target = scene.Create("Ancla", Vector2.Zero);
        var body = scene.Create("Cuerpo", new Vector2(3f, 0f));
        body.AddComponent(new Rigidbody2D());
        body.AddComponent(new DistanceJoint2D { TargetId = target.Id, Distance = 1f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(0.016f);

        Assert.InRange(Vector2.Distance(body.Transform.Position, target.Transform.Position), 0.9f, 1.1f);
    }

    [Fact]
    public void RopeJointNoEmpujaCuandoEstaCerca()
    {
        var scene = new Scene();
        var target = scene.Create("Ancla", Vector2.Zero);
        var body = scene.Create("Cuerpo", new Vector2(0.5f, 0f));
        body.AddComponent(new Rigidbody2D());
        body.AddComponent(new RopeJoint2D { TargetId = target.Id, MaxDistance = 2f });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(0.016f);

        Assert.Equal(0.5f, body.Transform.Position.X, 3);
    }

    [Fact]
    public void TriggerConCapasNoRepiteEntradaYEmiteSalida()
    {
        var scene = new Scene();
        var areaEntity = scene.Create("Area", Vector2.Zero);
        var area = areaEntity.AddComponent(new Area2D
        {
            Size = new Vector2(2f, 2f), Layer = 1, Mask = 1 << 2,
        });
        var bodyEntity = scene.Create("Body", new Vector2(0.5f, 0f));
        bodyEntity.AddComponent(new BoxCollider { Size = Vector2.One, Layer = 2, Mask = 1 << 1 });
        var entered = 0;
        var exited = 0;
        area.BodyEntered += _ => entered++;
        area.BodyExited += _ => exited++;
        var server = new PhysicsServer2D();

        server.Step(scene);
        server.Step(scene);
        Assert.Equal(1, entered);
        bodyEntity.Transform.Position = new Vector2(10f, 0f);
        server.Step(scene);

        Assert.Equal(1, exited);
    }

    [Fact]
    public void CcdEvitaAtravesarUnMuroConVelocidadAlta()
    {
        var scene = new Scene();
        var wall = scene.Create("Muro", new Vector2(5f, 0f));
        wall.AddComponent(new BoxCollider { Size = new Vector2(0.5f, 5f) });

        var body = scene.Create("Rapido", Vector2.Zero);
        body.AddComponent(new BoxCollider { Size = new Vector2(0.5f, 0.5f) });
        body.AddComponent(new Rigidbody2D { Velocity = new Vector2(100f, 0f), ContinuousCollision = true });

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(0.1f);

        Assert.True(body.Transform.Position.X < 5f);
    }
}
