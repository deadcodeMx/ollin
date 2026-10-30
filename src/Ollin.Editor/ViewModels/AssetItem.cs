using Avalonia.Media;
using Avalonia.Media.Imaging;
using Ollin.Core.Assets;

namespace Ollin.Editor.ViewModels;

/// <summary>Textura del proyecto lista para mostrarse o asignarse a un sprite.</summary>
public sealed class AssetItem
{
    public AssetItem(AssetInfo info)
    {
        Info = info;
        Thumbnail = LoadThumbnail(info.FullPath);
    }

    public AssetInfo Info { get; }

    public string Name => Info.Name;

    public string RelativePath => Info.RelativePath;

    public string SizeText => Info.SizeText;

    public IImage? Thumbnail { get; }

    /// <summary>Relación de aspecto de la imagen (1 si no se pudo leer).</summary>
    public float AspectRatio
    {
        get
        {
            if (Thumbnail is { } image && image.Size.Height > 0.5)
            {
                return (float)(image.Size.Width / image.Size.Height);
            }

            return 1f;
        }
    }

    private static IImage? LoadThumbnail(string path)
    {
        try
        {
            using var stream = File.OpenRead(path);
            return new Bitmap(stream);
        }
        catch
        {
            return null;
        }
    }

    public override string ToString() => RelativePath;
}
