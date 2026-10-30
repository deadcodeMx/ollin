using System.Reflection;

namespace Ollin.Setup;

/// <summary>Versión del instalador leída del ensamblado (nunca se queda obsoleta).</summary>
internal static class SetupInfo
{
    public static string Version { get; } = ReadVersion();

    public static string DisplayName => $"Ollin {Version} beta";

    private static string ReadVersion()
    {
        var assembly = typeof(SetupInfo).Assembly;
        var informational = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        return informational.Split('+')[0];
    }
}
