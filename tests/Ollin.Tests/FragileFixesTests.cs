using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Prefabs;
using Ollin.Core.Projects;
using Ollin.Core.Scenes;
using Xunit;

namespace Ollin.Tests;

public class PrefabPathTests
{
    [Fact]
    public void NormalizaLaExtensionDelPrefab()
    {
        Assert.Equal("torreta.prefab.json", PrefabSerializer.NormalizePath("torreta"));
        Assert.Equal("torreta.prefab.json", PrefabSerializer.NormalizePath("torreta.json"));
        Assert.Equal("torreta.prefab.json", PrefabSerializer.NormalizePath("torreta.prefab.json"));
        Assert.Equal("torreta.PREFAB.JSON", PrefabSerializer.NormalizePath("torreta.PREFAB.JSON"));
        Assert.Equal("torreta.prefab.json", PrefabSerializer.NormalizePath("  torreta  "));
    }
}

public class ProjectFolderNameTests
{
    [Fact]
    public void QuitaEspaciosYCaracteresInvalidos()
    {
        Assert.Equal("Mi_Juego", ProjectService.FolderNameFor("Mi Juego"));
        Assert.Equal("Proyecto", ProjectService.FolderNameFor("   "));
        Assert.DoesNotContain(':', ProjectService.FolderNameFor("a:b"));
    }

    [Fact]
    public void ValidaNombreYCarpetaAntesDeCrear()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "folder-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(parent);

        try
        {
            Assert.True(ProjectService.CanCreate(parent, "Nuevo", out var sinError));
            Assert.Null(sinError);

            Assert.False(ProjectService.CanCreate("", "Nuevo", out var sinCarpeta));
            Assert.NotNull(sinCarpeta);

            Assert.False(ProjectService.CanCreate(parent, "   ", out var sinNombre));
            Assert.NotNull(sinNombre);

            var ocupada = Path.Combine(parent, "Ocupada");
            Directory.CreateDirectory(ocupada);
            File.WriteAllText(Path.Combine(ocupada, "algo.txt"), "x");

            Assert.False(ProjectService.CanCreate(parent, "Ocupada", out var ocupado));
            Assert.Contains("vacía", ocupado);
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }

    [Fact]
    public void PermiteReutilizarUnaCarpetaVacia()
    {
        var parent = Path.Combine(AppContext.BaseDirectory, "folder-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Path.Combine(parent, "Vacio"));

        try
        {
            var project = ProjectService.Create(parent, "Vacio");
            Assert.True(File.Exists(project.ProjectFile));
        }
        finally
        {
            Directory.Delete(parent, recursive: true);
        }
    }
}

public class CompletionCatalogTests
{
    [Fact]
    public void IncluyePalabrasTiposApiEntidadesYScripts()
    {
        var all = Ollin.Scripting.CompletionCatalog.All(new[] { "Jugador" }, new[] { "PlayerBob" });

        Assert.Contains(all, e => e.Text == "public");
        Assert.Contains(all, e => e.Text == "Vector2");
        Assert.Contains(all, e => e.Text == "Input.GetKey(KeyCode.A)");
        Assert.Contains(all, e => e.Text == "PlayerBob");
        Assert.Contains(all, e => e.Text == "\"Jugador\"");
    }

    [Fact]
    public void LosMiembrosNoSeRepiten()
    {
        var texts = Ollin.Scripting.CompletionCatalog.Members.Select(m => m.Text).ToList();
        Assert.Equal(texts.Count, texts.Distinct().Count());
    }
}

public class TilemapPaintMathTests
{
    [Fact]
    public void ElPuntoDeMundoSeConvierteEnCeldaConLaPosicionDeLaEntidad()
    {
        var scene = new Scene();
        var ground = scene.Create("Suelo", new Vector2(-12f, -4f));
        var map = ground.AddComponent(new Tilemap
        {
            Width = 24,
            Height = 3,
            TileSize = 1f,
        });

        var world = scene.GetWorldMatrix(ground);
        Assert.True(Matrix4x4.Invert(world, out var inverse));

        var first = map.WorldToCell(Vector2.Transform(new Vector2(-11.5f, -4.5f), inverse));
        Assert.Equal((0, 0), first);

        var second = map.WorldToCell(Vector2.Transform(new Vector2(-9.5f, -5.5f), inverse));
        Assert.Equal((2, 1), second);
    }

    [Fact]
    public void UnPuntoFueraDelMapaNoEsUnaCeldaValida()
    {
        var map = new Tilemap { Width = 4, Height = 2, TileSize = 1f };

        // Por encima de la fila 0 (el mapa crece hacia abajo).
        var (x, y) = map.WorldToCell(new Vector2(0.5f, 0.5f));
        Assert.True(x < 0 || x >= map.Width || y < 0 || y >= map.Height);
    }
}
