using System.Numerics;
using Ollin.Core.Model.Components;

namespace Ollin.Core.Scenes;

/// <summary>Escena de ejemplo que se muestra al abrir el editor por primera vez.</summary>
public static class DefaultScene
{
    public static Scene Create()
    {
        var scene = new Scene { Name = "Escena de ejemplo" };

        var camera = scene.Create("Cámara principal", new Vector2(0.9f, -1.2f));
        camera.AddComponent(new Camera2D { OrthographicSize = 6f, Zoom = 1f });

        var ground = scene.Create("Suelo", new Vector2(0f, -2.5f));
        ground.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/ground.png",
            Size = new Vector2(14f, 0.5f),
            SortingOrder = -10,
        });

        var player = scene.Create("Jugador", new Vector2(0f, -1.6f));
        player.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/hero.png",
            Size = new Vector2(1.3f, 1.3f),
            SortingOrder = 10,
        });
        player.AddComponent(new ScriptComponent
        {
            ScriptPath = "scripts/PlayerBob.cs",
            ClassName = "PlayerBob",
        });

        var crate = scene.Create("Caja", new Vector2(2.6f, -1.9f));
        crate.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(1f, 1f),
            SortingOrder = 5,
        });
        crate.AddComponent(new OscillatorComponent
        {
            Axis = new Vector2(0f, 1f),
            Amplitude = 0.22f,
            Frequency = 0.9f,
        });

        var coin = scene.Create("Moneda", new Vector2(4.4f, -1.6f));
        coin.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.55f, 0.55f),
            SortingOrder = 20,
        });
        coin.AddComponent(new RotatorComponent { DegreesPerSecond = 120f });

        var tilted = scene.Create("Caja girada", new Vector2(-2.8f, -1.8f));
        tilted.Transform.Rotation = 0.35f;
        tilted.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(1f, 0.6f),
            Tint = new Vector4(1f, 0.82f, 0.78f, 1f),
            SortingOrder = 6,
        });

        return scene;
    }
}
