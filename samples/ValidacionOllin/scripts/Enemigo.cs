using System;
using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Scripting;

public sealed class Enemigo : OllinScript
{
    private Vector2 _origin;
    private float _direction = 1f;

    public override void OnStart()
    {
        _origin = Position;
    }

    public override void OnUpdate(float deltaTime)
    {
        var player = Find("Jugador");
        if (player is null) return;

        var distance = Vector2.Distance(WorldPosition, player.Transform.Position);
        if (distance < 4f)
        {
            var delta = player.Transform.Position - Position;
            if (MathF.Abs(delta.X) > 0.05f)
                _direction = MathF.Sign(delta.X);
        }
        else if (Position.X > _origin.X + 3.5f)
        {
            _direction = -1f;
        }
        else if (Position.X < _origin.X - 3.5f)
        {
            _direction = 1f;
        }

        Position += new Vector2(_direction * 1.4f * deltaTime, 0f);
        if (Position.Y < -2f) Position = new Vector2(_origin.X, 1.1f);
    }
}