using Silk.NET.OpenGLES;

namespace Ollin.Rendering;

/// <summary>Textura 2D en GPU con su tamaño original en píxeles.</summary>
public sealed class Texture2D : IDisposable
{
    private readonly GL _gl;

    public Texture2D(GL gl, uint handle, int width, int height, string? sourcePath = null, bool isRenderTarget = false)
    {
        _gl = gl;
        Handle = handle;
        Width = width;
        Height = height;
        SourcePath = sourcePath;
        IsRenderTarget = isRenderTarget;
    }

    public uint Handle { get; }

    public int Width { get; }

    public int Height { get; }

    public string? SourcePath { get; }

    public bool IsRenderTarget { get; }

    public float AspectRatio => Height == 0 ? 1f : Width / (float)Height;

    /// <summary>Carga un PNG/JPG desde disco con origen arriba-izquierda.</summary>
    public static Texture2D FromFile(GL gl, string path)
    {
        var bytes = File.ReadAllBytes(path);
        var image = StbImageSharp.ImageResult.FromMemory(bytes, StbImageSharp.ColorComponents.RedGreenBlueAlpha)
            ?? throw new InvalidDataException($"No se pudo decodificar la imagen: {path}");

        return FromPixels(gl, image.Data, image.Width, image.Height, path);
    }

    public static Texture2D FromPixels(GL gl, byte[] rgba, int width, int height, string? sourcePath = null, bool isRenderTarget = false)
    {
        var handle = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, handle);

        unsafe
        {
            fixed (byte* pixels = rgba)
            {
                gl.TexImage2D(
                    TextureTarget.Texture2D,
                    0,
                    InternalFormat.Rgba,
                    (uint)width,
                    (uint)height,
                    0,
                    PixelFormat.Rgba,
                    PixelType.UnsignedByte,
                    pixels);
            }
        }

        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.Repeat);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.Repeat);
        gl.BindTexture(TextureTarget.Texture2D, 0);

        return new Texture2D(gl, handle, width, height, sourcePath, isRenderTarget);
    }

    /// <summary>Genera una textura de tablero de ajedrez (marcador de "falta textura").</summary>
    public static Texture2D CreateChecker(GL gl, int size = 64, int cell = 8)
    {
        var pixels = new byte[size * size * 4];
        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dark = ((x / cell) + (y / cell)) % 2 == 0;
                var i = (y * size + x) * 4;
                var value = dark ? (byte)120 : (byte)220;
                pixels[i] = value;
                pixels[i + 1] = (byte)(dark ? 40 : 90);
                pixels[i + 2] = value;
                pixels[i + 3] = 255;
            }
        }

        return FromPixels(gl, pixels, size, size);
    }

    /// <summary>Genera una textura blanca de 1x1 para dibujar formas sólidas.</summary>
    public static Texture2D CreateWhite(GL gl)
    {
        var pixels = new byte[] { 255, 255, 255, 255 };
        return FromPixels(gl, pixels, 1, 1);
    }

    public void Dispose() => _gl.DeleteTexture(Handle);
}
