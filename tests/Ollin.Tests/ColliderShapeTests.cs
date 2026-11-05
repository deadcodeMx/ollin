using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public sealed class ColliderShapeTests
{
    [Fact]
    public void LaCajaTieneCuatroEsquinas()
    {
        var box = new BoxCollider { Size = new Vector2(2f, 1f) };
        Assert.Equal(4, box.GetWorldCorners(Matrix4x4.Identity).Length);
    }

    [Fact]
    public void CilindroYConoTienenFormaPropia()
    {
        Assert.True(new CylinderCollider2D().GetWorldCorners(Matrix4x4.Identity).Length > 4);
        Assert.Equal(3, new ConeCollider2D().GetWorldCorners(Matrix4x4.Identity).Length);
    }

    [Fact]
    public void LosNuevosCollidersSeSerializan()
    {
        var scene = new Scene();
        var entity = scene.Create("Forma", Vector2.Zero);
        entity.AddComponent(new ConeCollider2D { Radius = 2f, Height = 3f });
        var json = Ollin.Core.Serialization.SceneSerializer.Serialize(scene);
        var loaded = Ollin.Core.Serialization.SceneSerializer.Deserialize(json);
        Assert.NotNull(loaded.Find("Forma")?.GetComponent<ConeCollider2D>());
    }

    [Fact]
    public void ElSpriteConservaSuForma()
    {
        var scene = new Scene();
        var entity = scene.Create("Sprite", Vector2.Zero);
        entity.AddComponent(new SpriteRenderer { Shape = SpriteShape2D.Cone });
        var json = Ollin.Core.Serialization.SceneSerializer.Serialize(scene);
        var loaded = Ollin.Core.Serialization.SceneSerializer.Deserialize(json);
        Assert.Equal(SpriteShape2D.Cone, loaded.Find("Sprite")?.GetComponent<SpriteRenderer>()?.Shape);
    }
}