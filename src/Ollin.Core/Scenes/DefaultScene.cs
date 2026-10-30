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
            Size = new Vector2(14f, 0.4f),
            Tint = new Vector4(0.24f, 0.29f, 0.36f, 1f),
            SortingOrder = -10,
        });

        var player = scene.Create("Jugador", new Vector2(0f, -1.6f));
        player.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(1.1f, 1.1f),
            Tint = new Vector4(0.91f, 0.64f, 0.23f, 1f),
            SortingOrder = 10,
        });

        var crate = scene.Create("Caja", new Vector2(2.6f, -1.9f));
        crate.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.9f, 0.9f),
            Tint = new Vector4(0.25f, 0.71f, 0.66f, 1f),
            SortingOrder = 5,
        });

        var coin = scene.Create("Moneda", new Vector2(4.4f, -1.6f));
        coin.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.45f, 0.45f),
            Tint = new Vector4(0.96f, 0.84f, 0.32f, 1f),
            SortingOrder = 20,
        });

        var tilted = scene.Create("Caja girada", new Vector2(-2.8f, -1.8f));
        tilted.Transform.Rotation = 0.35f;
        tilted.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(1f, 0.6f),
            Tint = new Vector4(0.85f, 0.36f, 0.32f, 1f),
            SortingOrder = 6,
        });

        return scene;
    }
}
