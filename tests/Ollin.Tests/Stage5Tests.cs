using System.Numerics;
using Ollin.Core.Dialogue;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Projects;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class DialogueDatabaseTests
{
    [Fact]
    public void CargaDialogosPorNombreYPorCampoId()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "dialog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "dialogs"));

        try
        {
            File.WriteAllText(Path.Combine(root, "dialogs", "nota.json"), """
                { "speaker": "Nota", "lines": [ { "text": "Hola" }, { "text": "Adiós" } ] }
                """);

            File.WriteAllText(Path.Combine(root, "dialogs", "otro.json"), """
                { "id": "saludo", "lines": [ { "text": "Buenas" } ] }
                """);

            File.WriteAllText(Path.Combine(root, "dialogs", "roto.json"), "{ no es json }");

            var database = new DialogueDatabase(root);

            Assert.Equal(2, database.Count);
            Assert.True(database.TryGet("nota", out var note));
            Assert.Equal("Nota", note.Speaker);
            Assert.Equal(2, note.Lines.Count);
            Assert.True(database.TryGet("saludo", out var greeting));
            Assert.Equal("Buenas", greeting.Lines[0].Text);
            Assert.False(database.TryGet("roto", out _));
            Assert.False(database.TryGet("no-existe", out _));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}

public class DialogueRunnerTests
{
    [Fact]
    public void AvanzaLineaALineaYTermina()
    {
        var runner = new DialogueRunner();
        var file = new DialogueFile
        {
            Speaker = "Guía",
            Lines =
            {
                new DialogueLine { Text = "Primera" },
                new DialogueLine { Speaker = "Otro", Text = "Segunda" },
            },
        };

        Assert.True(runner.Start(file));
        Assert.True(runner.IsActive);
        Assert.Equal("Guía", runner.Speaker);
        Assert.Equal("Primera", runner.Text);

        runner.Advance();
        Assert.Equal("Otro", runner.Speaker);
        Assert.Equal("Segunda", runner.Text);

        var finished = false;
        runner.Finished += () => finished = true;
        runner.Advance();

        Assert.False(runner.IsActive);
        Assert.True(finished);
        Assert.Equal(string.Empty, runner.Text);
    }
}

public class InteractionTests
{
    private static SceneRuntime CreateRuntime(out Scene scene, out string root)
    {
        root = Path.Combine(AppContext.BaseDirectory, "dialog-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "dialogs"));
        File.WriteAllText(Path.Combine(root, "dialogs", "nota.json"), """
            { "speaker": "Nota", "lines": [ { "text": "uno" }, { "text": "dos" } ] }
            """);

        scene = new Scene();

        var player = scene.Create("Jugador", Vector2.Zero);
        player.Tags.Add("player");

        var note = scene.Create("Nota", new Vector2(0.5f, 0f));
        note.AddComponent(new Interactable { Prompt = "E: leer", DialogueId = "nota", Radius = 2f });

        var runtime = new SceneRuntime { Dialogues = new DialogueDatabase(root) };
        runtime.Play(scene);
        return runtime;
    }

    [Fact]
    public void PulsarEIniciaElDialogoYNoAvanzaEnElMismoFrame()
    {
        var runtime = CreateRuntime(out _, out var root);

        try
        {
            InputState.SetKey(KeyCode.E, true);
            runtime.Tick(0.016f);

            Assert.True(runtime.Dialogue.IsActive);
            Assert.Equal("uno", runtime.Dialogue.Text);

            runtime.Tick(0.016f);
            Assert.Equal("uno", runtime.Dialogue.Text);
        }
        finally
        {
            runtime.Stop();
            InputState.Reset();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void UnaSegundaPulsacionAvanzaLaLinea()
    {
        var runtime = CreateRuntime(out _, out var root);

        try
        {
            InputState.SetKey(KeyCode.E, true);
            runtime.Tick(0.016f);
            runtime.Tick(0.016f);

            InputState.SetKey(KeyCode.E, false);
            runtime.Tick(0.016f);
            InputState.SetKey(KeyCode.E, true);
            runtime.Tick(0.016f);

            Assert.Equal("dos", runtime.Dialogue.Text);
        }
        finally
        {
            runtime.Stop();
            InputState.Reset();
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ElAvisoApareceSoloSiHayJugadorCerca()
    {
        var runtime = CreateRuntime(out _, out var root);

        try
        {
            runtime.Tick(0.016f);
            Assert.Equal("E: leer", Ollin.Core.Interaction.InteractionState.Prompt);
        }
        finally
        {
            runtime.Stop();
            InputState.Reset();
            Directory.Delete(root, recursive: true);
        }
    }
}

public class InteractableSerializationTests
{
    [Fact]
    public void SerializaEtiquetasYComponenteDeInteraccion()
    {
        var scene = new Scene();
        var note = scene.Create("Nota", new Vector2(2f, 3f));
        note.Tags.AddRange(new[] { "nota", "leer" });
        note.AddComponent(new Interactable
        {
            Prompt = "E: abrir",
            DialogueId = "nota",
            Radius = 2.5f,
            Once = false,
        });

        var json = SceneSerializer.Serialize(scene);
        var restored = SceneSerializer.Deserialize(json);
        var entity = restored.Find("Nota")!;
        var interactable = entity.GetComponent<Interactable>();

        Assert.Contains("leer", entity.Tags);
        Assert.NotNull(interactable);
        Assert.Equal("nota", interactable!.DialogueId);
        Assert.Equal(2.5f, interactable.Radius, 3);
        Assert.False(interactable.Once);
    }
}

public class TutorialTemplateTests
{
    [Fact]
    public void LaPlantillaTutorialEsJugableYCompila()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "template-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "TutorialTest", template: ProjectTemplate.Tutorial);
            var scene = Ollin.Core.Serialization.SceneSerializer.Load(project.StartSceneFullPath);

            Assert.NotNull(scene.Find("Jugador"));
            Assert.NotNull(scene.Find("Nota"));
            Assert.NotNull(scene.Find("Merodeador"));
            Assert.Contains("player", scene.Find("Jugador")!.Tags);

            using var workspace = new Ollin.Scripting.ScriptWorkspace(project.RootPath);
            var result = workspace.Reload();

            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Select(d => d.Text)));
            Assert.Contains(result.Types, type => type.Name == "Tutorial");
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }
}

public class Stage5TemplateTests
{
    [Fact]
    public void LaPlantillaIncluyeNotaConDialogoYJugadorEtiquetado()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "template-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "ConNota", template: ProjectTemplate.Platformer);
            var scene = SceneSerializer.Load(project.StartSceneFullPath);

            Assert.True(File.Exists(Path.Combine(project.RootPath, "dialogs", "bienvenida.json")));

            var note = scene.Find("Nota")!;
            Assert.Equal("bienvenida", note.GetComponent<Interactable>()!.DialogueId);

            var player = scene.Find("Jugador")!;
            Assert.Contains("player", player.Tags);

            var database = new DialogueDatabase(project.RootPath);
            Assert.True(database.TryGet("bienvenida", out _));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }
}
