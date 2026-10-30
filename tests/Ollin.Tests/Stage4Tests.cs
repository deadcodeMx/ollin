using System.Numerics;
using Ollin.Core.Diagnostics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Ollin.Scripting;
using Xunit;

namespace Ollin.Tests;

public class TilemapTests
{
    [Fact]
    public void EscribeYLeeMosaicosYConvierteCoordenadas()
    {
        var map = new Tilemap { Width = 4, Height = 3, TileSize = 1f };

        map.SetTile(2, 1, 7);

        Assert.Equal(7, map.TileAt(2, 1));
        Assert.Equal(-1, map.TileAt(0, 0));
        Assert.Equal(-1, map.TileAt(9, 9));

        // Y del mundo crece hacia arriba; la fila 0 está arriba.
        Assert.Equal((0, 0), map.WorldToCell(new Vector2(0.2f, -0.2f)));
        Assert.Equal((2, 1), map.WorldToCell(new Vector2(2.5f, -1.5f)));
    }

    [Fact]
    public void RedimensionarConservaLoQueCabe()
    {
        var map = new Tilemap { Width = 2, Height = 2 };
        map.SetTile(0, 0, 3);
        map.SetTile(1, 1, 5);

        map.Resize(4, 4);

        Assert.Equal(4, map.Width);
        Assert.Equal(3, map.TileAt(0, 0));
        Assert.Equal(5, map.TileAt(1, 1));
        Assert.Equal(-1, map.TileAt(3, 3));
    }
}

public class AnimationTests
{
    [Fact]
    public void ElAnimadorAvanzaCuadrosYRecortaLaTextura()
    {
        var scene = new Scene();
        var entity = scene.Create("Héroe", Vector2.Zero);
        var sprite = entity.AddComponent(new SpriteRenderer { TexturePath = "assets/sprites/hero-walk.png" });
        var animator = entity.AddComponent(new SpriteAnimator
        {
            TexturePath = "assets/sprites/hero-walk.png",
            FrameWidth = 32,
            FrameHeight = 32,
            Columns = 4,
            FrameCount = 4,
            Fps = 10f,
            Loop = true,
        });

        animator.OnUpdate(0.05f); // medio cuadro
        Assert.Equal(0, animator.CurrentFrame);

        animator.OnUpdate(0.10f); // 1.5 cuadros
        Assert.Equal(1, animator.CurrentFrame);
        Assert.Equal(32f, sprite.FrameRect.X);
        Assert.Equal(32f, sprite.FrameRect.Z);

        animator.OnUpdate(0.5f); // vuelve a empezar
        Assert.InRange(animator.CurrentFrame, 0, 3);
        Assert.Equal(0f, sprite.FrameRect.Y);
    }

    [Fact]
    public void SinBucleSeDetieneEnElUltimoCuadro()
    {
        var scene = new Scene();
        var entity = scene.Create("Héroe", Vector2.Zero);
        entity.AddComponent(new SpriteRenderer());
        var animator = entity.AddComponent(new SpriteAnimator { FrameCount = 3, Fps = 10f, Loop = false });

        animator.OnUpdate(1f);

        Assert.Equal(2, animator.CurrentFrame);
        Assert.False(animator.Playing);
    }
}

public class PhysicsTests
{
    private static SceneRuntime CreateRuntime(out Scene scene)
    {
        scene = new Scene();
        return new SceneRuntime();
    }

    [Fact]
    public void UnCuerpoConVelocidadSeDetieneContraUnSolido()
    {
        var scene = new Scene();

        var wall = scene.Create("Muro", new Vector2(2f, 0f));
        wall.AddComponent(new BoxCollider { Size = new Vector2(1f, 4f) });
        wall.AddComponent(new Rigidbody2D { IsStatic = true });

        var player = scene.Create("Jugador", Vector2.Zero);
        player.AddComponent(new BoxCollider { Size = new Vector2(1f, 1f) });
        player.AddComponent(new Rigidbody2D { Velocity = new Vector2(6f, 0f) });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        for (var i = 0; i < 20; i++)
        {
            runtime.Tick(0.05f);
        }

        // El jugador avanza y se queda pegado al muro (sin atravesarlo).
        Assert.InRange(player.Transform.Position.X, 0.6f, 1.55f);
    }

    [Fact]
    public void UnDisparadorAvisaAlScript()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "physics-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scripts"));
        File.WriteAllText(Path.Combine(root, "scripts", "Trigger.cs"), """
            using Ollin.Scripting;

            public sealed class Trigger : OllinScript
            {
                public override void OnTriggerEnter(Ollin.Core.Model.Entity other)
                {
                    Entity.Transform.Rotation = 1.5f;
                    Debug("trigger con " + other.Name);
                }
            }
            """);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            Assert.True(workspace.Reload().Success);

            var scene = new Scene();
            var zone = scene.Create("Zona", new Vector2(1f, 0f));
            zone.AddComponent(new BoxCollider { Size = new Vector2(2f, 2f), IsTrigger = true });
            zone.AddComponent(new ScriptComponent { ScriptPath = "scripts/Trigger.cs", ClassName = "Trigger" });

            var player = scene.Create("Jugador", new Vector2(-2f, 0f));
            player.AddComponent(new BoxCollider { Size = new Vector2(1f, 1f) });
            player.AddComponent(new Rigidbody2D { Velocity = new Vector2(8f, 0f) });

            var runtime = new SceneRuntime();
            runtime.Play(scene, workspace);

            for (var i = 0; i < 20 && zone.Transform.Rotation == 0f; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.Equal(1.5f, zone.Transform.Rotation, 3);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

public class CoroutineTests
{
    [Fact]
    public void WaitForSecondsRetrasaLaRutina()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "coroutine-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scripts"));
        File.WriteAllText(Path.Combine(root, "scripts", "Delayed.cs"), """
            using System.Collections;
            using Ollin.Scripting;

            public sealed class Delayed : OllinScript
            {
                public override void OnStart()
                {
                    StartCoroutine(Run());
                }

                private IEnumerator Run()
                {
                    yield return new WaitForSeconds(0.2f);
                    Entity.Transform.Position = new System.Numerics.Vector2(5f, 0f);
                }
            }
            """);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            workspace.Reload();

            var scene = new Scene();
            var entity = scene.Create("Entidad", Vector2.Zero);
            entity.AddComponent(new ScriptComponent { ScriptPath = "scripts/Delayed.cs", ClassName = "Delayed" });

            var runtime = new SceneRuntime();
            runtime.Play(scene, workspace);

            runtime.Tick(0.05f);
            runtime.Tick(0.05f);
            Assert.Equal(0f, entity.Transform.Position.X, 3);

            for (var i = 0; i < 6; i++)
            {
                runtime.Tick(0.05f);
            }

            Assert.Equal(5f, entity.Transform.Position.X, 3);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

public class DebugDrawTests
{
    [Fact]
    public void LasFigurasSeAcumulanYSeLimpian()
    {
        DebugDraw.Clear();
        DebugDraw.Line(Vector2.Zero, new Vector2(1, 1));
        DebugDraw.Rect(Vector2.Zero, new Vector2(2, 2));

        var snapshot = DebugDraw.Snapshot();
        Assert.Equal(5, snapshot.Length);

        DebugDraw.Clear();
        Assert.Empty(DebugDraw.Snapshot());
    }
}
