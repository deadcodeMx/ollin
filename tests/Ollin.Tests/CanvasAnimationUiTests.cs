using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Rendering;
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

    [Fact]
    public void UiCanvasAplanaContenedores()
    {
        var canvas = new UiCanvas();
        canvas.Elements.Add(new UiElement { Id = "menu", Kind = UiElementKind.Container, X = 100, Y = 50, Layout = UiLayout.Vertical, Padding = 10, Gap = 4 });
        canvas.Elements.Add(new UiElement { Id = "a", Kind = UiElementKind.Button, ParentId = "menu", X = 0, Y = 0, Height = 30 });
        canvas.Elements.Add(new UiElement { Id = "b", Kind = UiElementKind.Button, ParentId = "menu", X = 0, Y = 0, Height = 30 });

        var flat = canvas.Flatten();
        Assert.Equal(110f, flat[1].X);
        Assert.Equal(60f, flat[1].Y);
        Assert.Equal(94f, flat[2].Y);
    }

    [Fact]
    public void UiCanvasAplanaContenedoresAnidadosYConservaTooltip()
    {
        var canvas = new UiCanvas();
        canvas.Elements.Add(new UiElement
        {
            Id = "menu", Kind = UiElementKind.Container, X = 100, Y = 50,
            Layout = UiLayout.Vertical, Padding = 10, Gap = 4,
        });
        canvas.Elements.Add(new UiElement
        {
            Id = "grupo", Kind = UiElementKind.Container, ParentId = "menu",
            X = 0, Y = 0, Height = 30, Layout = UiLayout.Vertical, Padding = 5, Gap = 2,
        });
        canvas.Elements.Add(new UiElement
        {
            Id = "boton", Kind = UiElementKind.Button, ParentId = "grupo",
            X = 0, Y = 0, Height = 20, Tooltip = "Abrir opciones",
        });

        var flat = canvas.Flatten();
        var button = flat.Single(element => element.Id == "boton");

        Assert.Equal(115f, button.X);
        Assert.Equal(65f, button.Y);
        Assert.Equal("Abrir opciones", ((UiCanvas)canvas.CloneFor(new Entity())).Elements[2].Tooltip);
    }

    [Fact]
    public void UiCanvasLayoutGridColocaEnFilasYColumnas()
    {
        var canvas = new UiCanvas();
        canvas.Elements.Add(new UiElement
        {
            Id = "grid", Kind = UiElementKind.Container, X = 100, Y = 50,
            Layout = UiLayout.Grid, GridColumns = 2, Padding = 0, Gap = 4,
        });
        for (var i = 1; i <= 3; i++)
            canvas.Elements.Add(new UiElement
            {
                Id = $"b{i}", Kind = UiElementKind.Button, ParentId = "grid",
                Width = 20, Height = 20,
            });

        var flat = canvas.Flatten();
        Assert.Equal(100f, flat[1].X);
        Assert.Equal(50f, flat[1].Y);
        Assert.Equal(124f, flat[2].X);
        Assert.Equal(50f, flat[2].Y);
        Assert.Equal(100f, flat[3].X);
        Assert.Equal(74f, flat[3].Y);
    }

    [Fact]
    public void AnimacionDisparaEventosYAplicaEasing()
    {
        var scene = new Scene();
        var entity = scene.Create("Objeto", Vector2.Zero);
        var player = entity.AddComponent(new AnimationPlayer2D
        {
            Clips = new()
            {
                new AnimationTimeline
                {
                    Name = "fx", Duration = 1f, Loop = false,
                    Tracks = new()
                    {
                        new AnimationTrack
                        {
                            Property = "position",
                            Keys = new()
                            {
                                new AnimationKeyframe { Time = 0f, Value = Vector2.Zero, Easing = AnimationEasing.Linear },
                                new AnimationKeyframe { Time = 1f, Value = new Vector2(10f, 0f), Easing = AnimationEasing.EaseOut },
                            },
                        },
                        new AnimationTrack
                        {
                            Property = "event",
                            Keys = new() { new AnimationKeyframe { Time = 0.5f, Text = "hit" } },
                        },
                    },
                },
            },
        });
        var fired = false;
        player.Event += name => fired = name == "hit";
        player.Play("fx");
        player.OnUpdate(0.5f);
        Assert.True(fired);
        Assert.Equal(7.5f, entity.Transform.Position.X, 2);
    }

    [Fact]
    public void TriangulaPoligonoRelleno()
    {
        var points = new List<Vector2> { new(-1f, -1f), new(1f, -1f), new(1f, 1f), new(-1f, 1f) };
        var triangles = PolygonTriangulator.Triangulate(points);
        Assert.Equal(2, triangles.Count);
    }

    [Fact]
    public void CrossFadeMezclaPosiciones()
    {
        var scene = new Scene();
        var entity = scene.Create("Objeto", Vector2.Zero);
        var player = entity.AddComponent(new AnimationPlayer2D
        {
            Clips = new()
            {
                new AnimationTimeline { Name = "a", Duration = 1f, Loop = false, Tracks = new() { new AnimationTrack { Property = "position", Keys = new() { new AnimationKeyframe { Time = 0f, Value = Vector2.Zero }, new AnimationKeyframe { Time = 1f, Value = new Vector2(10f, 0f) } } } } },
                new AnimationTimeline { Name = "b", Duration = 1f, Loop = false, Tracks = new() { new AnimationTrack { Property = "position", Keys = new() { new AnimationKeyframe { Time = 0f, Value = new Vector2(20f, 0f) }, new AnimationKeyframe { Time = 1f, Value = new Vector2(20f, 0f) } } } } },
            },
        });
        player.Play("a");
        player.OnUpdate(0.5f);
        player.CrossFade("b", 0.5f);
        player.OnUpdate(0.25f);
        Assert.InRange(entity.Transform.Position.X, 12.4f, 12.6f);
    }

    [Fact]
    public void EsqueletoGeneraPoseDeReposo()
    {
        var scene = new Scene();
        var entity = scene.Create("Rig", Vector2.Zero);
        var skeleton = entity.AddComponent(new Skeleton2D());
        skeleton.Solve();
        Assert.Equal(3, skeleton.BindPoints.Count);
        Assert.Equal(3, skeleton.WorldPoints.Count);
    }

    [Fact]
    public void TangentesCurvanLaInterpolacion()
    {
        var scene = new Scene();
        var entity = scene.Create("Objeto", Vector2.Zero);
        var player = entity.AddComponent(new AnimationPlayer2D
        {
            Clips = new()
            {
                new AnimationTimeline { Name = "curve", Duration = 1f, Loop = false, Tracks = new() { new AnimationTrack { Property = "position", Keys = new() { new AnimationKeyframe { Time = 0f, Value = Vector2.Zero, OutTangent = new Vector2(10f, 0f) }, new AnimationKeyframe { Time = 1f, Value = new Vector2(10f, 0f) } } } } },
            },
        });
        player.Play("curve");
        player.OnUpdate(0.5f);
        Assert.InRange(entity.Transform.Position.X, 6.2f, 6.3f);
    }

    [Fact]
    public void Mesh2DClonaSuGeometria()
    {
        var mesh = new Mesh2D { Mesh = new Mesh2DData { Vertices = new() { Vector2.Zero, Vector2.UnitX, Vector2.UnitY }, Triangles = new() { 0, 1, 2 } } };
        var copy = (Mesh2D)mesh.CloneFor(new Entity());
        Assert.Equal(3, copy.Mesh.Vertices.Count);
        Assert.Equal(3, copy.Mesh.Triangles.Count);
    }

    [Fact]
    public void SkinnedMeshConservaCuatroPesos()
    {
        var mesh = new SkinnedMesh2D { Vertices = new() { Vector2.Zero }, BoneA = new() { 0 }, BoneB = new() { 1 }, BoneC = new() { 2 }, BoneD = new() { 0 }, Weight = new() { 0.25f }, WeightC = new() { 0.25f }, WeightD = new() { 0.1f } };
        var copy = (SkinnedMesh2D)mesh.CloneFor(new Entity());
        Assert.Equal(4, copy.BoneA.Count + copy.BoneB.Count + copy.BoneC.Count + copy.BoneD.Count);
        Assert.Equal(0.25f, copy.WeightC[0]);
    }

    [Fact]
    public void BlendTreeMezclaDosClips()
    {
        var scene = new Scene();
        var entity = scene.Create("Objeto", Vector2.Zero);
        entity.AddComponent(new AnimationPlayer2D { Clips = new()
        {
            new AnimationTimeline { Name = "a", Duration = 1f, Tracks = new() { new AnimationTrack { Property = "position", Keys = new() { new AnimationKeyframe { Time = 0f, Value = Vector2.Zero }, new AnimationKeyframe { Time = 1f, Value = Vector2.Zero } } } } },
            new AnimationTimeline { Name = "b", Duration = 1f, Tracks = new() { new AnimationTrack { Property = "position", Keys = new() { new AnimationKeyframe { Time = 0f, Value = new Vector2(10f, 0f) }, new AnimationKeyframe { Time = 1f, Value = new Vector2(10f, 0f) } } } } },
        } });
        var tree = entity.AddComponent(new AnimationBlendTree2D { Entries = new() { new AnimationBlendEntry { Clip = "a", Weight = 0.5f }, new AnimationBlendEntry { Clip = "b", Weight = 0.5f } } });
        tree.OnUpdate(1f / 60f);
        Assert.InRange(entity.Transform.Position.X, 4.9f, 5.1f);
    }

    [Fact]
    public void CanvasItemCombinaModulate()
    {
        var item = new CanvasItem2D { Modulate = new Vector4(0.5f, 1f, 1f, 1f), SelfModulate = new Vector4(1f, 0.5f, 1f, 1f) };
        var copy = (CanvasItem2D)item.CloneFor(new Entity());
        Assert.Equal(0.5f, copy.EffectiveModulate.X);
        Assert.Equal(0.5f, copy.EffectiveModulate.Y);
    }

    [Fact]
    public void SubViewportConservaRectangulo()
    {
        var viewport = new SubViewport2D { CameraTag = "minimap", Rect = new Vector4(0.6f, 0.6f, 0.3f, 0.3f) };
        var copy = (SubViewport2D)viewport.CloneFor(new Entity());
        Assert.Equal("minimap", copy.CameraTag);
        Assert.Equal(0.3f, copy.Rect.Z);
    }

    [Fact]
    public void CanvasMaterialConservaShader()
    {
        var material = new CanvasMaterial2D { FragmentSource = "void main(){}", Tint = new Vector4(1f, 0.5f, 1f, 1f), Parameters = new() { ["uTime"] = new Vector4(0.25f, 0f, 0f, 0f) } };
        var copy = (CanvasMaterial2D)material.CloneFor(new Entity());
        Assert.Equal("void main(){}", copy.FragmentSource);
        Assert.Equal(0.5f, copy.Tint.Y);
        Assert.Equal(0.25f, copy.Parameters["uTime"].X);
    }
}
