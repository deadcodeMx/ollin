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
        camera.AddComponent(new Camera2D
        {
            OrthographicSize = 6f,
            Zoom = 1f,
            Ambient = new Vector4(0.34f, 0.36f, 0.44f, 1f),
            FollowTag = "player",
            FollowSmoothing = 5f,
        });

        camera.AddComponent(new DayNightCycle
        {
            DayLengthSeconds = 180f,
            TimeOfDay = 0.2f,
            DayAmbient = new Vector4(1f, 0.97f, 0.9f, 1f),
            NightAmbient = new Vector4(0.12f, 0.14f, 0.22f, 1f),
        });

        var ground = scene.Create("Terreno", new Vector2(-10f, -2.5f));
        var tilemap = new Tilemap
        {
            Width = 20,
            Height = 4,
            TileSize = 1f,
            TexturePath = "assets/sprites/tiles.png",
            TilesetColumns = 4,
            SortingOrder = -10,
            Collides = true,
        };

        for (var y = 0; y < tilemap.Height; y++)
        {
            for (var x = 0; x < tilemap.Width; x++)
            {
                tilemap.SetTile(x, y, y == 0 ? 0 : 2);
            }
        }

        ground.AddComponent(tilemap);

        var player = scene.Create("Jugador", new Vector2(0f, -1.6f));
        player.Tags.Add("player");
        player.AddComponent(new Stats
        {
            Entries =
            {
                new StatEntry { Name = "Vida", Value = 100f, Max = 100f },
                new StatEntry { Name = "Hambre", Value = 100f, Max = 100f, DecayPerSecond = 0.4f },
            },
        });
        player.AddComponent(new Inventory
        {
            Capacity = 8,
            Slots =
            {
                new InventorySlot { ItemId = "pocion", Quantity = 2 },
                new InventorySlot { ItemId = "moneda", Quantity = 1 },
            },
        });
        player.AddComponent(new Light2D
        {
            Radius = 5.5f,
            Intensity = 0.85f,
            Color = new Vector4(1f, 0.92f, 0.78f, 1f),
        });
        player.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/hero-walk.png",
            Size = new Vector2(1.3f, 1.3f),
            SortingOrder = 10,
        });
        player.AddComponent(new SpriteAnimator
        {
            TexturePath = "assets/sprites/hero-walk.png",
            FrameWidth = 32,
            FrameHeight = 32,
            Columns = 4,
            FrameCount = 4,
            Fps = 6f,
            Loop = true,
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
        crate.AddComponent(new BoxCollider { Size = new Vector2(1.2f, 1.2f) });
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
        coin.AddComponent(new Light2D
        {
            Radius = 2.2f,
            Intensity = 0.7f,
            Color = new Vector4(1f, 0.82f, 0.35f, 1f),
            FlickerAmplitude = 0.08f,
        });

        var tilted = scene.Create("Caja girada", new Vector2(-2.8f, -1.8f));
        tilted.Transform.Rotation = 0.35f;
        tilted.AddComponent(new BoxCollider { Size = new Vector2(1f, 0.6f) });
        tilted.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(1f, 0.6f),
            Tint = new Vector4(1f, 0.82f, 0.78f, 1f),
            SortingOrder = 6,
        });

        var note = scene.Create("Nota", new Vector2(-1.4f, -1.25f));
        note.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.45f, 0.55f),
            Tint = new Vector4(0.93f, 0.89f, 0.72f, 1f),
            SortingOrder = 22,
        });
        note.AddComponent(new Interactable
        {
            Prompt = "E: leer la nota",
            DialogueId = "bienvenida",
            Radius = 2.2f,
        });

        var music = scene.Create("Música", new Vector2(0f, 0f));
        music.AddComponent(new AudioSource
        {
            Clip = "assets/audio/ambient.wav",
            Volume = 0.25f,
            Loop = true,
            Music = true,
            PlayOnStart = true,
        });

        return scene;
    }
}
