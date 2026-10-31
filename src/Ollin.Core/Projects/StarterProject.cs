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
}

/// <summary>Contenido inicial de un proyecto nuevo, según su plantilla.</summary>
public static class StarterProject
{
    public static Scene CreateScene(ProjectTemplate template = ProjectTemplate.Empty) => template switch
    {
        ProjectTemplate.Platformer => CreatePlatformer(),
        ProjectTemplate.Horror => CreateHorror(),
        ProjectTemplate.Tutorial => CreateTutorial(),
        ProjectTemplate.Corridor => CreateCorridor(),
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
        camera.AddComponent(new ScriptComponent { ScriptPath = "scripts/Pasillo.cs", ClassName = "Pasillo" });

        AddWall(scene, "Suelo", new Vector2(0f, 13f), new Vector3(9f, 0.2f, 26f), new Vector3(0.3f, 0.27f, 0.22f), false);
        AddWall(scene, "Techo", new Vector2(0f, 13f), new Vector3(9f, 0.2f, 26f), new Vector3(0.3f, 0.29f, 0.33f), false, 3.6f);
        AddWall(scene, "Pared izquierda", new Vector2(-4.3f, 13f), new Vector3(0.4f, 3.6f, 26f), new Vector3(0.58f, 0.55f, 0.5f), true);
        AddWall(scene, "Pared derecha", new Vector2(4.3f, 13f), new Vector3(0.4f, 3.6f, 26f), new Vector3(0.58f, 0.55f, 0.5f), true);
        AddWall(scene, "Muro final", new Vector2(0f, 26f), new Vector3(9f, 3.6f, 0.4f), new Vector3(0.36f, 0.35f, 0.34f), true);
        AddWall(scene, "Puerta", new Vector2(0f, 16f), new Vector3(3f, 3f, 0.35f), new Vector3(0.72f, 0.42f, 0.2f), true);

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
        enemy.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/crate.png",
            Size = new Vector2(1.2f, 1.4f),
            Tint = new Vector4(0.45f, 0.35f, 0.6f, 1f),
            SortingOrder = 10,
        });

        var coin = scene.Create("Moneda", new Vector2(2.2f, 20f));
        coin.AddComponent(new SpriteRenderer
        {
            TexturePath = "assets/sprites/coin.png",
            Size = new Vector2(0.6f, 0.6f),
            SortingOrder = 12,
        });
        coin.AddComponent(new RotatorComponent { DegreesPerSecond = 120f });

        return scene;
    }

    private static void AddWall(Scene scene, string name, Vector2 position, Vector3 size, Vector3 color, bool collides, float baseY = 0f)
    {
        var wall = scene.Create(name, position);
        wall.AddComponent(new Wall3D
        {
            Size = size,
            Color = color,
            Collides = collides,
            BaseY = baseY,
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
