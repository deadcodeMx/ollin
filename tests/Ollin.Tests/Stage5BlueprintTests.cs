using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Prefabs;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class BlueprintTests
{
    [Fact]
    public void GuardaLaJerarquiaYLaInstanciaConIdsNuevos()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "blueprint-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(folder, "torre.blueprint.json");

        try
        {
            var scene = new Scene();
            var root = scene.Create("Torre", new Vector2(1f, 1f));
            root.Tags.Add("torre");
            root.AddComponent(new Inventory { Capacity = 4 });

            var child = scene.Create("Cima", new Vector2(2f, 1f), root.Id);
            child.AddComponent(new Light2D { Radius = 3f });

            BlueprintSerializer.Save(scene, root, path);

            var document = BlueprintSerializer.Load(path);
            var target = new Scene();
            var instance = BlueprintSerializer.Instantiate(target, document, new Vector2(5f, 5f));

            Assert.Equal(2, target.Entities.Count);
            Assert.Equal(new Vector2(5f, 5f), instance.Transform.Position);
            Assert.NotEqual(root.Id, instance.Id);
            Assert.Contains("torre", instance.Tags);
            Assert.NotNull(instance.GetComponent<Inventory>());

            var childInstance = target.Find("Cima")!;
            Assert.Equal(instance.Id, childInstance.ParentId);
            Assert.Equal(new Vector2(2f, 1f), childInstance.Transform.Position);
            Assert.NotNull(childInstance.GetComponent<Light2D>());
        }
        finally
        {
            if (Directory.Exists(folder))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
    }

    [Fact]
    public void NormalizaLaExtensionDelBlueprint()
    {
        Assert.Equal("torre.blueprint.json", BlueprintSerializer.NormalizePath("torre"));
        Assert.Equal("torre.blueprint.json", BlueprintSerializer.NormalizePath("torre.json"));
        Assert.Equal("torre.blueprint.json", BlueprintSerializer.NormalizePath("torre.blueprint.json"));
    }
}

public class PhysicsStabilityTests
{
    [Fact]
    public void UnCuerpoRapidoNoAtraviesaElMuro()
    {
        var scene = new Scene();

        var wall = scene.Create("Muro", new Vector2(2f, 0f));
        wall.AddComponent(new BoxCollider { Size = new Vector2(1f, 4f) });
        wall.AddComponent(new Rigidbody2D { IsStatic = true });

        var player = scene.Create("Jugador", Vector2.Zero);
        player.AddComponent(new BoxCollider { Size = new Vector2(1f, 1f) });
        player.AddComponent(new Rigidbody2D { Velocity = new Vector2(40f, 0f) });

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        try
        {
            for (var i = 0; i < 30; i++)
            {
                runtime.Tick(0.1f);
            }

            Assert.True(player.Transform.Position.X < 1.6f, $"x={player.Transform.Position.X}");
            Assert.True(player.Transform.Position.X > 0.4f, $"x={player.Transform.Position.X}");
        }
        finally
        {
            runtime.Stop();
        }
    }
}

public class EditorMetadataTests
{
    [Fact]
    public void ElIconoYElColorSeGuardanConLaEscena()
    {
        var scene = new Scene();
        var entity = scene.Create("Nota", Vector2.Zero);
        entity.EditorIcon = "★";
        entity.EditorColor = "#E9A23B";

        var restored = Ollin.Core.Serialization.SceneSerializer.Deserialize(
            Ollin.Core.Serialization.SceneSerializer.Serialize(scene));

        var loaded = restored.Find("Nota")!;
        Assert.Equal("★", loaded.EditorIcon);
        Assert.Equal("#E9A23B", loaded.EditorColor);
    }
}
