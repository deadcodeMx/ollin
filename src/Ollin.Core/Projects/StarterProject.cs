using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Projects;

/// <summary>Contenido inicial de un proyecto nuevo: escena mínima y script vacío.</summary>
public static class StarterProject
{
    public static Scene CreateEmptyScene()
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

    public static (string FileName, string Source) Script() => ("Juego.cs", ScriptSource);

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
