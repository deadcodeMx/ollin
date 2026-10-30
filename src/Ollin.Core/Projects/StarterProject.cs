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
}

/// <summary>Contenido inicial de un proyecto nuevo, según su plantilla.</summary>
public static class StarterProject
{
    public static Scene CreateScene(ProjectTemplate template = ProjectTemplate.Empty) => template switch
    {
        ProjectTemplate.Platformer => CreatePlatformer(),
        ProjectTemplate.Horror => CreateHorror(),
        _ => CreateEmpty(),
    };

    public static (string FileName, string Source) ScriptFor(ProjectTemplate template) => template switch
    {
        ProjectTemplate.Platformer => ("Plataformas.cs", PlatformerScriptSource),
        ProjectTemplate.Horror => ("Terror.cs", HorrorScriptSource),
        _ => ("Juego.cs", ScriptSource),
    };

    private static Scene CreateEmpty()
    {
        var scene = new Scene { Name = "Inicio" };

        var camera = scene.Create("Cámara principal", new Vector2(0f, -0.5f));
        camera.AddComponent(new Camera2D { OrthographicSize = 6f, Zoom = 1f });

        var ground = scene.Create("Suelo", new Vector2(0f, -2.5f));
        ground.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(16f, 0.5f),
            Tint = new Vector4(0.24f, 0.29f, 0.36f, 1f),
            SortingOrder = -10,
        });

        var player = scene.Create("Jugador", new Vector2(0f, -1.6f));
        player.AddComponent(new SpriteRenderer
        {
            Size = new Vector2(1f, 1f),
            Tint = new Vector4(0.91f, 0.64f, 0.23f, 1f),
            SortingOrder = 10,
        });
        player.AddComponent(new BoxCollider { Size = new Vector2(1f, 1f) });
        player.AddComponent(new Rigidbody2D());
        player.AddComponent(new ScriptComponent { ScriptPath = "scripts/Juego.cs", ClassName = "Juego" });

        return scene;
    }

    private static Scene CreatePlatformer()
    {
        var scene = new Scene { Name = "Nivel 1" };

        var camera = scene.Create("Cámara principal", new Vector2(0f, 0f));
        camera.AddComponent(new Camera2D { OrthographicSize = 7f, Zoom = 1f });

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

        return scene;
    }

    private static Scene CreateHorror()
    {
        var scene = new Scene { Name = "Pasillo" };

        var camera = scene.Create("Cámara principal", new Vector2(-6f, -1f));
        camera.AddComponent(new Camera2D { OrthographicSize = 5.5f, Zoom = 1f });

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

        return scene;
    }

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
