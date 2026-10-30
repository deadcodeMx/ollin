using System;
using System.Numerics;
using Ollin.Core.Input;
using Ollin.Scripting;

/// <summary>
/// Script de ejemplo: hace flotar al jugador y lo mueve con A/D.
/// Se recarga solo al guardar el archivo, sin detener la reproducción.
/// </summary>
public sealed class PlayerBob : OllinScript
{
    private Vector2 _origin;
    private float _time;
    private float _horizontal;

    public override void OnStart()
    {
        _origin = Entity.Transform.Position;
        Hud.SetBar("Vida", 100f, 100f);
        Hud.SetLabel("Monedas", "0");
        Hud.Message("Busca la nota y pulsa E", 4f);
        Debug("PlayerBob: mueve al jugador con A/D; flota mientras se reproduce");
    }

    public override void OnUpdate(float deltaTime)
    {
        _time += deltaTime;

        var direction = 0f;
        if (Input.GetKey(KeyCode.A))
        {
            direction -= 1f;
        }

        if (Input.GetKey(KeyCode.D))
        {
            direction += 1f;
        }

        _horizontal += direction * 2.5f * Time.DeltaTime;

        Entity.Transform.Position = _origin + new Vector2(_horizontal, MathF.Sin(_time * 2.2f) * 0.28f);
    }

    public override void OnDestroy()
    {
        Entity.Transform.Position = _origin;
    }
}
