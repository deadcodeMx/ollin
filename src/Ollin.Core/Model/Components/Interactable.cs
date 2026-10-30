using System.Text.Json.Serialization;

namespace Ollin.Core.Model.Components;

/// <summary>
/// Punto de interacción: muestra un aviso cuando el jugador está cerca y, al
/// pulsar la tecla de interacción, inicia un diálogo o avisa a los scripts.
/// </summary>
public sealed class Interactable : Component
{
    /// <summary>Texto que se muestra al jugador cuando está en rango.</summary>
    public string Prompt { get; set; } = "E para interactuar";

    /// <summary>Distancia máxima al jugador (en unidades de mundo).</summary>
    public float Radius { get; set; } = 1.8f;

    /// <summary>Identificador del diálogo en `dialogs/*.json` (opcional).</summary>
    public string? DialogueId { get; set; }

    /// <summary>Si es verdadero, solo se puede usar una vez.</summary>
    public bool Once { get; set; } = true;

    /// <summary>Marca de uso en la sesión de juego (no se guarda).</summary>
    [JsonIgnore]
    public bool Used { get; set; }

    public override Component CloneFor(Entity newOwner) => new Interactable
    {
        Enabled = Enabled,
        Prompt = Prompt,
        Radius = Radius,
        DialogueId = DialogueId,
        Once = Once,
        Owner = newOwner,
    };
}
