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
            var target = GameExporter.Export(project, output, template);

            Assert.True(File.Exists(Path.Combine(target, "Ollin.Player.exe")));
            Assert.True(File.Exists(Path.Combine(target, "project.ollin")));
            Assert.True(File.Exists(Path.Combine(target, "game.json")));
            Assert.True(File.Exists(Path.Combine(target, "LEEME.txt")));
            Assert.True(Directory.Exists(Path.Combine(target, "scenes")));
            Assert.True(Directory.Exists(Path.Combine(target, "scripts")));

            var manifest = JsonSerializer.Deserialize<GameManifest>(
                File.ReadAllText(Path.Combine(target, "game.json")),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

            Assert.NotNull(manifest);
            Assert.Equal("Juego Test", manifest!.ProjectName);
            Assert.True(manifest.Splash);
            Assert.True(manifest.Watermark);
            Assert.True(File.Exists(Path.Combine(target, manifest.StartScene.Replace('/', Path.DirectorySeparatorChar))));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}
