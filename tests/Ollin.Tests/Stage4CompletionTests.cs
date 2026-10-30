using System.Numerics;
using Ollin.Core.Audio;
using TestAudio = Ollin.Core.Audio.Audio;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Prefabs;
using Ollin.Core.Projects;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Ollin.Scripting;
using Xunit;

namespace Ollin.Tests;

public class PrefabTests
{
    [Fact]
    public void UnPrefabConservaComponentesYSeInstanciaAparte()
    {
        var entity = new Entity { Name = "Enemigo" };
        entity.Transform.Position = new Vector2(3f, -2f);
        entity.AddComponent(new SpriteRenderer { Size = new Vector2(1f, 2f) });
        entity.AddComponent(new BoxCollider { Size = new Vector2(1f, 2f), IsTrigger = true });
        entity.AddComponent(new AudioSource { Clip = "assets/audio/hit.wav", Loop = true, Volume = 0.4f });

        var json = PrefabSerializer.Serialize(entity);
        var loaded = PrefabSerializer.Deserialize(json);

        Assert.Equal("Enemigo", loaded.Name);
        Assert.Equal(3f, loaded.Transform.Position.X);
        Assert.True(loaded.GetComponent<BoxCollider>()!.IsTrigger);
        Assert.Equal("assets/audio/hit.wav", loaded.GetComponent<AudioSource>()!.Clip);
        Assert.True(loaded.GetComponent<AudioSource>()!.Loop);

        var instance = PrefabSerializer.Instantiate(loaded, new Vector2(10f, 5f), "Copia");

        Assert.NotEqual(loaded.Id, instance.Id);
        Assert.Equal("Copia", instance.Name);
        Assert.Equal(10f, instance.Transform.Position.X);
        Assert.NotNull(instance.GetComponent<SpriteRenderer>());
    }

    [Fact]
    public void UnPrefabGuardadoSePuedeCargarDelDisco()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "prefab-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);
        var path = Path.Combine(folder, "torreta.prefab.json");

        try
        {
            var entity = new Entity { Name = "Torreta" };
            entity.AddComponent(new BoxCollider());
            PrefabSerializer.Save(entity, path);

            Assert.True(File.Exists(path));

            var loaded = PrefabSerializer.Load(path);
            Assert.Equal("Torreta", loaded.Name);
            Assert.NotNull(loaded.GetComponent<BoxCollider>());
        }
        finally
        {
            Directory.Delete(folder, recursive: true);
        }
    }
}

public class AudioTests
{
    private sealed class FakeBackend : IAudioBackend
    {
        public List<(string Path, float Volume, bool Loop, AudioChannel Channel)> Calls { get; } = new();

        public List<int> Stopped { get; } = new();

        public int Play(string absolutePath, float volume, bool loop, AudioChannel channel)
        {
            Calls.Add((absolutePath, volume, loop, channel));
            return Calls.Count;
        }

        public void Stop(int handle) => Stopped.Add(handle);

        public void StopAll() => Stopped.Add(-1);

        public void SetVolume(int handle, float volume)
        {
        }

        public void Fade(int handle, float targetVolume, float seconds)
        {
        }
    }

    [Fact]
    public void PlayResuelveLaRutaYDelegaEnElBackend()
    {
        var folder = Path.Combine(AppContext.BaseDirectory, "audio-tests", Guid.NewGuid().ToString("N"));
        var audioFolder = Path.Combine(folder, "assets", "audio");
        Directory.CreateDirectory(audioFolder);
        File.WriteAllBytes(Path.Combine(audioFolder, "eco.wav"), new byte[] { 0, 1, 2, 3 });

        var backend = new FakeBackend();
        TestAudio.Initialize(backend);
        TestAudio.Root = folder;

        try
        {
            var handle = TestAudio.Play("assets/audio/eco.wav", 0.5f, loop: true, AudioChannel.Music);

            Assert.Equal(1, handle);
            var call = Assert.Single(backend.Calls);
            Assert.Equal(Path.Combine(audioFolder, "eco.wav"), call.Path);
            Assert.Equal(0.5f, call.Volume, 3);
            Assert.True(call.Loop);
            Assert.Equal(AudioChannel.Music, call.Channel);

            TestAudio.StopAll();
            Assert.Contains(-1, backend.Stopped);
        }
        finally
        {
            TestAudio.Shutdown();
            TestAudio.Root = null;
            Directory.Delete(folder, recursive: true);
        }
    }

