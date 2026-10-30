using System.Numerics;
using Ollin.Core.History;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public class SceneHistoryTests
{
    private static Scene SceneWith(float x)
    {
        var scene = new Scene { Name = "Prueba" };
        scene.Create("Entidad", new Vector2(x, 0f));
        return scene;
    }

    [Fact]
    public void DeshacerVuelveAlEstadoAnterior()
    {
        var scene = SceneWith(1f);
        var history = new SceneHistory();
        history.Initialize(scene);

        scene.Find("Entidad")!.Transform.Position = new Vector2(5f, 0f);
        history.Record(scene, "Mover");

        Assert.True(history.CanUndo);
        Assert.False(history.CanRedo);

        var undo = history.Undo();

        Assert.NotNull(undo);
        var restored = Ollin.Core.Serialization.SceneSerializer.Deserialize(undo!.Json);
        Assert.Equal(1f, restored.Find("Entidad")!.Transform.Position.X, 3);
        Assert.True(history.CanRedo);
    }

    [Fact]
    public void RehacerDevuelveLaEdicionDeshecha()
    {
        var scene = SceneWith(1f);
        var history = new SceneHistory();
        history.Initialize(scene);

        scene.Find("Entidad")!.Transform.Position = new Vector2(5f, 0f);
        history.Record(scene, "Mover");
        history.Undo();

        var redo = history.Redo();

        Assert.NotNull(redo);
        var restored = Ollin.Core.Serialization.SceneSerializer.Deserialize(redo!.Json);
        Assert.Equal(5f, restored.Find("Entidad")!.Transform.Position.X, 3);
        Assert.False(history.CanRedo);
    }

    [Fact]
    public void UnaNuevaEdicionDescartaElRehacer()
    {
        var scene = SceneWith(1f);
        var history = new SceneHistory();
        history.Initialize(scene);

        scene.Find("Entidad")!.Transform.Position = new Vector2(5f, 0f);
        history.Record(scene, "Mover");
        history.Undo();

        scene.Find("Entidad")!.Transform.Position = new Vector2(9f, 0f);
        history.Record(scene, "Mover otra vez");

        Assert.False(history.CanRedo);
        Assert.Equal(1, history.UndoCount);
    }

    [Fact]
    public void ReplaceCurrentAgrupaEdicionesContinuas()
    {
        var scene = SceneWith(1f);
        var history = new SceneHistory();
        history.Initialize(scene);

        for (var i = 2; i <= 5; i++)
        {
            scene.Find("Entidad")!.Transform.Position = new Vector2(i, 0f);
            history.ReplaceCurrent(Ollin.Core.Serialization.SceneSerializer.Serialize(scene));
        }

        Assert.Equal(0, history.UndoCount);
        Assert.NotNull(history.Current);

        var current = Ollin.Core.Serialization.SceneSerializer.Deserialize(history.Current!.Json);
        Assert.Equal(5f, current.Find("Entidad")!.Transform.Position.X, 3);
    }

    [Fact]
    public void ElLimiteDescartaLosPasosMasAntiguos()
    {
        var scene = SceneWith(0f);
        var history = new SceneHistory(limit: 3);
        history.Initialize(scene);

        for (var i = 1; i <= 6; i++)
        {
            scene.Find("Entidad")!.Transform.Position = new Vector2(i, 0f);
            history.Record(scene, $"Paso {i}");
        }

        Assert.Equal(3, history.UndoCount);
        // Lo que se deshace desde el estado final es el paso 5.\n        Assert.Equal("Paso 5", history.UndoLabel);
    }

    [Fact]
    public void SinHistorialNoHayNadaQueDeshacer()
    {
        var history = new SceneHistory();

        Assert.False(history.CanUndo);
        Assert.False(history.CanRedo);
        Assert.Null(history.Undo());
        Assert.Null(history.Redo());
    }
}
