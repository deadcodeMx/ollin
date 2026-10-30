using System;
using System.Numerics;
using Ollin.Scripting;

/// <summary>
/// Script de ejemplo: hace flotar al jugador alrededor de su posición inicial.
/// Se recarga al pulsar Reproducir o el botón «Recargar scripts».
/// </summary>
public sealed class PlayerBob : OllinScript
{
    private Vector2 _origin;
    private float _time;

    public override void OnStart()
    {
        _origin = Entity.Transform.Position;
        Debug("PlayerBob: el jugador flota mientras se reproduce");
    }

    public override void OnUpdate(float deltaTime)
    {
        _time += deltaTime;
        Entity.Transform.Position = _origin + new Vector2(0f, MathF.Sin(_time * 2.2f) * 0.28f);
    }

    public override void OnDestroy()
    {
        Entity.Transform.Position = _origin;
    }
}
