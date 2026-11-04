using Silk.NET.OpenGLES;
using Ollin.Core.Diagnostics;

namespace Ollin.Rendering;

/// <summary>Framebuffer con textura de color y depth renderbuffer.</summary>
public sealed class RenderTarget2D : IDisposable
{
    private readonly GL _gl;
    private readonly uint _fbo;
    private readonly uint _depth;
    private readonly int _width;
    private readonly int _height;
    private int _previousFramebuffer;

    public unsafe RenderTarget2D(GL gl, int width, int height)
    {
        _gl = gl; _width = width; _height = height;

        var texture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture2D, texture);
        gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba, (uint)width, (uint)height, 0,
            PixelFormat.Rgba, PixelType.UnsignedByte, (void*)0);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Linear);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int)GLEnum.ClampToEdge);
        gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int)GLEnum.ClampToEdge);

        _fbo = gl.GenFramebuffer();
        gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0,
            TextureTarget.Texture2D, texture, 0);

        _depth = gl.GenRenderbuffer();
        gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, _depth);
        gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent16, (uint)width, (uint)height);
        gl.FramebufferRenderbuffer(FramebufferTarget.Framebuffer, FramebufferAttachment.DepthAttachment,
            RenderbufferTarget.Renderbuffer, _depth);

        if (gl.CheckFramebufferStatus(FramebufferTarget.Framebuffer) != GLEnum.FramebufferComplete)
            throw new InvalidOperationException("Framebuffer incompleto");

        gl.BindFramebuffer(FramebufferTarget.Framebuffer, 0);
        gl.BindTexture(TextureTarget.Texture2D, 0);
        Texture = new Texture2D(gl, texture, width, height, isRenderTarget: true);
    }

    public Texture2D Texture { get; }

    public void Bind()
    {
        _previousFramebuffer = _gl.GetInteger((GetPName)0x8CA6);
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, _fbo);
        _gl.Viewport(0, 0, (uint)_width, (uint)_height);
        _gl.Disable(EnableCap.ScissorTest);
    }

    public void Unbind()
    {
        _gl.BindFramebuffer(FramebufferTarget.Framebuffer, (uint)_previousFramebuffer);
    }

    public void Dispose()
    {
        _gl.DeleteFramebuffer(_fbo);
        _gl.DeleteRenderbuffer(_depth);
        Texture.Dispose();
    }
}
