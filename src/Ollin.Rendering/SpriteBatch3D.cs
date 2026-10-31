using System.Numerics;
using Silk.NET.OpenGLES;

namespace Ollin.Rendering;

/// <summary>
/// Lote de geometría 3D con posiciones de mundo, UV, color y búfer de profundidad.
/// Se usa para el puente 2.5D (paredes, suelos, techos y billboards).
/// </summary>
public sealed class SpriteBatch3D : IDisposable
{
    private const int MaxQuads = 4096;
    private const int FloatsPerVertex = 9; // posición(3) + uv(2) + color(4)
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;

    private const string VertexShaderSource = """
        #version 300 es
        layout(location = 0) in vec3 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        layout(location = 2) in vec4 aColor;

        uniform mat4 uViewProjection;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            gl_Position = uViewProjection * vec4(aPosition, 1.0);
            vTexCoord = aTexCoord;
            vColor = aColor;
        }
        """;

    private const string FragmentShaderSource = """
        #version 300 es
        precision mediump float;
        in vec2 vTexCoord;
        in vec4 vColor;
        uniform sampler2D uTexture;
        out vec4 fragColor;

        void main()
        {
            fragColor = texture(uTexture, vTexCoord) * vColor;
        }
        """;

    private readonly GL _gl;
    private readonly float[] _vertices = new float[MaxQuads * VerticesPerQuad * FloatsPerVertex];
    private readonly ushort[] _indices = new ushort[MaxQuads * IndicesPerQuad];

    private uint _program;
    private int _uViewProjection;
    private int _uTexture;
    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private Texture2D? _whiteTexture;
    private Texture2D? _currentTexture;
    private int _quadCount;
    private int _vertexOffset;

    public SpriteBatch3D(GL gl)
    {
        _gl = gl;
    }

    public int LastDrawCalls { get; private set; }

    public int LastQuadCount { get; private set; }

    public unsafe void Initialize(Texture2D whiteTexture)
    {
        _whiteTexture = whiteTexture;
        _program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _uViewProjection = _gl.GetUniformLocation(_program, "uViewProjection");
        _uTexture = _gl.GetUniformLocation(_program, "uTexture");

        _vao = _gl.GenVertexArray();
        _gl.BindVertexArray(_vao);

        _vbo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferData(BufferTargetARB.ArrayBuffer, (nuint)(_vertices.Length * sizeof(float)), null, BufferUsageARB.DynamicDraw);

        _ebo = _gl.GenBuffer();
        _gl.BindBuffer(BufferTargetARB.ElementArrayBuffer, _ebo);

        for (var i = 0; i < MaxQuads; i++)
        {
            var v = (ushort)(i * 4);
            var o = i * IndicesPerQuad;
            _indices[o] = v;
            _indices[o + 1] = (ushort)(v + 1);
            _indices[o + 2] = (ushort)(v + 2);
            _indices[o + 3] = v;
            _indices[o + 4] = (ushort)(v + 2);
            _indices[o + 5] = (ushort)(v + 3);
        }

        _gl.BufferData<ushort>(BufferTargetARB.ElementArrayBuffer, _indices.AsSpan(), BufferUsageARB.StaticDraw);

        var stride = (uint)(FloatsPerVertex * sizeof(float));

        _gl.EnableVertexAttribArray(0);
        _gl.VertexAttribPointer(0, 3, VertexAttribPointerType.Float, false, stride, (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(3 * sizeof(float)));

        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(5 * sizeof(float)));

        _gl.BindVertexArray(0);
    }

    public void Begin(Matrix4x4 viewProjection)
    {
        _quadCount = 0;
        _vertexOffset = 0;
        _currentTexture = null;
        LastDrawCalls = 0;
        LastQuadCount = 0;

        _gl.UseProgram(_program);
        _gl.UniformMatrix4(_uViewProjection, 1, false, ToFloatArray(viewProjection));
        _gl.Uniform1(_uTexture, 0);
    }

