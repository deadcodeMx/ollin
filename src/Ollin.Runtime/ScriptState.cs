using System.Reflection;

namespace Ollin.Runtime;

/// <summary>
/// Copia el estado de un script entre recargas en caliente. Solo conserva valores
/// cuyo tipo viva fuera del ensamblado recargable (números, cadenas, vectores,
/// enumeraciones…); las referencias a tipos definidos por el propio script se
/// descartan porque cambian de identidad en cada compilación.
/// </summary>
internal static class ScriptState
{
    private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

    public static Dictionary<string, object?> Capture(object instance)
    {
        var state = new Dictionary<string, object?>(StringComparer.Ordinal);

        foreach (var field in instance.GetType().GetFields(Fields))
        {
            if (field.IsStatic)
            {
                continue;
            }

            object? value;
            try
            {
                value = field.GetValue(instance);
            }
            catch
            {
                continue;
            }

            if (value is not null && value.GetType().Assembly.IsCollectible)
            {
                // Tipo definido por el script: no sobrevive a la recarga.
                continue;
            }

            state[field.Name] = value;
        }

        return state;
    }

    public static int Restore(object instance, Dictionary<string, object?> state)
    {
        var restored = 0;

        foreach (var field in instance.GetType().GetFields(Fields))
        {
            if (field.IsStatic || !state.TryGetValue(field.Name, out var value) || value is null)
            {
                continue;
            }

            if (!field.FieldType.IsInstanceOfType(value))
            {
                continue;
            }

            try
            {
                field.SetValue(instance, value);
                restored++;
            }
            catch
            {
                // Campo de solo lectura o valor incompatible: se ignora.
            }
        }

        return restored;
    }
}
