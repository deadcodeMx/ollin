using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Projects;

/// <summary>Plantilla de proyecto inicial.</summary>
public enum ProjectTemplate
{
    Empty,
    Platformer,
    Horror,
    Tutorial,
    Corridor,
    TechnicalDemo,
}

/// <summary>Contenido inicial de un proyecto nuevo, sin sistemas de gameplay.</summary>
public static class StarterProject
{
    /// <summary>Escena inicial mínima según el género elegido.</summary>
    public static Scene CreateGenre(ProjectGenre genre) => genre switch
    {
        ProjectGenre.Platformer or ProjectGenre.Shooter => CreatePlatformer(),
        _ => CreateEmpty(),
    };

    public static Scene CreateScene(ProjectTemplate template = ProjectTemplate.Empty) => template switch
    {
        ProjectTemplate.Platformer => CreatePlatformer(),
        _ => CreateEmpty(),
    };

    public static (string FileName, string Source) ScriptFor(ProjectTemplate template) =>
        ("Juego.cs", ScriptSource);

    private static Scene CreateEmpty()
    {
        var scene = new Scene { Name = "Inicio" };
        var camera = scene.Create("Cámara principal", Vector2.Zero);
        camera.AddComponent(new Camera2D { OrthographicSize = 6f, Zoom = 1f });

        var player = scene.Create("Jugador", new Vector2(0f, -1f));
        player.Tags.Add("player");
        player.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.8f, 1f),
            Tint = new Vector4(0.95f, 0.75f, 0.35f, 1f),
        });
        player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 1f) });
        player.AddComponent(new Rigidbody2D { AffectedByGravity = false });
        player.AddComponent(new ScriptComponent { ScriptPath = "scripts/Juego.cs", ClassName = "Juego" });
        return scene;
    }

    private static Scene CreatePlatformer()
    {
        var scene = new Scene { Name = "Inicio" };
        var camera = scene.Create("Cámara principal", new Vector2(0f, 1f));
        camera.AddComponent(new Camera2D { OrthographicSize = 7f, FollowTag = "player", FollowSmoothing = 6f });

        var ground = scene.Create("Suelo", new Vector2(-10f, -3f));
        var tilemap = new Tilemap
        {
            Width = 24,
            Height = 2,
            TileSize = 1f,
            TexturePath = "assets/sprites/tiles.png",
            TilesetColumns = 4,
            SortingOrder = -10,
            Collides = true,
        };
        for (var y = 0; y < tilemap.Height; y++)
            for (var x = 0; x < tilemap.Width; x++)
                tilemap.SetTile(x, y, 0);
        ground.AddComponent(tilemap);

        var player = scene.Create("Jugador", new Vector2(0f, -1f));
        player.Tags.Add("player");
        player.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.8f, 1f),
            Tint = new Vector4(0.95f, 0.75f, 0.35f, 1f),
            SortingOrder = 10,
        });
        player.AddComponent(new CharacterBody2D());
        player.AddComponent(new Rigidbody2D { AffectedByGravity = false });
        player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 1f) });
        player.AddComponent(new ScriptComponent { ScriptPath = "scripts/Juego.cs", ClassName = "Juego" });
        return scene;
    }

    private const string ScriptSource = """
using Ollin.Scripting;

/// <summary>Script inicial del proyecto.</summary>
public sealed class Juego : OllinScript
{
    public override void OnStart()
    {
    }

    public override void OnUpdate(float deltaTime)
    {
    }
}
""";
}
