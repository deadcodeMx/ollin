using System;
using System.Numerics;
using Ollin.Core.Input;
using Ollin.Scripting;

/// <summary>Script de Entidad.</summary>
public sealed class Prueba : OllinScript
{
    private Vector2 _origin;

    public override void OnStart()
    {
        _origin = Entity.Transform.Position;
        Debug("Prueba iniciado en " + Entity.Name);
    }

    public override void OnUpdate(float deltaTime)
    {
        // Movimiento con WASD; cambia o borra este ejemplo.
        var direction = Vector2.Zero;
        if (Input.GetKey(KeyCode.A)) direction.X -= 1f;
        if (Input.GetKey(KeyCode.D)) direction.X += 1f;
        if (Input.GetKey(KeyCode.W)) direction.Y += 1f;
        if (Input.GetKey(KeyCode.S)) direction.Y -= 1f;

        if (direction != Vector2.Zero)
        {
            Entity.Transform.Position += Vector2.Normalize(direction) * 3f * Time.DeltaTime;
        }
    }
}