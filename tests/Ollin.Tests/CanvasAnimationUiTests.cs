using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class CanvasAnimationUiTests
{
    [Fact]
    public void UiCanvasAgregaYBuscasElementos()
    {
        var canvas = new UiCanvas();
        canvas.Add("vida", UiElementKind.Bar, "Vida", 10, 10);
        canvas.Add("jugar", UiElementKind.Button, "Jugar", 10, 60);

        Assert.Equal(2, canvas.Elements.Count);
        Assert.Equal(UiElementKind.Button, canvas.Find("jugar")!.Kind);
    }

    [Fact]
    public void AnimationPlayerInterpolaPosicion()
    {
        var scene = new Scene();
        var entity = scene.Create("Objeto", Vector2.Zero);
        var player = entity.AddComponent(new AnimationPlayer2D
        {
            Clips = new()
            {
                new AnimationTimeline
                {
                    Name = "move",
                    Duration = 1f,
                    Loop = false,
                    Tracks = new()
                    {
                        new AnimationTrack
                        {
                            Property = "position",
                            Keys = new()
                            {
                                new AnimationKeyframe { Time = 0f, Value = Vector2.Zero },
                                new AnimationKeyframe { Time = 1f, Value = new Vector2(10f, 0f) },
                            },
                        },
                    },
                },
            },
        });

        player.Play("move");
        player.OnUpdate(0.5f);

        Assert.Equal(5f, entity.Transform.Position.X, 2);
    }

    [Fact]
    public void CanvasLayerConservaOrdenYVisible()
    {
        var entity = new Entity();
        var layer = entity.AddComponent(new CanvasLayer { Order = 3, YSort = true, Visible = false });
        var copy = (CanvasLayer)layer.CloneFor(new Entity());

        Assert.Equal(3, copy.Order);
        Assert.True(copy.YSort);
        Assert.False(copy.Visible);
    }

    [Fact]
    public void SkeletonIKLlegaAlObjetivo()
    {
        var scene = new Scene();
        var entity = scene.Create("Rig", Vector2.Zero);
        var skeleton = entity.AddComponent(new Skeleton2D { UseIK = true, IKTarget = new Vector2(1f, 0f) });
        skeleton.Solve();
        Assert.Equal(3, skeleton.WorldPoints.Count);
        Assert.InRange(Vector2.Distance(skeleton.WorldPoints[2], new Vector2(1f, 0f)), 0f, 0.05f);
    }
}
