using System.Numerics;
using Ollin.Core;
using Ollin.Core.Input;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Ollin.Scripting;
using Xunit;

namespace Ollin.Tests;

public class InputStateTests
{
    [Fact]
    public void LosFlancosDeTeclaSeCalculanPorCuadro()
    {
        InputState.Reset();
        InputState.SetKey(KeyCode.W, true);

        Assert.True(InputState.IsKeyDown(KeyCode.W));
        Assert.True(InputState.WasKeyPressed(KeyCode.W));

        InputState.Update(); // cierra el cuadro

        Assert.True(InputState.IsKeyDown(KeyCode.W));
        Assert.False(InputState.WasKeyPressed(KeyCode.W));
        Assert.False(InputState.WasKeyReleased(KeyCode.W));

        InputState.SetKey(KeyCode.W, false);

        Assert.False(InputState.IsKeyDown(KeyCode.W));
        Assert.True(InputState.WasKeyReleased(KeyCode.W));

        InputState.Update();
        Assert.False(InputState.WasKeyReleased(KeyCode.W));

        InputState.Reset();
    }

    [Fact]
    public void ElDeltaDelRatonSeCalculaEntreCuadros()
    {
        InputState.Reset();
        InputState.SetMousePosition(new Vector2(10, 20), new Vector2(1, 2));
        InputState.Update();

        Assert.Equal(new Vector2(1, 2), InputState.MouseWorldPosition);

        InputState.SetMousePosition(new Vector2(15, 25), new Vector2(3, 4));
        InputState.Update();

        Assert.Equal(new Vector2(5, 5), InputState.MouseDelta);
        Assert.Equal(new Vector2(3, 4), InputState.MouseWorldPosition);

        InputState.Reset();
        Assert.Equal(Vector2.Zero, InputState.MouseDelta);
    }

    [Fact]
    public void LosBotonesDelRatonTienenFlancos()
    {
        InputState.Reset();

        InputState.SetMouseButton(0, true);
        Assert.True(InputState.IsMouseButtonDown(0));
        Assert.True(InputState.WasMouseButtonPressed(0));

        InputState.Update();
        Assert.False(InputState.WasMouseButtonPressed(0));

        InputState.SetMouseButton(0, false);
        Assert.True(InputState.WasMouseButtonReleased(0));

        InputState.Update();
        Assert.False(InputState.WasMouseButtonReleased(0));

        InputState.Reset();
    }
}

public class TimeStateTests
{
    [Fact]
    public void ElRuntimeAvanzaElRelojYLoReiniciaAlDetener()
    {
        var scene = new Scene();
        scene.Create("Entidad", Vector2.Zero);

        var runtime = new SceneRuntime();
        runtime.Play(scene);
        runtime.Tick(0.05f);
        runtime.Tick(0.05f);

        Assert.Equal(0.05f, TimeState.DeltaTime, 4);
        Assert.Equal(0.1f, TimeState.Elapsed, 4);
        Assert.Equal(2, TimeState.Frame);

        runtime.Stop();

        Assert.Equal(0f, TimeState.DeltaTime);
        Assert.Equal(0f, TimeState.Elapsed);
        Assert.Equal(0, TimeState.Frame);
    }
}

public class HotReloadTests
{
    private const string CounterV1 = """
        using System.Numerics;
        using Ollin.Scripting;

        public sealed class Contador : OllinScript
        {
            public int Counter;

            public override void OnUpdate(float deltaTime)
            {
                Counter++;
                Entity.Transform.Position = new Vector2(Counter, 0f);
            }
        }
        """;

    private const string CounterV2 = """
        using System.Numerics;
        using Ollin.Scripting;

        public sealed class Contador : OllinScript
        {
            public int Counter;
            public int Version = 7;

            public override void OnUpdate(float deltaTime)
            {
                Counter++;
                Entity.Transform.Position = new Vector2(Counter, Version);
            }
        }
        """;

    private static string CreateProjectWith(string source)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "hotreload-tests", Guid.NewGuid().ToString("N"));
        var scripts = Path.Combine(root, "scripts");
        Directory.CreateDirectory(scripts);
        File.WriteAllText(Path.Combine(scripts, "Contador.cs"), source);
        return root;
    }

    [Fact]
    public void LaRecargaEnCalienteConservaElEstadoYEjecutaElCodigoNuevo()
    {
        var root = CreateProjectWith(CounterV1);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            Assert.True(workspace.Reload().Success);

            var scene = new Scene();
            var entity = scene.Create("Contador", Vector2.Zero);
            entity.AddComponent(new ScriptComponent { ScriptPath = "scripts/Contador.cs", ClassName = "Contador" });

            var runtime = new SceneRuntime();
            runtime.Play(scene, workspace);
            runtime.Tick(0.05f);
            runtime.Tick(0.05f);

            Assert.Equal(2f, entity.Transform.Position.X, 3);
            Assert.Equal(0f, entity.Transform.Position.Y, 3);

            File.WriteAllText(Path.Combine(root, "scripts", "Contador.cs"), CounterV2);

            var reloaded = runtime.HotReload(workspace);

            Assert.Equal(1, reloaded);
            Assert.True(runtime.IsPlaying);

            // El estado (Counter) se conserva y el código nuevo ya corre (Version = 7).
            runtime.Tick(0.05f);
            Assert.Equal(3f, entity.Transform.Position.X, 3);
            Assert.Equal(7f, entity.Transform.Position.Y, 3);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void UnaRecargaConErrorDejaElRuntimeSinScriptsPeroSinRomperLaReproduccion()
    {
        var root = CreateProjectWith(CounterV1);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            workspace.Reload();

            var scene = new Scene();
            var entity = scene.Create("Contador", Vector2.Zero);
            entity.AddComponent(new ScriptComponent { ScriptPath = "scripts/Contador.cs", ClassName = "Contador" });

            var runtime = new SceneRuntime();
            runtime.Play(scene, workspace);
            runtime.Tick(0.05f);

            File.WriteAllText(Path.Combine(root, "scripts", "Contador.cs"), "public sealed class Roto { int x = \"no\"; }");

            var reloaded = runtime.HotReload(workspace);

            Assert.Equal(0, reloaded);
            Assert.True(runtime.IsPlaying);

            // El bucle de juego sigue funcionando sin scripts.
            runtime.Tick(0.05f);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