    public unsafe void DrawQuad(
        Texture2D? texture,
        Vector3 topLeft, Vector3 topRight, Vector3 bottomRight, Vector3 bottomLeft,
        Vector2 uvTopLeft, Vector2 uvTopRight, Vector2 uvBottomRight, Vector2 uvBottomLeft,
        Vector4 color)
    {
        var effective = texture ?? _whiteTexture;

        if (effective is null)
        {
            return;
        }

        if (_quadCount >= MaxQuads || (_quadCount > 0 && !ReferenceEquals(_currentTexture, effective)))
        {
            Flush();
        }

        _currentTexture = effective;

        AddVertex(topLeft, uvTopLeft, color);
        AddVertex(topRight, uvTopRight, color);
        AddVertex(bottomRight, uvBottomRight, color);
        AddVertex(bottomLeft, uvBottomLeft, color);
        _quadCount++;
    }

    private void AddVertex(Vector3 position, Vector2 uv, Vector4 color)
    {
        var o = _vertexOffset;
        _vertices[o] = position.X;
        _vertices[o + 1] = position.Y;
        _vertices[o + 2] = position.Z;
        _vertices[o + 3] = uv.X;
        _vertices[o + 4] = uv.Y;
        _vertices[o + 5] = color.X;
        _vertices[o + 6] = color.Y;
        _vertices[o + 7] = color.Z;
        _vertices[o + 8] = color.W;
        _vertexOffset += FloatsPerVertex;
    }

    public unsafe void Flush()
    {
        if (_quadCount == 0)
        {
            return;
        }

        _gl.BindVertexArray(_vao);
        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferSubData<float>(BufferTargetARB.ArrayBuffer, 0, _vertices.AsSpan(0, _vertexOffset));

        var texture = _currentTexture ?? _whiteTexture;
        if (texture is not null)
        {
            _gl.ActiveTexture(TextureUnit.Texture0);
            _gl.BindTexture(TextureTarget.Texture2D, texture.Handle);
        }

        _gl.DrawElements(PrimitiveType.Triangles, (uint)(_quadCount * IndicesPerQuad), DrawElementsType.UnsignedShort, (void*)0);

        LastDrawCalls++;
        LastQuadCount += _quadCount;
        _quadCount = 0;
        _vertexOffset = 0;
    }

    public void End() => Flush();

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);
        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var status);

        if (status == 0)
        {
            throw new InvalidOperationException("No se pudo enlazar el shader 3D: " + _gl.GetProgramInfoLog(program));
        }

        _gl.DetachShader(program, vertex);
        _gl.DetachShader(program, fragment);
        _gl.DeleteShader(vertex);
        _gl.DeleteShader(fragment);
        return program;
    }

    private uint CompileShader(ShaderType type, string source)
    {
        var shader = _gl.CreateShader(type);
        _gl.ShaderSource(shader, source);
        _gl.CompileShader(shader);
        _gl.GetShader(shader, ShaderParameterName.CompileStatus, out var status);

        if (status == 0)
        {
            throw new InvalidOperationException($"No se pudo compilar el shader 3D ({type}): {_gl.GetShaderInfoLog(shader)}");
        }

        return shader;
    }

    private static float[] ToFloatArray(Matrix4x4 matrix) => new[]
    {
        matrix.M11, matrix.M12, matrix.M13, matrix.M14,
        matrix.M21, matrix.M22, matrix.M23, matrix.M24,
        matrix.M31, matrix.M32, matrix.M33, matrix.M34,
        matrix.M41, matrix.M42, matrix.M43, matrix.M44,
    };

    public void Dispose()
    {
        _gl.DeleteProgram(_program);
        _gl.DeleteVertexArray(_vao);
        _gl.DeleteBuffer(_vbo);
        _gl.DeleteBuffer(_ebo);
    }
}
