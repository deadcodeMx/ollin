using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Projects;

/// <summary>Contenido inicial de un proyecto nuevo.</summary>
public static class StarterProject
{
    public static Scene CreateScene()
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
        player.AddComponent(new ScriptComponent
        {
            ScriptPath = "scripts/Juego.cs",
            ClassName = "Juego",
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
}
