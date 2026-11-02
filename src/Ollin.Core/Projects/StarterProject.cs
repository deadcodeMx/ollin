using Ollin.Core.Model;
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

/// <summary>Contenido inicial de un proyecto nuevo, según su plantilla.</summary>
public static class StarterProject
{
    /// <summary>Escena inicial mínima según el género elegido.</summary>
    public static Scene CreateGenre(ProjectGenre genre)
    {
        var scene = new Scene { Name = "Inicio" };
        var camera = scene.Create("Cámara principal", Vector2.Zero);
        camera.AddComponent(new Camera2D { OrthographicSize = 7f, FollowTag = "player", FollowSmoothing = 6f });

        var player = scene.Create("Jugador", new Vector2(0f, -1f));
        player.Tags.Add("player");
        player.AddComponent(new SpriteRenderer { Size = new Vector2(0.8f, 1f), Tint = new Vector4(0.95f, 0.75f, 0.35f, 1f) });

        if (genre is ProjectGenre.Platformer or ProjectGenre.Shooter)
        {
            player.AddComponent(new CharacterBody2D());
            player.AddComponent(new Rigidbody2D { AffectedByGravity = false });
            player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 1f) });
        }
        else
        {
            player.AddComponent(new CircleCollider2D { Radius = 0.45f });
            player.AddComponent(new Rigidbody2D());
        }

        switch (genre)
        {
            case ProjectGenre.Platformer:
                player.AddComponent(new Health());
                player.AddComponent(new Checkpoint2D());
                break;
            case ProjectGenre.TopDown:
                player.AddComponent(new Inventory());
                player.AddComponent(new Equipment());
                player.AddComponent(new QuestLog());
                break;
            case ProjectGenre.Horror:
                player.AddComponent(new Inventory());
                player.AddComponent(new Health());
                camera.AddComponent(new Light2D { Radius = 6f, Intensity = 0.8f });
                break;
            case ProjectGenre.Shooter:
                player.AddComponent(new Health());
                player.AddComponent(new Weapon2D());
                break;
            case ProjectGenre.Puzzle:
                player.AddComponent(new Inventory());
                player.AddComponent(new Equipment());
                break;
        }

        return scene;
    }
    public static Scene CreateScene(ProjectTemplate template = ProjectTemplate.Empty) => template switch
    {
        ProjectTemplate.Platformer => CreatePlatformer(),
        ProjectTemplate.Horror => CreateHorror(),
        ProjectTemplate.Tutorial => CreateTutorial(),
        ProjectTemplate.Corridor => CreateCorridor(),
        ProjectTemplate.TechnicalDemo => CreateTechnicalDemo(),
        _ => CreateEmpty(),
    };

    public static (string FileName, string Source) ScriptFor(ProjectTemplate template) => template switch
    {
        ProjectTemplate.Platformer => ("Plataformas.cs", PlatformerScriptSource),
        ProjectTemplate.Horror => ("Terror.cs", HorrorScriptSource),
        ProjectTemplate.Tutorial => ("Tutorial.cs", TutorialScriptSource),
        ProjectTemplate.Corridor => ("Pasillo.cs", CorridorScriptSource),
        _ => ("Juego.cs", ScriptSource),
    };

    private static Scene CreateEmpty()
    {
        var scene = new Scene { Name = "Inicio" };

        var camera = scene.Create("Cámara principal", new Vector2(0f, 0f));
        camera.AddComponent(new Camera2D
        {
            OrthographicSize = 6f,
            Zoom = 1f,
        });

        return scene;
    }
    private static Scene CreatePlatformer()
    {
        var scene = new Scene { Name = "Nivel 1" };

        var camera = scene.Create("Cámara principal", new Vector2(0f, 0f));
        camera.AddComponent(new Camera2D
        {
            OrthographicSize = 7f,
            Zoom = 1f,
            Ambient = new Vector4(0.5f, 0.52f, 0.6f, 1f),
            FollowTag = "player",
            FollowSmoothing = 5f,
        });
        camera.AddComponent(new DayNightCycle { DayLengthSeconds = 150f, TimeOfDay = 0.4f });

        var ground = scene.Create("Suelo", new Vector2(-12f, -4f));
        var tilemap = ground.AddComponent(new Tilemap
        {
            Width = 24,
            Height = 3,
            TileSize = 1f,
            TexturePath = "assets/sprites/tiles.png",
            TilesetColumns = 4,
            SortingOrder = -10,
            Collides = true,
        });

        for (var x = 0; x < tilemap.Width; x++)
        {
            tilemap.SetTile(x, 0, 1);
            tilemap.SetTile(x, 1, 2);
            tilemap.SetTile(x, 2, 0);
        }

        var player = scene.Create("Jugador", new Vector2(-10f, -2.2f));
        player.Tags.Add("player");
        AddSystems(player);
        player.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.9f, 1f),
            TexturePath = "assets/sprites/hero-walk.png",
            SortingOrder = 10,
        });
        player.AddComponent(new SpriteAnimator
        {
            TexturePath = "assets/sprites/hero-walk.png",
            FrameWidth = 32,
            FrameHeight = 32,
            FrameCount = 4,
            Fps = 10f,
        });
        player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 1f) });
        player.AddComponent(new Rigidbody2D { AffectedByGravity = true });
        player.AddComponent(new ScriptComponent { ScriptPath = "scripts/Plataformas.cs", ClassName = "Plataformas" });
        player.AddComponent(new AudioSource { Clip = "assets/audio/jump.wav", PlayOnStart = false });

        var coinPositions = new[] { -5f, 0f, 5f };
        for (var i = 0; i < coinPositions.Length; i++)
        {
            var coin = scene.Create($"Moneda {i + 1}", new Vector2(coinPositions[i], -1.4f));
            coin.AddComponent(new SpriteRenderer
            {
                Size = new Vector2(0.6f, 0.6f),
                TexturePath = "assets/sprites/coin.png",
                SortingOrder = 8,
            });
            coin.AddComponent(new BoxCollider { Size = new Vector2(0.6f, 0.6f), IsTrigger = true });
            coin.AddComponent(new RotatorComponent { DegreesPerSecond = 90f });
            coin.AddComponent(new AudioSource { Clip = "assets/audio/coin.wav", PlayOnStart = false });
        }

        AddNote(scene, new Vector2(-3.2f, -1.4f));
        return scene;
    }

    private static Scene CreateHorror()
    {
        var scene = new Scene { Name = "Pasillo" };

        var camera = scene.Create("Cámara principal", new Vector2(-6f, -1f));
        camera.AddComponent(new Camera2D
        {
            OrthographicSize = 5.5f,
            Zoom = 1f,
            Ambient = new Vector4(0.35f, 0.36f, 0.42f, 1f),
            FollowTag = "player",
            FollowSmoothing = 4f,
        });

        var floor = scene.Create("Suelo", new Vector2(-8f, -2.5f));
        var tiles = floor.AddComponent(new Tilemap
        {
            Width = 16,
            Height = 2,
            TileSize = 1f,
            TexturePath = "assets/sprites/tiles.png",
            TilesetColumns = 4,
            SortingOrder = -10,
            Collides = true,
        });

        for (var x = 0; x < tiles.Width; x++)
        {
            tiles.SetTile(x, 0, 1);
            tiles.SetTile(x, 1, 2);
        }

        var leftWall = scene.Create("Pared izquierda", new Vector2(-8.2f, -0.5f));
        leftWall.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.4f, 4f),
            Tint = new Vector4(0.16f, 0.17f, 0.22f, 1f),
            SortingOrder = -2,
        });
        leftWall.AddComponent(new BoxCollider { Size = new Vector2(0.4f, 4f) });
        leftWall.AddComponent(new Rigidbody2D { IsStatic = true });

        var rightWall = scene.Create("Pared derecha", new Vector2(0.2f, -0.5f));
        rightWall.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.4f, 4f),
            Tint = new Vector4(0.16f, 0.17f, 0.22f, 1f),
            SortingOrder = -2,
        });
        rightWall.AddComponent(new BoxCollider { Size = new Vector2(0.4f, 4f) });
        rightWall.AddComponent(new Rigidbody2D { IsStatic = true });

        var player = scene.Create("Jugador", new Vector2(-7f, -1.6f));
        player.Tags.Add("player");
        AddSystems(player);
        player.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.8f, 1f),
            Tint = new Vector4(0.85f, 0.85f, 0.9f, 1f),
            SortingOrder = 10,
        });
        player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 1f) });
        player.AddComponent(new Rigidbody2D());
        player.AddComponent(new ScriptComponent { ScriptPath = "scripts/Terror.cs", ClassName = "Terror" });

        var note = scene.Create("Nota", new Vector2(-3f, -1.4f));
        note.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(0.5f, 0.6f),
            Tint = new Vector4(0.92f, 0.88f, 0.7f, 1f),
            SortingOrder = 5,
        });
        note.AddComponent(new BoxCollider { Size = new Vector2(1.4f, 1.6f), IsTrigger = true });

        var ambience = scene.Create("Ambiente", new Vector2(0f, 0f));
        ambience.AddComponent(new AudioSource
        {
            Clip = "assets/audio/ambient.wav",
            Volume = 0.35f,
            Loop = true,
            Music = true,
            PlayOnStart = true,
        });

        AddNote(scene, new Vector2(-2.2f, -1.2f));
        return scene;
    }

    private static void AddSystems(Entity player)
    {
        player.AddComponent(new Stats
        {
            Entries =
            {
                new StatEntry { Name = "Vida", Value = 100f, Max = 100f },
            },
        });

        player.AddComponent(new Inventory
        {
            Capacity = 8,
            Slots =
            {
                new InventorySlot { ItemId = "pocion", Quantity = 2 },
            },
        });
    }

    private static void AddLight(Entity entity, bool cone = false)
    {
        entity.AddComponent(new Light2D
        {
            Kind = cone ? LightKind.Cone : LightKind.Point,
            Radius = cone ? 8f : 5f,
            Intensity = cone ? 0.9f : 0.75f,
            ConeAngle = 55f,
            FlickerAmplitude = cone ? 0.12f : 0f,
            Color = new Vector4(1f, 0.93f, 0.8f, 1f),
        });
    }

    private static Scene CreateCorridor()
    {
        var scene = new Scene { Name = "Pasillo 2.5D" };

        var camera = scene.Create("Cámara FPS", new Vector2(0f, -6f));
        camera.Tags.Add("player");
        camera.AddComponent(new Camera3D
        {
            Height = 1.7f,
            FovDegrees = 72f,
            FogStart = 10f,
            FogEnd = 45f,
            Background = new Vector4(0.03f, 0.04f, 0.06f, 1f),
            FogColor = new Vector4(0.06f, 0.07f, 0.1f, 1f),
            Ambient = new Vector4(0.58f, 0.58f, 0.65f, 1f),
        });
        camera.AddComponent(new FirstPersonController { Speed = 3.6f });
        camera.AddComponent(new Health
        {
            Max = 100f,
            Current = 100f,
            DeactivateOnDeath = false,
            ShowOnHud = true,
            HudLabel = "Vida",
        });
        camera.AddComponent(new WeaponGroup { ActiveSlot = 1 });
        camera.AddComponent(new Weapon
        {
            Name = "Pistola",
            Slot = 1,
            Damage = 24f,
            Range = 25f,
            Cooldown = 0.25f,
            Ammo = 12,
            MagazineSize = 12,
            ReserveAmmo = 48,
            ReloadSeconds = 1.1f,
            Automatic = true,
        });
        camera.AddComponent(new Weapon
        {
            Name = "Escopeta",
            Slot = 2,
            Damage = 55f,
            Range = 18f,
            Cooldown = 0.8f,
            Ammo = 6,
            MagazineSize = 6,
            ReserveAmmo = 18,
            ReloadSeconds = 1.6f,
            Automatic = false,
            IsProjectile = true,
            ProjectileSpeed = 16f,
            ProjectileSize = 0.35f,
        });
        camera.AddComponent(new Respawn { Lives = 3, RespawnSeconds = 1.5f });
        camera.AddComponent(new Inventory { Capacity = 8 });
        camera.AddComponent(new WinLoseRules
        {
            PlayerTag = "player",
            EnemiesTag = "enemy",
            WinMessage = "¡Has ganado!",
            LoseMessage = "La Sombra te alcanzó",
        });
        camera.AddComponent(new IntroMessage
        {
            Text = "WASD + ratón · clic para disparar · E abre la puerta · recoge la llave",
            Duration = 6f,
        });

        AddWall(scene, "Suelo", new Vector2(0f, 13f), new Vector3(9f, 0.2f, 26f), new Vector3(0.3f, 0.27f, 0.22f), false, 0f, "assets/textures/floor.png", 2.2f);
        AddWall(scene, "Techo", new Vector2(0f, 13f), new Vector3(9f, 0.2f, 26f), new Vector3(0.3f, 0.29f, 0.33f), false, 3.6f, "assets/textures/ceiling.png", 2.4f);
        AddWall(scene, "Pared izquierda", new Vector2(-4.3f, 13f), new Vector3(0.4f, 3.6f, 26f), new Vector3(0.58f, 0.55f, 0.5f), true, 0f, "assets/textures/wall.png", 2.5f);
        AddWall(scene, "Pared derecha", new Vector2(4.3f, 13f), new Vector3(0.4f, 3.6f, 26f), new Vector3(0.58f, 0.55f, 0.5f), true, 0f, "assets/textures/wall.png", 2.5f);
        AddWall(scene, "Muro final", new Vector2(0f, 26f), new Vector3(9f, 3.6f, 0.4f), new Vector3(0.36f, 0.35f, 0.34f), true);
        AddWall(scene, "Puerta", new Vector2(0f, 16f), new Vector3(3f, 3f, 0.35f), new Vector3(0.72f, 0.42f, 0.2f), true);
        scene.Find("Puerta")!.AddComponent(new Door
        {
            KeyItemId = "llave",
            OpenHeight = 3.2f,
            Speed = 1.4f,
            InteractionRadius = 3f,
        });

        var lamp = scene.Create("Luz pasillo", new Vector2(0f, 8f));
        lamp.AddComponent(new Light2D
        {
            Radius = 11f,
            Intensity = 1.35f,
            Color = new Vector4(1f, 0.78f, 0.45f, 1f),
        });
        lamp.AddComponent(new AudioSource
        {
            Clip = "assets/audio/ambient.wav",
            Volume = 0.35f,
            Loop = true,
            Spatial = true,
            MaxDistance = 12f,
        });

        var lamp2 = scene.Create("Luz final", new Vector2(0f, 20f));
        lamp2.AddComponent(new Light2D
        {
            Radius = 9f,
            Intensity = 0.8f,
            Color = new Vector4(0.55f, 0.7f, 1f, 1f),
        });

        var enemy = scene.Create("Sombra", new Vector2(0.8f, 9f));
        enemy.Tags.Add("enemy");
        enemy.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/hero-walk.png",
            Size = new Vector2(1.2f, 1.4f),
            Tint = new Vector4(0.45f, 0.35f, 0.6f, 1f),
            SortingOrder = 10,
        });
        enemy.AddComponent(new SpriteAnimator
        {
            TexturePath = "assets/sprites/hero-walk.png",
            FrameWidth = 32,
            FrameHeight = 32,
            FrameCount = 4,
            Fps = 8f,
        });
        enemy.AddComponent(new Health
        {
            Max = 70f,
            Current = 70f,
            DeactivateOnDeath = true,
            ShowOnHud = false,
        });
        enemy.AddComponent(new AiAgent3D
        {
            TargetTag = "player",
            Speed = 1.4f,
            VisionRadius = 7f,
            PatrolRadius = 2f,
            MemorySeconds = 3f,
            AttackDamage = 14f,
            AttackRange = 1.5f,
            AttackCooldown = 1.1f,
        });

        var key = scene.Create("Llave", new Vector2(2.4f, 6f));
        key.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.6f, 0.6f),
            Tint = new Vector4(1f, 0.82f, 0.3f, 1f),
            SortingOrder = 12,
        });
        key.AddComponent(new RotatorComponent { DegreesPerSecond = 140f });
        key.AddComponent(new Pickup
        {
            Kind = PickupKind.Item,
            ItemId = "llave",
            Quantity = 1,
            Radius = 1.3f,
            Message = "Has encontrado la llave",
        });

        var checkpoint = scene.Create("Punto de control", new Vector2(0f, 12f));
        checkpoint.AddComponent(new Checkpoint3D { Radius = 2.5f, Message = "Punto de control" });

        var trigger = scene.Create("Aviso Sombra", new Vector2(0f, 8f));
        trigger.AddComponent(new Trigger3D
        {
            Radius = 3f,
            Action = ActionKind.Message,
            Message = "¡La Sombra está cerca!",
            Once = true,
        });

        var button = scene.Create("Botón puerta", new Vector2(2.6f, -3f));
        button.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(0.5f, 0.5f),
            Tint = new Vector4(0.4f, 0.9f, 0.6f, 1f),
            SortingOrder = 11,
        });
        button.AddComponent(new Button3D
        {
            Radius = 2f,
            TargetName = "Puerta",
            Action = ActionKind.OpenDoor,
            Message = "Puerta abierta desde el botón",
        });

        var coin = scene.Create("Moneda", new Vector2(-2.2f, 20f));
        coin.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.6f, 0.6f),
            SortingOrder = 12,
        });
        coin.AddComponent(new RotatorComponent { DegreesPerSecond = 120f });

        return scene;
    }
    private static Scene CreateTechnicalDemo()
    {
        var scene = new Scene { Name = "Demo técnico 2.5D" };

        var camera = scene.Create("Jugador", new Vector2(0f, 0f));
        camera.Tags.Add("player");
        camera.AddComponent(new Camera3D
        {
            Height = 1.7f,
            FovDegrees = 74f,
            FogStart = 12f,
            FogEnd = 52f,
            Background = new Vector4(0.025f, 0.035f, 0.055f, 1f),
            FogColor = new Vector4(0.05f, 0.06f, 0.09f, 1f),
            Ambient = new Vector4(0.62f, 0.62f, 0.7f, 1f),
        });
        camera.AddComponent(new FirstPersonController { Speed = 4f });
        camera.AddComponent(new Health
        {
            Max = 100f,
            Current = 100f,
            DeactivateOnDeath = false,
            ShowOnHud = true,
            HudLabel = "Vida",
        });
        camera.AddComponent(new WeaponGroup { ActiveSlot = 1 });
        camera.AddComponent(new Weapon
        {
            Name = "Pistola",
            Slot = 1,
            Damage = 28f,
            Range = 28f,
            Cooldown = 0.22f,
            Ammo = 12,
            MagazineSize = 12,
            ReserveAmmo = 72,
            ReloadSeconds = 1f,
            Automatic = true,
        });
        camera.AddComponent(new Weapon
        {
            Name = "Escopeta",
            Slot = 2,
            Damage = 62f,
            Range = 18f,
            Cooldown = 0.75f,
            Ammo = 6,
            MagazineSize = 6,
            ReserveAmmo = 24,
            ReloadSeconds = 1.5f,
            Automatic = false,
            IsProjectile = true,
            ProjectileSpeed = 17f,
            ProjectileSize = 0.35f,
        });
        camera.AddComponent(new Weapon
        {
            Name = "Rifle",
            Slot = 3,
            Damage = 18f,
            Range = 34f,
            Cooldown = 0.11f,
            Ammo = 30,
            MagazineSize = 30,
            ReserveAmmo = 120,
            ReloadSeconds = 1.3f,
            Automatic = true,
        });
        camera.AddComponent(new Respawn { Lives = 3, RespawnSeconds = 1.5f });
        camera.AddComponent(new Inventory { Capacity = 8 });
        camera.AddComponent(new VariableHud
        {
            VariableName = "enemigos_muertos",
            Label = "Bajas",
            Prefix = "Bajas: ",
            HideWhenEmpty = false,
        });
        camera.AddComponent(new WinLoseRules
        {
            PlayerTag = "player",
            EnemiesTag = "enemy",
            WinMessage = "¡Demo completada! Has eliminado a las tres Sombras.",
            LoseMessage = "La Sombra te alcanzó",
        });
        camera.AddComponent(new IntroMessage
        {
            Text = "Demo técnica 0.31 · WASD + ratón · 1/2/3 armas · R recarga · E interactúa · recoge la llave y elimina a las 3 Sombras",
            Duration = 8f,
        });

        AddWall(scene, "Suelo", new Vector2(0f, 18f), new Vector3(13f, 0.2f, 40f), new Vector3(0.28f, 0.25f, 0.21f), false, 0f, "assets/textures/floor.png", 2.2f);
        AddWall(scene, "Techo", new Vector2(0f, 18f), new Vector3(13f, 0.2f, 40f), new Vector3(0.3f, 0.29f, 0.34f), false, 3.6f, "assets/textures/ceiling.png", 2.4f);
        AddWall(scene, "Pared izquierda", new Vector2(-6.4f, 18f), new Vector3(0.4f, 3.6f, 40f), new Vector3(0.55f, 0.52f, 0.48f), true, 0f, "assets/textures/wall.png", 2.5f);
        AddWall(scene, "Pared derecha", new Vector2(6.4f, 18f), new Vector3(0.4f, 3.6f, 40f), new Vector3(0.55f, 0.52f, 0.48f), true, 0f, "assets/textures/wall.png", 2.5f);
        AddWall(scene, "Muro inicial", new Vector2(0f, -1.8f), new Vector3(13f, 3.6f, 0.4f), new Vector3(0.4f, 0.38f, 0.36f), true);
        AddWall(scene, "Muro final", new Vector2(0f, 38f), new Vector3(13f, 3.6f, 0.4f), new Vector3(0.35f, 0.34f, 0.33f), true);
        AddWall(scene, "Cobertura 1", new Vector2(-3f, 9f), new Vector3(5f, 3f, 0.5f), new Vector3(0.46f, 0.42f, 0.38f), true, 0f, "assets/textures/wall.png", 1.8f);
        AddWall(scene, "Cobertura 2", new Vector2(3f, 23f), new Vector3(5f, 3f, 0.5f), new Vector3(0.46f, 0.42f, 0.38f), true, 0f, "assets/textures/wall.png", 1.8f);
        AddWall(scene, "Puerta", new Vector2(0f, 15f), new Vector3(3f, 3f, 0.35f), new Vector3(0.72f, 0.42f, 0.2f), true);
        scene.Find("Puerta")!.AddComponent(new Door
        {
            KeyItemId = "llave",
            OpenHeight = 3.2f,
            Speed = 1.4f,
            InteractionRadius = 3f,
        });

        var key = scene.Create("Llave", new Vector2(3f, 6f));
        key.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.7f, 0.7f),
            Tint = new Vector4(1f, 0.82f, 0.3f, 1f),
            SortingOrder = 20,
        });
        key.AddComponent(new RotatorComponent { DegreesPerSecond = 150f });
        key.AddComponent(new Pickup
        {
            Kind = PickupKind.Item,
            ItemId = "llave",
            Quantity = 1,
            Radius = 1.4f,
            Message = "Llave conseguida: abre la puerta del fondo",
        });

        var healthPack = scene.Create("Botiquín", new Vector2(2.5f, 13f));
        healthPack.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(0.55f, 0.55f),
            Tint = new Vector4(0.35f, 0.95f, 0.55f, 1f),
            SortingOrder = 20,
        });
        healthPack.AddComponent(new Pickup
        {
            Kind = PickupKind.Health,
            HealthAmount = 35f,
            Radius = 1.2f,
            Message = "+35 de vida",
        });

        var checkpoint = scene.Create("Punto de control", new Vector2(0f, 12f));
        checkpoint.AddComponent(new Checkpoint3D { Radius = 2.5f, Message = "Checkpoint: la Sombra está cerca" });

        var trigger = scene.Create("Aviso Sombra", new Vector2(0f, 7f));
        trigger.AddComponent(new Trigger3D
        {
            Radius = 3f,
            Action = ActionKind.Message,
            Message = "Una Sombra ronda el pasillo",
            Once = true,
        });

        var lava = scene.Create("Lava", new Vector2(-4f, 22f));
        lava.AddComponent(new Wall3D
        {
            Size = new Vector3(3.5f, 0.15f, 4f),
            Color = new Vector3(0.7f, 0.18f, 0.12f),
            Collides = false,
            BaseY = 0.02f,
            TexturePath = "assets/textures/floor.png",
            TextureScale = 1.5f,
        });
        lava.AddComponent(new Hazard3D { Radius = 2.3f, DamagePerSecond = 28f });

        var arena = scene.Create("Arena", new Vector2(0f, 31f));
        var teleporter = scene.Create("Portal", new Vector2(5f, 24f));
        teleporter.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.8f, 0.8f),
            Tint = new Vector4(0.5f, 0.7f, 1f, 1f),
            SortingOrder = 20,
        });
        teleporter.AddComponent(new Teleporter3D
        {
            Radius = 1.6f,
            TargetName = "Arena",
            Message = "Portal a la arena final",
        });

        AddDemoEnemy(scene, "Sombra 1", new Vector2(2f, 9f), 60f, 12f, 1.5f, new Vector2(1.2f, 1.4f), false);
        AddDemoEnemy(scene, "Sombra 2", new Vector2(-3f, 20f), 80f, 14f, 1.55f, new Vector2(1.2f, 1.4f), false);
        AddDemoEnemy(scene, "Guardián", new Vector2(0f, 33f), 200f, 20f, 1.15f, new Vector2(2f, 2.4f), true);

        var crate = scene.Create("Caja", new Vector2(4.5f, 10f));
        crate.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(1f, 1f),
            SortingOrder = 12,
        });

        var guide = scene.Create("Guía", new Vector2(0f, 3f));
        var graph = guide.AddComponent(new NodeGraph());
        var timer = graph.AddNode(GraphNodeKind.Event, 0f, 0f);
        timer.Event = GraphEventKind.OnTimer;
        timer.Interval = 0.5f;
        timer.Once = false;

        var half = graph.AddNode(GraphNodeKind.Condition, 0f, 120f);
        half.Condition = GraphConditionKind.Expression;
        half.Expression = "enemigos_muertos >= 2 && aviso_mitad == 0";

        var flag = graph.AddNode(GraphNodeKind.Action, 0f, 240f);
        flag.Action = ActionKind.SetVariable;
        flag.VariableName = "aviso_mitad";
        flag.Expression = "1";

        var message = graph.AddNode(GraphNodeKind.Action, 0f, 360f);
        message.Action = ActionKind.Message;
        message.Message = "¡Dos Sombras menos! El Guardián espera al final";

        timer.Next = half.Id;
        half.TrueNext = flag.Id;
        flag.Next = message.Id;

        var lamp = scene.Create("Luz entrada", new Vector2(0f, 5f));
        lamp.AddComponent(new Light2D { Radius = 12f, Intensity = 1.2f, Color = new Vector4(1f, 0.8f, 0.5f, 1f) });
        lamp.AddComponent(new AudioSource
        {
            Clip = "assets/audio/ambient.wav",
            Volume = 0.32f,
            Loop = true,
            Spatial = true,
            MaxDistance = 16f,
        });

        var lamp2 = scene.Create("Luz pasillo", new Vector2(0f, 20f));
        lamp2.AddComponent(new Light2D { Radius = 12f, Intensity = 1.1f, Color = new Vector4(1f, 0.72f, 0.42f, 1f) });

        var lamp3 = scene.Create("Luz arena", new Vector2(0f, 32f));
        lamp3.AddComponent(new Light2D { Radius = 14f, Intensity = 1.25f, Color = new Vector4(0.6f, 0.72f, 1f, 1f) });

        return scene;
    }

    private static void AddDemoEnemy(
        Scene scene,
        string name,
        Vector2 position,
        float health,
        float damage,
        float speed,
        Vector2 size,
        bool boss)
    {
        var enemy = scene.Create(name, position);
        enemy.Tags.Add("enemy");
        enemy.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/hero-walk.png",
            Size = size,
            Tint = boss ? new Vector4(0.9f, 0.25f, 0.2f, 1f) : new Vector4(0.45f, 0.35f, 0.6f, 1f),
            SortingOrder = boss ? 14 : 10,
        });
        enemy.AddComponent(new SpriteAnimator
        {
            TexturePath = "assets/sprites/hero-walk.png",
            FrameWidth = 32,
            FrameHeight = 32,
            FrameCount = 4,
            Fps = boss ? 6f : 8f,
        });
        enemy.AddComponent(new Health
        {
            Max = health,
            Current = health,
            DeactivateOnDeath = true,
            ShowOnHud = false,
            DeathVariable = "enemigos_muertos",
            DeathVariableAmount = 1f,
        });
        enemy.AddComponent(new AiAgent3D
        {
            TargetTag = "player",
            Speed = speed,
            VisionRadius = boss ? 11f : 9f,
            PatrolRadius = boss ? 3f : 2f,
            MemorySeconds = 4f,
            AttackDamage = damage,
            AttackRange = boss ? 2f : 1.5f,
            AttackCooldown = boss ? 1.4f : 1.1f,
            UseNavigation = true,
        });
    }
    private static void AddWall(Scene scene, string name, Vector2 position, Vector3 size, Vector3 color, bool collides, float baseY = 0f, string? texture = null, float textureScale = 2f)
    {
        var wall = scene.Create(name, position);
        wall.AddComponent(new Wall3D
        {
            Size = size,
            Color = color,
            Collides = collides,
            BaseY = baseY,
            TexturePath = texture,
            TextureScale = textureScale,
        });
    }

    private static Scene CreateTutorial()
    {
        var scene = new Scene { Name = "Tutorial" };

        var camera = scene.Create("Cámara principal", new Vector2(0f, 0f));
        camera.AddComponent(new Camera2D
        {
            OrthographicSize = 7f,
            Zoom = 1f,
            Ambient = new Vector4(0.75f, 0.76f, 0.82f, 1f),
            FollowTag = "player",
            FollowSmoothing = 5f,
        });

        var ground = scene.Create("Suelo", new Vector2(-10f, -3f));
        var tiles = ground.AddComponent(new Tilemap
        {
            Width = 20,
            Height = 2,
            TileSize = 1f,
            TexturePath = "assets/sprites/tiles.png",
            TilesetColumns = 4,
            SortingOrder = -10,
            Collides = true,
        });

        for (var x = 0; x < tiles.Width; x++)
        {
            tiles.SetTile(x, 0, 1);
            tiles.SetTile(x, 1, 2);
        }

        var player = scene.Create("Jugador", new Vector2(-6f, -1.8f));
        player.Tags.Add("player");
        player.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/hero-walk.png",
            Size = new Vector2(0.9f, 1f),
            SortingOrder = 10,
        });
        player.AddComponent(new SpriteAnimator
        {
            TexturePath = "assets/sprites/hero-walk.png",
            FrameWidth = 32,
            FrameHeight = 32,
            FrameCount = 4,
            Fps = 10f,
        });
        player.AddComponent(new BoxCollider { Size = new Vector2(0.8f, 1f) });
        player.AddComponent(new Rigidbody2D());
        player.AddComponent(new ScriptComponent { ScriptPath = "scripts/Tutorial.cs", ClassName = "Tutorial" });
        AddSystems(player);

        var note = scene.Create("Nota", new Vector2(-3f, -1.8f));
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
            Radius = 2f,
        });

        var coin = scene.Create("Moneda", new Vector2(0f, -1.6f));
        coin.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.55f, 0.55f),
            SortingOrder = 20,
        });
        coin.AddComponent(new RotatorComponent { DegreesPerSecond = 120f });

        var enemy = scene.Create("Merodeador", new Vector2(3f, -1.8f));
        enemy.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(0.9f, 0.9f),
            Tint = new Vector4(0.5f, 0.42f, 0.6f, 1f),
            SortingOrder = 8,
        });
        enemy.AddComponent(new AiAgent
        {
            TargetTag = "player",
            Speed = 1.5f,
            VisionRadius = 4f,
            PatrolRadius = 2f,
        });

        return scene;
    }

    private static void AddNote(Scene scene, Vector2 position)
    {
        var note = scene.Create("Nota", position);
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
    }

    public const string PotionItem = """
        { "id": "pocion", "name": "Poción", "description": "Recupera 25 de vida.", "maxStack": 5 }
        """;

    public const string CoinItem = """
        { "id": "moneda", "name": "Moneda", "description": "Moneda de cambio.", "maxStack": 99 }
        """;

    public const string WelcomeDialogue = """
        {
          "id": "bienvenida",
          "speaker": "Nota",
          "lines": [
            { "text": "Bienvenido a tu nuevo proyecto de Ollin." },
            { "text": "A/D para moverte, espacio para saltar y E para interactuar." }
          ]
        }
        """;

    public const string CorridorScriptSource = """
        using System;
        using System.Numerics;
        using Ollin.Core.Input;
        using Ollin.Core.Model;
        using Ollin.Scripting;

        /// <summary>Recorrido 2.5D: muestra el HUD y abre la puerta con E.</summary>
        public sealed class Pasillo : OllinScript
        {
            private Entity? _door;
            private bool _opened;

            public override void OnStart()
            {
                Hud.Message("WASD + ratón para moverte · E abre la puerta", 5f);
                _door = Scene.Find("Puerta");
            }

            public override void OnUpdate(float deltaTime)
            {
                if (_opened || _door is null)
                {
                    return;
                }

                var distance = Vector2.Distance(Entity.Transform.Position, _door.Transform.Position);
                Hud.SetLabel("Puerta", distance < 3.5f ? "E para abrir" : "");

                if (distance < 3.5f && Input.GetKeyDown(KeyCode.E))
                {
                    _opened = true;
                    _door.Active = false;
                    Hud.Message("La puerta se abre", 2f);
                    Audio.Play("assets/audio/menu-elegir.wav", 0.6f);
                }
            }
        }
        """;

    public const string TutorialScriptSource = """
        using System.Numerics;
        using Ollin.Core.Input;
        using Ollin.Core.Model.Components;
        using Ollin.Scripting;

        /// <summary>
        /// Proyecto de tutorial: movimiento, HUD, estadísticas, inventario,
        /// interacción y guardado. Lee los comentarios y cambia lo que quieras.
        /// </summary>
        public sealed class Tutorial : OllinScript
        {
            private float _verticalSpeed;
            private bool _grounded;

            public override void OnStart()
            {
                // 1) HUD: barras y etiquetas.
                Hud.SetBar("Vida", 100f, 100f);
                Hud.SetLabel("Monedas", "0");
                Hud.Message("A/D para moverte, espacio para saltar, E para leer", 4f);

                // 2) Datos persistentes: se guardan con la partida.
                if (!Save.Has("tutorial_visto"))
                {
                    Save.SetFlag("tutorial_visto", true);
                    Debug("Primera vez en el tutorial");
                }
            }

            public override void OnUpdate(float deltaTime)
            {
                // 3) Movimiento con física sencilla.
                var body = Entity.GetComponent<Rigidbody2D>();
                var direction = 0f;
                if (Input.GetKey(KeyCode.A)) direction -= 1f;
                if (Input.GetKey(KeyCode.D)) direction += 1f;

                if (body is not null)
                {
                    body.Velocity = new Vector2(direction * 5f, body.Velocity.Y);
                }

                _grounded = Entity.Transform.Position.Y <= -1.8f;

                if (Input.GetKeyDown(KeyCode.Space) && _grounded)
                {
                    _verticalSpeed = 7.5f;
                    Audio.Play("assets/audio/jump.wav", 0.5f);
                }

                if (_verticalSpeed != 0f)
                {
                    _verticalSpeed -= 20f * deltaTime;
                    Entity.Transform.Position += new Vector2(0f, _verticalSpeed * deltaTime);

                    if (Entity.Transform.Position.Y <= -1.8f)
                    {
                        Entity.Transform.Position = new Vector2(Entity.Transform.Position.X, -1.8f);
                        _verticalSpeed = 0f;
                    }
                }

                // 4) Estadísticas e inventario reflejados en el HUD.
                var stats = Entity.GetComponent<Stats>();
                if (stats is not null) Hud.SetBar("Vida", stats.Get("Vida"), 100f);

                var bag = Entity.GetComponent<Inventory>();
                if (bag is not null) Hud.SetLabel("Monedas", bag.Count("moneda").ToString());
            }

            public override void OnInteract(Ollin.Core.Model.Entity other)
            {
                // 5) Al interactuar con la nota, además del diálogo, damos una moneda.
                var bag = Entity.GetComponent<Inventory>();
                bag?.Add("moneda", 1);
                Hud.Message("Has encontrado una moneda", 2f);
            }

            public override void OnCollisionEnter(Ollin.Core.Model.Entity other)
            {
                Debug("Choque con " + other.Name);
            }
        }
        """;

    public const string ScriptSource = """
        using System.Numerics;
        using Ollin.Core.Input;
        using Ollin.Scripting;

        /// <summary>Script inicial: mueve la entidad con A/D y la hace saltar con espacio.</summary>
        public sealed class Juego : OllinScript
        {
            private float _verticalSpeed;

            public override void OnStart()
            {
                Debug("Juego listo: A/D para moverte, espacio para saltar");
                Hud.SetBar("Vida", 100f, 100f);
                Hud.Message("A/D para moverte, espacio para saltar", 3f);
            }

            public override void OnUpdate(float deltaTime)
            {
                var direction = 0f;
                if (Input.GetKey(KeyCode.A)) direction -= 1f;
                if (Input.GetKey(KeyCode.D)) direction += 1f;

                Entity.Transform.Position += new Vector2(direction * 4f * Time.DeltaTime, 0f);

                if (Input.GetKeyDown(KeyCode.Space) && _verticalSpeed == 0f)
                {
                    _verticalSpeed = 7f;
                    Audio.Play("assets/audio/jump.wav", 0.7f);
                }

                if (_verticalSpeed != 0f)
                {
                    _verticalSpeed -= 18f * deltaTime;
                    Entity.Transform.Position += new Vector2(0f, _verticalSpeed * deltaTime);

                    if (Entity.Transform.Position.Y <= -1.6f)
                    {
                        Entity.Transform.Position = new Vector2(Entity.Transform.Position.X, -1.6f);
                        _verticalSpeed = 0f;
                    }
                }
            }
        }
        """;

    public const string PlatformerScriptSource = """
        using System.Numerics;
        using Ollin.Core.Input;
        using Ollin.Core.Model.Components;
        using Ollin.Scripting;

        /// <summary>Jugador de plataformas: A/D para correr y espacio para saltar.</summary>
        public sealed class Plataformas : OllinScript
        {
            private float _verticalSpeed;
            private bool _grounded;

            public override void OnStart()
            {
                Debug("Plataformas: A/D para correr, espacio para saltar");
                Hud.SetBar("Vida", 100f, 100f);
                Hud.SetLabel("Monedas", "0");
                Hud.Message("Recoge las monedas", 3f);
            }

            public override void OnUpdate(float deltaTime)
            {
                var body = Entity.GetComponent<Rigidbody2D>();
                var direction = 0f;
                if (Input.GetKey(KeyCode.A)) direction -= 1f;
                if (Input.GetKey(KeyCode.D)) direction += 1f;

                if (body is not null)
                {
                    body.Velocity = new Vector2(direction * 6f, body.Velocity.Y);
                }
                else
                {
                    Entity.Transform.Position += new Vector2(direction * 6f * deltaTime, 0f);
                }

                _grounded = Entity.Transform.Position.Y <= -2.2f;

                if (Input.GetKeyDown(KeyCode.Space) && _grounded)
                {
                    _verticalSpeed = 8f;
                    Audio.Play("assets/audio/jump.wav", 0.6f);
                }

                if (_verticalSpeed != 0f)
                {
                    _verticalSpeed -= 22f * deltaTime;
                    Entity.Transform.Position += new Vector2(0f, _verticalSpeed * deltaTime);

                    if (Entity.Transform.Position.Y <= -2.2f)
                    {
                        Entity.Transform.Position = new Vector2(Entity.Transform.Position.X, -2.2f);
                        _verticalSpeed = 0f;
                    }
                }
            }
        }
        """;

    public const string HorrorScriptSource = """
        using System.Numerics;
        using Ollin.Core.Input;
        using Ollin.Core.Model;
        using Ollin.Scripting;

        /// <summary>Pasillo: avanza despacio y descubre la nota al tocarla.</summary>
        public sealed class Terror : OllinScript
        {
            private bool _found;

            public override void OnStart()
            {
                Debug("Pasillo: muévete con A/D y busca la nota");
                Audio.Music("assets/audio/ambient.wav", 0.3f);
                Hud.SetBar("Cordura", 100f, 100f, new System.Numerics.Vector4(0.6f, 0.4f, 0.9f, 1f));
                Hud.Message("Encuentra la nota", 3f);
            }

            public override void OnUpdate(float deltaTime)
            {
                var direction = 0f;
                if (Input.GetKey(KeyCode.A)) direction -= 1f;
                if (Input.GetKey(KeyCode.D)) direction += 1f;

                Entity.Transform.Position += new Vector2(direction * 2.2f * Time.DeltaTime, 0f);
            }

            public override void OnTriggerEnter(Entity other)
            {
                if (_found || other.Name != "Nota")
                {
                    return;
                }

                _found = true;
                Debug("La nota dice: «no mires atrás»");
                Audio.Play("assets/audio/hit.wav", 0.5f);
            }
        }
        """;
}
