using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;
using Xunit;

namespace Ollin.Tests;

public class SceneSerializationTests
{
    [Fact]
    public void RoundtripConservaEntidadesYComponentes()
    {
        var scene = DefaultScene.Create();

        var json = SceneSerializer.Serialize(scene);
        var loaded = SceneSerializer.Deserialize(json);

        Assert.Equal(scene.Entities.Count, loaded.Entities.Count);

        var player = loaded.Find("Jugador");
        Assert.NotNull(player);
        Assert.NotNull(player!.Scene);
        Assert.Same(loaded, player.Scene);

        var sprite = player.GetComponent<SpriteRenderer>();
        Assert.NotNull(sprite);
        Assert.Equal(1.1f, sprite!.Size.X, 3);
        Assert.Equal(new Vector4(0.91f, 0.64f, 0.23f, 1f), sprite.Tint);

        Assert.NotNull(loaded.GetMainCamera());
    }

    [Fact]
    public void GuardarYCargarArchivoMantieneLaEscena()
    {
        var directory = Path.Combine(AppContext.BaseDirectory, "scene-tests", Guid.NewGuid().ToString("N"));
        var path = Path.Combine(directory, "escena.ollin.json");

        try
        {
            var scene = DefaultScene.Create();
            SceneSerializer.Save(scene, path);
            Assert.True(File.Exists(path));

            var loaded = SceneSerializer.Load(path);
            Assert.Equal(scene.Entities.Count, loaded.Entities.Count);
            Assert.Equal("Cámara principal", loaded.GetMainCamera()!.Owner!.Name);
        }
        finally
        {
            if (Directory.Exists(directory))
            {
                Directory.Delete(directory, recursive: true);
            }
        }
    }

    [Fact]
    public void ElJsonEsLegibleYUsaDiscriminadorDeTipo()
    {
        var json = SceneSerializer.Serialize(DefaultScene.Create());

        Assert.Contains("\"$type\": \"sprite\"", json);
        Assert.Contains("\"$type\": \"camera\"", json);
        Assert.Contains("\"formatVersion\": 1", json);
    }
}
