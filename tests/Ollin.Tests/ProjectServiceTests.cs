using Ollin.Core.Projects;
using Ollin.Core.Serialization;
using Ollin.Scripting;
using Xunit;

namespace Ollin.Tests;

public class ProjectServiceTests
{
    [Fact]
    public void CrearProyectoGeneraCarpetasEscenaYScript()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "project-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "Mi Juego");

            Assert.True(Directory.Exists(Path.Combine(project.RootPath, "scenes")));
            Assert.True(Directory.Exists(Path.Combine(project.RootPath, "scripts")));
            Assert.True(Directory.Exists(Path.Combine(project.RootPath, "assets")));
            Assert.True(Directory.Exists(Path.Combine(project.RootPath, "prefabs")));
            Assert.True(File.Exists(project.ProjectFile));
            Assert.True(File.Exists(project.StartSceneFullPath));
            Assert.True(File.Exists(Path.Combine(project.RootPath, "scripts", "Juego.cs")));

            var scene = SceneSerializer.Load(project.StartSceneFullPath);
            Assert.NotNull(scene.GetMainCamera());
            Assert.NotNull(scene.Find("Jugador"));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void ElScriptInicialCompilaConElWorkspace()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "project-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "Compila");
            using var workspace = new ScriptWorkspace(project.RootPath);

            var result = workspace.Reload();

            Assert.True(result.Success, string.Join("\n", result.Diagnostics.Select(d => d.Text)));
            Assert.Contains(result.Types, t => t.Name == "Juego");
            Assert.NotNull(workspace.CreateInstance("Juego"));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void EncontrarProyectoDesdeUnaSubcarpeta()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "project-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            var project = ProjectService.Create(parent, "Buscable");
            var fromScripts = ProjectService.Find(Path.Combine(project.RootPath, "scripts"));

            Assert.NotNull(fromScripts);
            Assert.Equal(project.Name, fromScripts!.Name);
            Assert.Equal(Path.GetFullPath(project.RootPath), Path.GetFullPath(fromScripts.RootPath));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void AbrirCarpetaSinProyectoLaTrataComoProyecto()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "project-tests", Guid.NewGuid().ToString("N"));
        var scripts = Path.Combine(parent, "scripts");
        Directory.CreateDirectory(scripts);

        try
        {
            var project = ProjectService.OpenFolder(parent, "Ejemplo");

            Assert.Equal("Ejemplo", project.Name);
            Assert.Equal(Path.GetFullPath(parent), Path.GetFullPath(project.RootPath));
            Assert.Null(ProjectService.Find(parent));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void NoSobrescribeUnaCarpetaConContenido()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "project-tests", Guid.NewGuid().ToString("N"));
        var existing = Path.Combine(parent, "Ocupado");
        Directory.CreateDirectory(existing);
        File.WriteAllText(Path.Combine(existing, "algo.txt"), "x");

        try
        {
            Assert.Throws<InvalidOperationException>(() => ProjectService.Create(parent, "Ocupado"));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }
}
