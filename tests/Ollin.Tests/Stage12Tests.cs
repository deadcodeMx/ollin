using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public class SaveSlotServiceTests
{
    [Fact]
    public void GuardaYCargaTresRanurasSeparadas()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-saves", Guid.NewGuid().ToString("N"));

        try
        {
            var service = new SaveSlotService(root);
            var scene = new Scene();
            scene.Create("Jugador", Vector2.Zero);

            var state1 = new GameState();
            state1.SetNumber("contador", 3);
            service.Save("Demo", 1, scene, state1);

            var state2 = new GameState();
            state2.SetFlag("abierto", true);
            service.Save("Demo", 2, scene, state2);

            Assert.True(service.Exists("Demo", 1));
            Assert.True(service.Exists("Demo", 2));
            Assert.False(service.Exists("Demo", 3));

            var loaded1 = service.TryLoad("Demo", 1)!;
            var loaded2 = service.TryLoad("Demo", 2)!;

            Assert.Equal(3f, loaded1.State.GetNumber("contador"), 3);
            Assert.True(loaded2.State.GetFlag("abierto"));
            Assert.NotEqual(service.GetPath("Demo", 1), service.GetPath("Demo", 2));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ElAutoguardadoUsaUnArchivoDistinto()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-saves", Guid.NewGuid().ToString("N"));

        try
        {
            var service = new SaveSlotService(root);
            var normal = service.GetPath("Mi Juego", 1);
            var auto = service.GetPath("Mi Juego", 1, autosave: true);

            Assert.NotEqual(normal, auto);
            Assert.Contains("slot1-auto", auto);
            Assert.DoesNotContain(" ", auto);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}

public class AutosaveSchedulerTests
{
    [Fact]
    public void DisparaCadaIntervalo()
    {
        var scheduler = new AutosaveScheduler(45d);

        Assert.False(scheduler.Tick(44.9));
        Assert.True(scheduler.Tick(0.2));
        Assert.False(scheduler.Tick(44d));
        Assert.True(scheduler.Tick(1d));
    }
}

public class PlayerSettingsTests
{
    [Fact]
    public void NormalizaYConservaAjustes()
    {
        var path = Path.Combine(Path.GetTempPath(), "ollin-settings", Guid.NewGuid().ToString("N") + ".json");

        try
        {
            var settings = new PlayerSettings
            {
                Volume = 2f,
                SensitivityX = 0.1f,
                SensitivityY = 0.00001f,
                InvertY = true,
                Smoothing = 2f,
            };
            settings.Save(path);

            var loaded = PlayerSettings.Load(path);
            Assert.Equal(1f, loaded.Volume, 3);
            Assert.Equal(0.006f, loaded.SensitivityX, 4);
            Assert.Equal(0.0004f, loaded.SensitivityY, 4);
            Assert.True(loaded.InvertY);
            Assert.Equal(0.95f, loaded.Smoothing, 3);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}

public class GraphEditingTests
{
    [Fact]
    public void ElHistorialDeshaceYRehace()
    {
        var graph = new NodeGraph();
        var first = graph.AddNode(GraphNodeKind.Event, 10, 10);
        first.Event = GraphEventKind.OnStart;
        var history = new NodeGraphHistory();
        var initial = GraphEditing.Capture(graph, first.Id);

        graph.AddNode(GraphNodeKind.Action, 200, 10);
        history.Record(initial);

        Assert.True(history.Undo(graph, first.Id, out var undone));
        Assert.Single(graph.Nodes);
        Assert.Equal(first.Id, undone);

        Assert.True(history.Redo(graph, first.Id, out _));
        Assert.Equal(2, graph.Nodes.Count);
    }

    [Fact]
    public void PegarCreaUnNodoNuevoDesplazadoYSinEnlaces()
    {
        var node = new GraphNode
        {
            Kind = GraphNodeKind.Action,
            X = 40,
            Y = 50,
            Next = Guid.NewGuid(),
        };

        var copy = GraphEditing.Paste(node);

        Assert.NotEqual(node.Id, copy.Id);
        Assert.Equal(70, copy.X);
        Assert.Equal(80, copy.Y);
        Assert.Null(copy.Next);
    }

    [Fact]
    public void ElValidadorDetectaFaltasYErrores()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        eventNode.Event = GraphEventKind.OnStart;

        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        action.Action = ActionKind.OpenDoor;
        action.TargetName = string.Empty;
        eventNode.Next = action.Id;

        var issues = GraphValidator.Validate(graph, scene);
        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("objetivo"));
    }

    [Fact]
    public void ElValidadorDetectaCiclos()
    {
        var scene = new Scene();
        var graph = new NodeGraph();
        var eventNode = graph.AddNode(GraphNodeKind.Event, 0, 0);
        var action = graph.AddNode(GraphNodeKind.Action, 200, 0);
        eventNode.Next = action.Id;
        action.Next = eventNode.Id;

        var issues = GraphValidator.Validate(graph, scene);
        Assert.Contains(issues, issue => issue.IsError && issue.Message.Contains("Ciclo"));
    }
}
