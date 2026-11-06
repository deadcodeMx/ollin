using System;
using System.Numerics;
using Ollin.Core.Model;
using Ollin.Scripting;

public sealed class Jugador : OllinScript
{
    private float _health = 3f;
    private int _coins;
    private float _invulnerable;
    private bool _finished;

    public override void OnStart()
    {
        _health = Save.GetNumber("validacion_vida", 3f);
        _coins = (int)Save.GetNumber("validacion_monedas", 0f);
        Hud.SetBar("Vida", _health, 3f, new Vector4(0.95f, 0.3f, 0.35f, 1f));
        Hud.SetLabel("Monedas", $"{_coins}/3");
        Hud.Message("Validacion Ollin - A/D mover, Espacio saltar, E interactuar", 4f);
    }

    public override void OnUpdate(float deltaTime)
    {
        _invulnerable = MathF.Max(0f, _invulnerable - deltaTime);
        Hud.SetBar("Vida", _health, 3f, new Vector4(0.95f, 0.3f, 0.35f, 1f));
        Hud.SetLabel("Monedas", $"{_coins}/3");
        if (_finished && Input.GetKeyDown(Ollin.Core.Input.KeyCode.R))
        {
            _health = 3f;
            _coins = 0;
            _finished = false;
            Save.SetNumber("validacion_vida", _health);
            Save.SetNumber("validacion_monedas", 0f);
            Position = new Vector2(1f, 2.2f);
            Hud.SetLabel("Monedas", "0/3");
        }
    }

    public override void OnTriggerEnter(Entity other)
    {
        if (other.Tags.Contains("coin"))
        {
            _coins++;
            Save.SetNumber("validacion_monedas", _coins);
            PlaySound("assets/audio/coin.wav", 0.8f);
            Hud.Message($"Moneda {_coins}/3", 1f);
            Destroy(other);
        }
        else if (other.Tags.Contains("enemy") && _invulnerable <= 0f && !_finished)
        {
            _health--;
            Save.SetNumber("validacion_vida", _health);
            _invulnerable = 1f;
            PlaySound("assets/audio/hit.wav", 0.8f);
            ShakeCamera(0.12f, 0.18f);
            Hud.Message("Golpe. Vida: " + _health, 1.2f);
            if (_health <= 0f)
            {
                _finished = true;
                Save.SetFlag("validacion_derrota", true);
                Hud.GameOver("Fin de la validacion - R para reintentar");
            }
        }
        else if (other.Tags.Contains("goal") && !_finished)
        {
            _finished = true;
            Save.SetFlag("validacion_completada", true);
            Save.SetNumber("validacion_monedas", _coins);
            PlaySound("assets/audio/jump.wav", 0.8f);
            Hud.GameOver("Nivel completado - prueba guardar y cargar");
        }
    }
}