using Ollin.Core.Assets;
using Xunit;

namespace Ollin.Tests;

public class AssetDatabaseTests
{
    private static string CreateProject()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "asset-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(root, "assets", "sprites"));
        Directory.CreateDirectory(Path.Combine(root, "assets", "ui"));
        File.WriteAllBytes(Path.Combine(root, "assets", "sprites", "hero.png"), new byte[] { 1, 2, 3 });
        File.WriteAllBytes(Path.Combine(root, "assets", "ui", "panel.png"), new byte[] { 4, 5, 6 });
        File.WriteAllText(Path.Combine(root, "assets", "notas.txt"), "no es una textura");
        return root;
    }

    [Fact]
    public void EscaneaSoloTexturasDeFormaRecursiva()
    {
        var root = CreateProject();

        try
        {
            var assets = AssetDatabase.ScanTextures(root);

            Assert.Equal(2, assets.Count);
            Assert.All(assets, a => Assert.EndsWith(".png", a.RelativePath));
            Assert.Contains(assets, a => a.RelativePath == "assets/sprites/hero.png");
            Assert.Contains(assets, a => a.RelativePath == "assets/ui/panel.png");
            Assert.DoesNotContain(assets, a => a.Name == "notas.txt");
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void LasRutasRelativasUsanBarrasNormales()
    {
        var root = CreateProject();

        try
        {
            var assets = AssetDatabase.ScanTextures(root);

            Assert.All(assets, a => Assert.DoesNotContain('\\', a.RelativePath));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ImportarCopiaLaTexturaYEvitaSobrescribir()
    {
        var root = CreateProject();
        var sourceFolder = Path.Combine(AppContext.BaseDirectory, "asset-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(sourceFolder);
        var source = Path.Combine(sourceFolder, "hero.png");
        File.WriteAllBytes(source, new byte[] { 9, 9, 9 });

        try
        {
            var first = AssetDatabase.ImportTexture(root, source);
            var second = AssetDatabase.ImportTexture(root, source);

            Assert.Equal("assets/hero.png", first);
            Assert.Equal("assets/hero-1.png", second);
            Assert.True(File.Exists(Path.Combine(root, first)));
            Assert.True(File.Exists(Path.Combine(root, second)));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
            Directory.Delete(sourceFolder, recursive: true);
        }
    }

    [Fact]
    public void ImportarRechazaArchivosQueNoSonImagen()
    {
        var root = CreateProject();
        var source = Path.Combine(root, "assets", "notas.txt");

        try
        {
            Assert.Throws<InvalidOperationException>(() => AssetDatabase.ImportTexture(root, source));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public void ElLocalizadorEncuentraLaRaizDelProyectoSubiendoCarpetas()
    {
        var root = Path.Combine(AppContext.BaseDirectory, "asset-tests", Guid.NewGuid().ToString("N"));
        var deep = Path.Combine(root, "src", "app", "bin", "Debug");
        Directory.CreateDirectory(deep);
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        File.WriteAllText(Path.Combine(root, "Ollin.sln"), string.Empty);

        try
        {
            var found = ProjectLocator.FindProjectRoot(deep);

            Assert.Equal(Path.GetFullPath(root), Path.GetFullPath(found!));
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }
}
