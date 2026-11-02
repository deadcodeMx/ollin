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

    /// <summary>Valores de campos públicos serializados desde el inspector.</summary>
    public Dictionary<string, string> Values { get; set; } = new();

    public override Component CloneFor(Entity newOwner) => new ScriptComponent
    {
        Enabled = Enabled,
        ScriptPath = ScriptPath,
        ClassName = ClassName,
        Values = new Dictionary<string, string>(Values),
        Owner = newOwner,
    };
}
