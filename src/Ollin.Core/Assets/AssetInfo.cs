namespace Ollin.Core.Assets;

/// <summary>Tipo de recurso que el editor puede mostrar.</summary>
public enum AssetKind
{
    Texture,
    Scene,
    Other,
}

/// <summary>Un archivo del proyecto con su ruta relativa a la raíz.</summary>
public sealed record AssetInfo(string RelativePath, string FullPath, long SizeBytes, AssetKind Kind)
{
    public string Name => Path.GetFileName(RelativePath);

    public string Folder => Path.GetDirectoryName(RelativePath)?.Replace('\\', '/') ?? string.Empty;

    public string SizeText => SizeBytes < 1024
        ? $"{SizeBytes} B"
        : SizeBytes < 1024 * 1024
            ? $"{SizeBytes / 1024.0:0.#} KB"
            : $"{SizeBytes / (1024.0 * 1024.0):0.#} MB";
}
