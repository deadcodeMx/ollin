using System.Reflection;
using Xunit;

// Ejecutor de pruebas en proceso. Permite validar el proyecto en entornos donde
// el host de VSTest no puede arrancar (por ejemplo, sesiones con sandbox).
// `dotnet test` sigue siendo el camino normal en una terminal de desarrollo.

var assembly = Assembly.GetExecutingAssembly();
var results = new List<(string Name, bool Passed, string? Error)>();

foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract).OrderBy(t => t.Name))
{
    foreach (var method in type.GetMethods().Where(m => m.GetCustomAttribute<FactAttribute>() is not null).OrderBy(m => m.Name))
    {
        object? instance = null;
        try
        {
            instance = Activator.CreateInstance(type);
            method.Invoke(instance, null);
            results.Add(($"{type.Name}.{method.Name}", true, null));
        }
        catch (TargetInvocationException exception)
        {
            results.Add(($"{type.Name}.{method.Name}", false, exception.InnerException?.Message ?? exception.Message));
        }
        catch (Exception exception)
        {
            results.Add(($"{type.Name}.{method.Name}", false, exception.Message));
        }
        finally
        {
            (instance as IDisposable)?.Dispose();
        }
    }
}

foreach (var result in results)
{
    Console.WriteLine($"{(result.Passed ? "PASA " : "FALLA")}  {result.Name}");
    if (!result.Passed)
    {
        Console.WriteLine($"       {result.Error}");
    }
}

var passed = results.Count(r => r.Passed);
Console.WriteLine();
Console.WriteLine($"{passed}/{results.Count} pruebas correctas");
return passed == results.Count && results.Count > 0 ? 0 : 1;
