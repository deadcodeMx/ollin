using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Saves;
using Ollin.Core.Scenes;
using Ollin.Core.Serialization;
using Ollin.Core.Validation;
using Ollin.Runtime;
using Xunit;

namespace Ollin.Tests;

public class SceneLoadingRobustnessTests
{
    [Fact]
    public void UnaEscenaCorruptaDevuelveErrorSinLanzar()
    {
        var path = Path.Combine(Path.GetTempPath(), "ollin-corrupt", Guid.NewGuid().ToString("N") + ".json");

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, "{ esto no es json");

            Assert.False(SceneSerializer.TryLoad(path, out var scene, out var error));
            Assert.Null(scene);
            Assert.False(string.IsNullOrWhiteSpace(error));
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void UnaEscenaValidaSeCargaConTryLoad()
    {
        var path = Path.Combine(Path.GetTempPath(), "ollin-valid", Guid.NewGuid().ToString("N") + ".json");

        try
        {
            var original = new Scene();
            original.Create("Jugador", new Vector2(1f, 2f));
            SceneSerializer.Save(original, path);

            Assert.True(SceneSerializer.TryLoad(path, out var loaded, out var error));
            Assert.Null(error);
            Assert.NotNull(loaded);
            Assert.Single(loaded!.Entities);
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }

    [Fact]
    public void LasEntidadesHijasConservanPadreYPosicionAlGuardar()
    {
        var path = Path.Combine(Path.GetTempPath(), "ollin-children", Guid.NewGuid().ToString("N") + ".json");

        try
        {
            var scene = new Scene();
            var parent = scene.Create("Padre", new Vector2(10f, 20f));
            var child = scene.Create("Hijo", new Vector2(2f, 3f), parent.Id);
            child.AddComponent(new Wall3D { Size = new Vector3(1f, 2f, 3f) });

            Assert.Equal(new Vector2(12f, 23f), scene.GetWorldPosition(child));

            SceneSerializer.Save(scene, path);
            var loaded = SceneSerializer.Load(path);
            var loadedChild = loaded.Find("Hijo")!;
            var loadedParent = loaded.Find("Padre")!;

            Assert.Equal(loadedParent.Id, loadedChild.ParentId);
            Assert.Equal(new Vector2(2f, 3f), loadedChild.Transform.Position);
            Assert.NotNull(loadedChild.GetComponent<Wall3D>());
            Assert.Equal(new Vector2(12f, 23f), loaded.GetWorldPosition(loadedChild));
        }
        finally
        {
            var directory = Path.GetDirectoryName(path);
            if (directory is not null && Directory.Exists(directory)) Directory.Delete(directory, recursive: true);
        }
    }
}

public class SceneValidatorTests
{
    [Fact]
    public void DetectaAssetsFaltantesYJerarquiaRota()
    {
        var root = Path.Combine(Path.GetTempPath(), "ollin-validation", Guid.NewGuid().ToString("N"));

        try
        {
            Directory.CreateDirectory(Path.Combine(root, "assets"));
            File.WriteAllText(Path.Combine(root, "assets", "ok.png"), "x");

            var scene = new Scene();
            var entity = scene.Create("Entidad", Vector2.Zero);
            entity.AddComponent(new SpriteRenderer { TexturePath = "assets/ok.png" });
            entity.AddComponent(new AudioSource { Clip = "assets/falta.wav" });

            var orphan = scene.Create("Huérfana", Vector2.Zero);
            orphan.ParentId = Guid.NewGuid();

            var issues = SceneValidator.Validate(scene, root);

            Assert.Contains(issues, issue => issue.Kind == SceneIssueKind.MissingAsset && issue.Detail.Contains("falta.wav"));
            Assert.Contains(issues, issue => issue.Kind == SceneIssueKind.Hierarchy && issue.IsError);
            Assert.DoesNotContain(issues, issue => issue.Detail.Contains("ok.png"));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void DetectaCiclosDeJerarquia()
    {
        var scene = new Scene();
        var a = scene.Create("A", Vector2.Zero);
        var b = scene.Create("B", Vector2.Zero, a.Id);
        a.ParentId = b.Id;

        var issues = SceneValidator.Validate(scene, Path.GetTempPath());

        Assert.Contains(issues, issue => issue.Kind == SceneIssueKind.Hierarchy && issue.Detail.Contains("circular"));
    }
}

public class GameStateLifecycleTests
{
    [Fact]
    public void CambiarDeEscenaLimpiaLasVariablesGlobales()
    {
        var runtime = new SceneRuntime();
        GameStateHost.Current = runtime.GameState;
        runtime.GameState.Set("contador", "5");

        runtime.ResetGameState();

        Assert.Same(runtime.GameState, GameStateHost.Current);
        Assert.False(runtime.GameState.Has("contador"));
    }
}