    [Fact]
    public void UnArchivoInexistenteNoLlegaAlBackend()
    {
        var backend = new FakeBackend();
        TestAudio.Initialize(backend);
        TestAudio.Root = Path.GetTempPath();

        try
        {
            Assert.Equal(-1, TestAudio.Play("no-existe.wav"));
            Assert.Empty(backend.Calls);
        }
        finally
        {
            TestAudio.Shutdown();
            TestAudio.Root = null;
        }
    }
}

public class TilemapCollisionTests
{
    private static Scene CreateSceneWithGround(out Tilemap map, bool collides)
    {
        var scene = new Scene();
        var ground = scene.Create("Suelo", new Vector2(-2f, 0f));
        map = ground.AddComponent(new Tilemap
        {
            Width = 4,
            Height = 1,
            TileSize = 1f,
            Collides = collides,
        });

        for (var x = 0; x < 4; x++)
        {
            map.SetTile(x, 0, 1);
        }

        return scene;
    }

    private static Entity CreateFallingPlayer(Scene scene)
    {
        var player = scene.Create("Jugador", new Vector2(0f, 2f));
        player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 0.8f) });
        player.AddComponent(new Rigidbody2D { Velocity = new Vector2(0f, -5f) });
        return player;
    }

    [Fact]
    public void UnCuerpoSeApoyaEnLasCeldasSolidas()
    {
        var scene = CreateSceneWithGround(out _, collides: true);
        var player = CreateFallingPlayer(scene);

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        for (var i = 0; i < 40; i++)
        {
            runtime.Tick(0.05f);
        }

        // La celda ocupa de y=-1 a y=0; el jugador (0.8 de alto) queda encima.
        Assert.True(player.Transform.Position.Y >= 0.35f, $"y={player.Transform.Position.Y}");
    }

    [Fact]
    public void SinColisionElCuerpoAtraviesaElTilemap()
    {
        var scene = CreateSceneWithGround(out _, collides: false);
        var player = CreateFallingPlayer(scene);

        var runtime = new SceneRuntime();
        runtime.Play(scene);

        for (var i = 0; i < 40; i++)
        {
            runtime.Tick(0.05f);
        }

        Assert.True(player.Transform.Position.Y < 0f);
    }
}

public class ProjectTemplateTests
{
    private static ProjectInfo CreateProject(ProjectTemplate template)
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "template-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);
        return ProjectService.Create(parent, $"Juego_{template}", template: template);
    }

    [Fact]
    public void LaPlantillaDePlataformasTraeTilemapSolidoYScriptValido()
    {
        var project = CreateProject(ProjectTemplate.Platformer);
        try
        {
            var scene = Ollin.Core.Serialization.SceneSerializer.Load(project.StartSceneFullPath);
            var ground = scene.Find("Suelo")!;
            var map = ground.GetComponent<Tilemap>();

            Assert.NotNull(map);
            Assert.True(map!.Collides);
            Assert.Contains(map.Tiles, tile => tile >= 0);
            Assert.True(File.Exists(Path.Combine(project.RootPath, "scripts", "Plataformas.cs")));

            using var workspace = new ScriptWorkspace(project.RootPath);
            var result = workspace.Reload();
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Select(d => d.Text)));
            Assert.Contains(result.Types, t => t.Name == "Plataformas");
        }
        finally
        {
            Directory.Delete(project.RootPath, recursive: true);
        }
    }

    [Fact]
    public void LaPlantillaDeTerrorTraeAmbienteYScriptValido()
    {
        var project = CreateProject(ProjectTemplate.Horror);
        try
        {
            var scene = Ollin.Core.Serialization.SceneSerializer.Load(project.StartSceneFullPath);
            var ambience = scene.Find("Ambiente")!;
            var audio = ambience.GetComponent<AudioSource>();

            Assert.NotNull(audio);
            Assert.True(audio!.Music);
            Assert.True(audio.Loop);
            Assert.EndsWith("ambient.wav", audio.Clip);

            using var workspace = new ScriptWorkspace(project.RootPath);
            var result = workspace.Reload();
            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Select(d => d.Text)));
            Assert.Contains(result.Types, t => t.Name == "Terror");
        }
        finally
        {
            Directory.Delete(project.RootPath, recursive: true);
        }
    }
}
