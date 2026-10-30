using System.Numerics;
using Ollin.Core.Hud;
using Ollin.Core.Scenes;
using Ollin.Runtime;
using Xunit;
using ScriptHud = Ollin.Scripting.Hud;

namespace Ollin.Tests;

public class HudTests
{
    [Fact]
    public void LasBarrasSeCreanYActualizan()
    {
        var hud = new HudModel();

        hud.SetBar("Vida", 50f, 100f, Vector4.One);
        hud.SetBar("Vida", 25f, 100f, Vector4.One);

        var bar = Assert.Single(hud.Bars);
        Assert.Equal("Vida", bar.Name);
        Assert.Equal(0.25f, bar.Fraction, 3);
    }

    [Fact]
    public void LosMensajesCaducan()
    {
        var hud = new HudModel();
        hud.ShowMessage("Hola", 0.5f);
        Assert.Single(hud.Messages);

        hud.Update(0.6f);
        Assert.Empty(hud.Messages);
    }

    [Fact]
    public void GameOverYClearFuncionan()
    {
        var hud = new HudModel();
        hud.SetBar("Vida", 0f, 100f, Vector4.One);
        hud.GameOver("Has muerto");

        Assert.True(hud.IsGameOver);
        Assert.Equal("Has muerto", hud.GameOverText);

        hud.Clear();
        Assert.False(hud.IsGameOver);
        Assert.Empty(hud.Bars);
    }

    [Fact]
    public void OnStartYaPuedeEscribirEnElHud()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "hud-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scripts"));
        File.WriteAllText(Path.Combine(root, "scripts", "HudStart.cs"), """
            using Ollin.Scripting;

            public sealed class HudStart : OllinScript
            {
                public override void OnStart()
                {
                    Hud.SetBar("Vida", 42f, 100f);
                }
            }
            """);

        try
        {
            using var workspace = new Ollin.Scripting.ScriptWorkspace(root);
            Assert.True(workspace.Reload().Success);

            var scene = new Scene();
            var entity = scene.Create("Control", Vector2.Zero);
            entity.AddComponent(new Ollin.Core.Model.Components.ScriptComponent
            {
                ScriptPath = "scripts/HudStart.cs",
                ClassName = "HudStart",
            });

            var runtime = new SceneRuntime();
            runtime.Play(scene, workspace);

            try
            {
                Assert.Equal(42f, Assert.Single(runtime.Hud.Bars).Value, 3);
            }
            finally
            {
                runtime.Stop();
            }
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LosScriptsEscribenEnElHudDelRuntime()
    {
        var runtime = new SceneRuntime();
        runtime.Play(new Scene());

        try
        {
            ScriptHud.SetBar("Vida", 40f, 100f);
            ScriptHud.SetLabel("Monedas", "3");
            ScriptHud.Message("Hola", 0.5f);

            Assert.Equal(0.4f, Assert.Single(runtime.Hud.Bars).Fraction, 3);
            Assert.Equal("3", Assert.Single(runtime.Hud.Labels).Text);
            Assert.Single(runtime.Hud.Messages);

            for (var i = 0; i < 6; i++)
            {
                runtime.Tick(0.1f);
            }

            Assert.Empty(runtime.Hud.Messages);

            ScriptHud.GameOver("Fin");
            Assert.True(runtime.Hud.IsGameOver);
        }
        finally
        {
            runtime.Stop();
        }
    }
}
