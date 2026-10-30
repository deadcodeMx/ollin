namespace Ollin.Core.Assets;

/// <summary>
/// Indexa los recursos de un proyecto. Por convención viven en la carpeta
/// <c>assets/</c> junto al archivo de escena, con rutas relativas guardadas en
/// la escena para que el proyecto se pueda mover de carpeta o de equipo.
/// </summary>
public static class AssetDatabase
{
    public const string AssetsFolderName = "assets";

    public static readonly string[] TextureExtensions = { ".png", ".jpg", ".jpeg", ".bmp" };

    public static string GetAssetsFolder(string projectRoot) => Path.Combine(projectRoot, AssetsFolderName);

    public static bool IsTexture(string path) =>
        TextureExtensions.Contains(Path.GetExtension(path), StringComparer.OrdinalIgnoreCase);

    /// <summary>Lista las texturas del proyecto ordenadas por ruta.</summary>
    public static IReadOnlyList<AssetInfo> ScanTextures(string projectRoot)
    {
        var folder = GetAssetsFolder(projectRoot);
        if (!Directory.Exists(folder))
        {
            return Array.Empty<AssetInfo>();
        }

        var root = Path.GetFullPath(projectRoot);
        var results = new List<AssetInfo>();

        foreach (var file in Directory.EnumerateFiles(folder, "*", SearchOption.AllDirectories))
        {
            if (!IsTexture(file))
            {
                continue;
            }

            var relative = Path.GetRelativePath(root, file).Replace('\\', '/');
            results.Add(new AssetInfo(relative, file, new FileInfo(file).Length, AssetKind.Texture));
        }

        results.Sort((a, b) => string.Compare(a.RelativePath, b.RelativePath, StringComparison.OrdinalIgnoreCase));
        return results;
    }

    /// <summary>Copia una textura dentro de <c>assets/</c> y devuelve su ruta relativa.</summary>
    public static string ImportTexture(string projectRoot, string sourcePath, string? subfolder = null)
    {
        if (!IsTexture(sourcePath))
        {
            throw new InvalidOperationException($"El archivo no es una imagen compatible: {Path.GetFileName(sourcePath)}");
        }

        var destinationFolder = GetAssetsFolder(projectRoot);
        if (!string.IsNullOrWhiteSpace(subfolder))
        {
            destinationFolder = Path.Combine(destinationFolder, subfolder);
        }

        Directory.CreateDirectory(destinationFolder);

        var fileName = Path.GetFileName(sourcePath);
        var destination = Path.Combine(destinationFolder, fileName);
        var counter = 1;
        while (File.Exists(destination))
        {
            var name = Path.GetFileNameWithoutExtension(fileName);
            var extension = Path.GetExtension(fileName);
            destination = Path.Combine(destinationFolder, $"{name}-{counter++}{extension}");
        }

        File.Copy(sourcePath, destination);

        var relative = Path.GetRelativePath(Path.GetFullPath(projectRoot), destination).Replace('\\', '/');
        return relative;
    }
}
