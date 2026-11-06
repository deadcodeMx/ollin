using System.Diagnostics;
using System.Numerics;
using Ollin.Core.Diagnostics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Rendering;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public sealed class PerformanceStressTests
{
    [Fact]
    public void RenderServerSometeMilesDeSprites()
    {
        var scene = new Scene();
        for (var i = 0; i < 20_000; i++)
        {
            var entity = scene.Create($"Sprite {i}", new Vector2(i % 200, i / 200));
            entity.AddComponent(new SpriteRenderer
            {
                Size = new Vector2(0.5f, 0.5f),
                SortingOrder = i % 32,
            });
        }

        var server = new RenderServer2D();
        var watch = Stopwatch.StartNew();
        server.Submit(scene);
        watch.Stop();
        Console.WriteLine($"20k sprites: {watch.ElapsedMilliseconds} ms, draw={server.SubmittedSprites}");

        Assert.Equal(20_000, server.SubmittedSprites);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(8), $"Submit tardó {watch.Elapsed}");
    }

    [Fact]
    public void FisicaConQuinientosCuerposNoSeDesborda()
    {
        var scene = new Scene();
        for (var i = 0; i < 500; i++)
        {
            var entity = scene.Create($"Cuerpo {i}", new Vector2(i % 25, i / 25));
            entity.AddComponent(new BoxCollider { Size = new Vector2(0.4f, 0.4f) });
            entity.AddComponent(new Rigidbody2D
            {
                Velocity = new Vector2((i % 5) - 2f, (i % 3) - 1f),
                Mass = 1f,
            });
        }

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        var watch = Stopwatch.StartNew();
        for (var tick = 0; tick < 30; tick++) runtime.Tick(1f / 60f);
        watch.Stop();
        Console.WriteLine($"500 cuerpos × 30 ticks: {watch.ElapsedMilliseconds} ms");

        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(10), $"Física tardó {watch.Elapsed}");
    }

    [Fact]
    public void UiCanvasAplanaDosMilElementos()
    {
        var canvas = new UiCanvas();
        canvas.Elements.Add(new UiElement { Id = "root", Kind = UiElementKind.Container, Layout = UiLayout.Grid, GridColumns = 10 });
        for (var i = 0; i < 2_000; i++)
            canvas.Elements.Add(new UiElement
            {
                Id = $"e{i}", Kind = UiElementKind.Label, ParentId = "root", Text = $"Elemento {i}",
                Width = 80, Height = 24,
            });

        var watch = Stopwatch.StartNew();
        var flat = canvas.Flatten();
        watch.Stop();
        Console.WriteLine($"2k UI elements: {watch.ElapsedMilliseconds} ms");

        Assert.Equal(2_001, flat.Count);
        Assert.True(watch.Elapsed < TimeSpan.FromSeconds(5), $"Flatten tardó {watch.Elapsed}");
    }

    [Fact]
    public void ProfilerMideFramesSinAcumularInfinito()
    {
        Profiler.Reset();
        for (var i = 0; i < 120; i++) Profiler.Tick(1f / 60f);

        Assert.True(Profiler.Fps > 0f);
        Assert.True(Profiler.FrameMs > 0f);
        Assert.Equal(120, Profiler.FrameCount);
    }
}
