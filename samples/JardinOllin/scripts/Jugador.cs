using System.Linq;
using System.Numerics;
using Ollin.Core.Input;
using Ollin.Scripting;

public sealed class Jugador : OllinScript
{
    private int _puntos;

    public override void OnStart()
    {
        Hud.SetLabel("puntos", "0 / 3");
        Hud.Message("Recoge las tres luces del jardín", 3f);
    }

    public override void OnUpdate(float deltaTime)
    {
        var dir = Vector2.Zero;
        if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.Left)) dir.X -= 1f;
        if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.Right)) dir.X += 1f;
        if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.Up)) dir.Y += 1f;
        if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.Down)) dir.Y -= 1f;
        if (dir.LengthSquared() > 0f) Position += Vector2.Normalize(dir) * 4f * deltaTime;

        foreach (var entity in Scene!.Entities.ToList())
        {
            if (!entity.Active || !entity.Tags.Contains("coleccionable") || DistanceTo(entity) > 0.9f) continue;
            entity.Active = false;
            _puntos++;
            Hud.SetLabel("puntos", $"{_puntos} / 3");
            Hud.Message($"¡{entity.Name} recogida!", 1.5f);
            if (_puntos >= 3) Hud.Message("¡Jardín completo!", 4f);
        }
    }
}