using System.Text;
using System.Text.Json;
using Ollin.Core.Projects;
using Xunit;

namespace Ollin.Tests;

public class GameExporterTests
{
    [Fact]
    public void ExportaProyectoReproductorYManifiesto()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "export-tests", Guid.NewGuid().ToString("N"));
        var template = Path.Combine(root, "player");
        var output = Path.Combine(root, "out");
        Directory.CreateDirectory(template);
        File.WriteAllText(Path.Combine(template, "Ollin.Player.exe"), "fake");
        File.WriteAllText(Path.Combine(template, "Ollin.Player.dll"), "fake");

        try
        {
            var project = ProjectService.Create(root, "Juego Test", template: ProjectTemplate.Platformer);
            var scripts = new byte[] { 1, 2, 3, 4 };
            var target = GameExporter.Export(project, output, template, scripts);

            // En Windows el reproductor se publica como .exe; en Linux/macOS va sin extensión.
            var ejecutable = "Juego Test" + (OperatingSystem.IsWindows() ? ".exe" : string.Empty);
            Assert.True(File.Exists(Path.Combine(target, ejecutable)));
            Assert.True(File.Exists(Path.Combine(target, "game.pak")));
            Assert.True(File.Exists(Path.Combine(target, "LEEME.txt")));
            Assert.False(File.Exists(Path.Combine(target, "project.ollin")));
            Assert.False(Directory.Exists(Path.Combine(target, "scripts")));

            var package = File.ReadAllBytes(Path.Combine(target, "game.pak"));
            Assert.True(GamePackage.IsPackage(package));
            Assert.DoesNotContain("project.ollin", Encoding.UTF8.GetString(package));

            var extract = Path.Combine(root, "extract");
            var (manifest, loadedScripts) = GamePackage.Extract(package, extract);

            Assert.Equal("Juego Test", manifest.ProjectName);
            Assert.True(manifest.Splash);
            Assert.True(manifest.Watermark);
            Assert.Equal(scripts, loadedScripts);
            Assert.True(File.Exists(Path.Combine(extract, manifest.StartScene.Replace('/', Path.DirectorySeparatorChar))));
            Assert.True(File.Exists(Path.Combine(extract, ProjectService.FileName)));
            Assert.Empty(Directory.EnumerateFiles(extract, "*.cs", SearchOption.AllDirectories));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public void ExportaTodasLasEscenasDelProyecto()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "export-tests", Guid.NewGuid().ToString("N"));
        var template = Path.Combine(root, "player");
        var output = Path.Combine(root, "out");
        Directory.CreateDirectory(template);
        File.WriteAllText(Path.Combine(template, "Ollin.Player.exe"), "fake");

        try
        {
            var project = ProjectService.Create(root, "Juego Multi Escena", template: ProjectTemplate.Platformer);
            var secondPath = Path.Combine(project.ScenesFolder, "nivel-2.ollin.json");
            Ollin.Core.Serialization.SceneSerializer.Save(new Ollin.Core.Scenes.Scene { Name = "Nivel 2" }, secondPath);

            var target = GameExporter.Export(project, output, template);
            var package = File.ReadAllBytes(Path.Combine(target, "game.pak"));
            var extract = Path.Combine(root, "extract");
            var (manifest, _) = GamePackage.Extract(package, extract);

            Assert.Contains("scenes/nivel-2.ollin.json", manifest.Scenes);
            Assert.True(File.Exists(Path.Combine(extract, "scenes", "nivel-2.ollin.json")));
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
