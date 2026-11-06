using System.Numerics;
using Ollin.Core.Model;
using Ollin.Scripting;
using Xunit;

namespace Ollin.Tests;

public class ScriptingTests
{
    private const string MoverSource = """
        using System.Numerics;
        using Ollin.Scripting;

        public sealed class TestMover : OllinScript
        {
            public override void OnUpdate(float deltaTime)
            {
                Entity.Transform.Position += new Vector2(deltaTime, 0f);
            }
        }
        """;

    private static string CreateProjectWith(string fileName, string source)
    {
        var root = Path.Combine(AppContext.BaseDirectory, "script-tests", Guid.NewGuid().ToString("N"));
        var scripts = Path.Combine(root, "scripts");
        Directory.CreateDirectory(scripts);
        File.WriteAllText(Path.Combine(scripts, fileName), source);
        return root;
    }

    [Fact]
    public void CompilaYEjecutaUnScriptSobreLaEntidad()
    {
        var root = CreateProjectWith("TestMover.cs", MoverSource);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            var result = workspace.Reload();

            Assert.True(result.Success);
            Assert.Single(result.Types);
            Assert.Equal("TestMover", result.Types[0].Name);

            var entity = new Entity { Name = "Prueba" };
            var script = workspace.CreateInstance("TestMover");

            Assert.NotNull(script);
            script!.Attach(entity);
            script.OnStart();
            script.OnUpdate(0.5f);
            script.OnUpdate(0.25f);

            Assert.Equal(0.75f, entity.Transform.Position.X, 3);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void UnErrorDeCompilacionDevuelveDiagnosticoConLinea()
    {
        var root = CreateProjectWith("Roto.cs", """
            using Ollin.Scripting;

            public sealed class Roto : OllinScript
            {
                public override void OnUpdate(float deltaTime)
                {
                    int numero = "texto";
                }
            }
            """);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            var result = workspace.Reload();

            Assert.False(result.Success);
            Assert.Empty(result.Types);

            var error = Assert.Single(result.Diagnostics.Where(d => d.IsError));
            Assert.Equal("CS0029", error.Id);
            Assert.Contains("Roto.cs", error.Text);
            Assert.True(error.Line >= 5);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void RecargarReemplazaElEnsambladoAnterior()
    {
        var root = CreateProjectWith("TestMover.cs", MoverSource);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            workspace.Reload();
            Assert.Equal("TestMover", workspace.Types[0].Name);

            File.WriteAllText(Path.Combine(root, "scripts", "TestMover.cs"), """
                using Ollin.Scripting;

                public sealed class Renombrado : OllinScript
                {
                }
                """);

            var reloaded = workspace.Reload();

            Assert.True(reloaded.Success);
            Assert.Single(reloaded.Types);
            Assert.Equal("Renombrado", reloaded.Types[0].Name);
            Assert.Null(workspace.CreateInstance("TestMover"));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void UnProyectoSinScriptsCompilaVacio()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "script-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "scripts"));

        try
        {
            using var workspace = new ScriptWorkspace(root);
            var result = workspace.Reload();

            Assert.True(result.Success);
            Assert.Empty(result.Types);
            Assert.False(workspace.HasScripts);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void SystemEstaDisponibleSinUsingExplicito()
    {
        const string source = """
            public sealed class Matematica
            {
                public float Absoluto(float valor) => MathF.Abs(valor);
            }
            """;

        var compilation = ScriptCompiler.Compile(new[] { ("global.cs", source) });

        Assert.True(compilation.Success, string.Join("\n", compilation.Diagnostics.Select(d => d.Text)));
    }

    [Fact]
    public void ElCompiladorAceptaFuentesEnMemoria()
    {
        var compilation = ScriptCompiler.Compile(new[] { ("memoria.cs", MoverSource) });

        Assert.True(compilation.Success);
        Assert.NotNull(compilation.Image);
        Assert.DoesNotContain(compilation.Diagnostics, d => d.IsError);
    }

    [Fact]
    public void LaApiDelScriptVeLaEntidadYLaEscena()
    {
        var root = CreateProjectWith("TestMover.cs", MoverSource);

        try
        {
            using var workspace = new ScriptWorkspace(root);
            workspace.Reload();

            var scene = new Ollin.Core.Scenes.Scene();
            var entity = scene.Create("Héroe", Vector2.Zero);
            var script = workspace.CreateInstance("TestMover")!;
            script.Attach(entity);

            Assert.Same(entity, script.Entity);
            Assert.Same(scene, script.Scene);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
