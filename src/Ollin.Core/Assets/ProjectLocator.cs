namespace Ollin.Core.Assets;

/// <summary>
/// Localiza la carpeta del proyecto subiendo desde el ejecutable. Permite que el
/// editor encuentre <c>assets/</c> aunque se lance desde <c>bin/</c>.
/// </summary>
public static class ProjectLocator
{
    public static string? FindProjectRoot(string? startDirectory = null, int maxDepth = 8)
    {
        var directory = new DirectoryInfo(startDirectory ?? AppContext.BaseDirectory);

        for (var depth = 0; depth < maxDepth && directory is not null; depth++)
        {
            if (Directory.Exists(Path.Combine(directory.FullName, AssetDatabase.AssetsFolderName)) ||
                File.Exists(Path.Combine(directory.FullName, "Ollin.sln")))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
