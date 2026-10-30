namespace Ollin.Core.Model.Components;

/// <summary>
/// Enlaza la entidad con un script en C#. La compilación y recarga en caliente
/// llegan en la etapa 3; por ahora solo guarda la referencia.
/// </summary>
public sealed class ScriptComponent : Component
{
    /// <summary>Ruta relativa al archivo de script, por ejemplo "scripts/Player.cs".</summary>
    public string? ScriptPath { get; set; }

    public string ClassName { get; set; } = string.Empty;

    public override Component CloneFor(Entity newOwner) => new ScriptComponent
    {
        Enabled = Enabled,
        ScriptPath = ScriptPath,
        ClassName = ClassName,
        Owner = newOwner,
    };
}
