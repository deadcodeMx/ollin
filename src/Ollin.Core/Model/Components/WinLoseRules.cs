using System.Text.Json.Serialization;
using Ollin.Core.Hud;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Reglas de victoria/derrota sin código: pierde si el jugador muere y gana cuando
/// no queda ningún enemigo vivo. Muestra el resultado en el HUD.
/// </summary>
public sealed class WinLoseRules : Component, ITickable
{
    public string PlayerTag { get; set; } = "player";

    public string EnemiesTag { get; set; } = "enemy";

    public bool WinWhenEnemiesDead { get; set; } = true;

    public bool LoseWhenPlayerDead { get; set; } = true;

    public string WinMessage { get; set; } = "¡Has ganado!";

    public string LoseMessage { get; set; } = "Has caído";

    [JsonIgnore]
    public bool Finished { get; private set; }

    private bool _sawEnemy;

    public void OnUpdate(float deltaTime)
    {
        if (Finished || Owner?.Scene is not { } scene)
        {
            return;
        }

        var player = Find(scene, PlayerTag);
        var playerHealth = player?.GetComponent<Health>();

        if (LoseWhenPlayerDead && playerHealth is { IsDead: true })
        {
            var respawn = player?.GetComponent<Respawn>();

            if (respawn is not { CanRespawn: true })
            {
                Finished = true;
                HudHost.GameOver(LoseMessage);
                return;
            }
        }

        if (!WinWhenEnemiesDead)
        {
            return;
        }

        var enemies = 0;

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || !entity.Tags.Any(tag => tag.Equals(EnemiesTag, StringComparison.OrdinalIgnoreCase)))
            {
                continue;
            }

            var health = entity.GetComponent<Health>();

            if (health is { IsDead: true })
            {
                continue;
            }

            enemies++;
            _sawEnemy = true;
        }

        if (_sawEnemy && enemies == 0)
        {
            Finished = true;
            HudHost.GameOver(WinMessage);
        }
    }

    private static Entity? Find(Scenes.Scene scene, string tag)
    {
        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.Tags.Any(value => value.Equals(tag, StringComparison.OrdinalIgnoreCase)))
            {
                return entity;
            }
        }

        return null;
    }

    public override Component CloneFor(Entity newOwner) => new WinLoseRules
    {
        Enabled = Enabled,
        PlayerTag = PlayerTag,
        EnemiesTag = EnemiesTag,
        WinWhenEnemiesDead = WinWhenEnemiesDead,
        LoseWhenPlayerDead = LoseWhenPlayerDead,
        WinMessage = WinMessage,
        LoseMessage = LoseMessage,
        Owner = newOwner,
    };
}

/// <summary>Muestra un mensaje de bienvenida la primera vez que arranca la partida.</summary>
public sealed class IntroMessage : Component, ITickable
{
    public string Text { get; set; } = "WASD + ratón para moverte · clic para disparar";

    public float Duration { get; set; } = 5f;

    [JsonIgnore]
    public bool Shown { get; private set; }

    public void OnUpdate(float deltaTime)
    {
        if (Shown)
        {
            return;
        }

        Shown = true;
        HudHost.ShowMessage(Text, Duration);
    }

    public override Component CloneFor(Entity newOwner) => new IntroMessage
    {
        Enabled = Enabled,
        Text = Text,
        Duration = Duration,
        Owner = newOwner,
    };
}
