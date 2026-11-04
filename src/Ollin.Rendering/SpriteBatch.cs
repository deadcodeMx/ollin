using System.Numerics;
using Silk.NET.OpenGLES;
using Ollin.Core.Model.Components;

namespace Ollin.Rendering;

/// <summary>
/// Lote de sprites: acumula vértices en un buffer dinámico y dibuja todos los
/// cuadros de la misma textura con una sola llamada a GPU.
/// </summary>
public sealed class SpriteBatch : IDisposable
{
    private const int MaxQuads = 8192;
    private const int FloatsPerVertex = 8; // posición(2) + uv(2) + color(4)
    private const int VerticesPerQuad = 4;
    private const int IndicesPerQuad = 6;

    private readonly GL _gl;
    private readonly float[] _vertices = new float[MaxQuads * VerticesPerQuad * FloatsPerVertex];
    private readonly ushort[] _indices = new ushort[MaxQuads * IndicesPerQuad];

    private uint _vao;
    private uint _vbo;
    private uint _ebo;
    private int _quadCount;
    private int _vertexOffset;
    private Texture2D? _currentTexture;
    private uint _program;
    private int _uMvp;
    private int _uTexture;
    private Texture2D? _whiteTexture;
    private BlendMode2D _blendMode = BlendMode2D.Normal;

    public SpriteBatch(GL gl)
    {
        _gl = gl;
    }

    public int LastDrawCalls { get; private set; }

    public int LastQuadCount { get; private set; }

    public unsafe void Initialize(uint program, int uniformMvp, int uniformTexture, Texture2D whiteTexture)
    {
        _program = program;
        _uMvp = uniformMvp;
        _uTexture = uniformTexture;
        _whiteTexture = whiteTexture;

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
        _gl.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, stride, (void*)0);

        _gl.EnableVertexAttribArray(1);
        _gl.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, stride, (void*)(2 * sizeof(float)));

        _gl.EnableVertexAttribArray(2);
        _gl.VertexAttribPointer(2, 4, VertexAttribPointerType.Float, false, stride, (void*)(4 * sizeof(float)));

        _gl.BindVertexArray(0);
    }

    public unsafe void Begin(Matrix4x4 viewProjection)
    {
        _quadCount = 0;
        _vertexOffset = 0;
        LastDrawCalls = 0;
        LastQuadCount = 0;
        _currentTexture = null;

        _gl.UseProgram(_program);
        _gl.Uniform1(_uTexture, 0);
        _gl.ActiveTexture(TextureUnit.Texture0);

        _gl.UniformMatrix4(_uMvp, 1, false, &viewProjection.M11);

        _gl.BindVertexArray(_vao);
    }

    public void SetBlendMode(BlendMode2D mode)
    {
        if (_blendMode == mode) return;
        Flush();
        _blendMode = mode;
        switch (mode)
        {
            case BlendMode2D.Add:
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
                break;
            case BlendMode2D.Multiply:
                _gl.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
                break;
            default:
                _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
                break;
        }
    }

    /// <summary>Agrega un cuadro con esquinas en mundo (TL, TR, BR, BL).</summary>
    public void DrawQuad(Texture2D? texture, Vector2 topLeft, Vector2 topRight, Vector2 bottomRight, Vector2 bottomLeft,
        Vector2 uvTopLeft, Vector2 uvTopRight, Vector2 uvBottomRight, Vector2 uvBottomLeft, Vector4 color)
    {
        var effectiveTexture = texture ?? _whiteTexture;
        if (effectiveTexture is null)
        {
            return;
        }

        if (_quadCount >= MaxQuads || (_quadCount > 0 && !ReferenceEquals(_currentTexture, effectiveTexture)))
        {
            Flush();
        }

        _currentTexture = effectiveTexture;

        AddVertex(topLeft, uvTopLeft, color);
        AddVertex(topRight, uvTopRight, color);
        AddVertex(bottomRight, uvBottomRight, color);
        AddVertex(bottomLeft, uvBottomLeft, color);

        _quadCount++;
    }

    /// <summary>Dibuja un triángulo usando un quad degenerado (tercer vértice duplicado).</summary>
    public void DrawTriangle(Texture2D? texture, Vector2 a, Vector2 b, Vector2 c, Vector4 color)
    {
        var effectiveTexture = texture ?? _whiteTexture;
        if (effectiveTexture is null) return;
        if (_quadCount >= MaxQuads || (_quadCount > 0 && !ReferenceEquals(_currentTexture, effectiveTexture))) Flush();
        _currentTexture = effectiveTexture;
        AddVertex(a, new Vector2(0f, 0f), color);
        AddVertex(b, new Vector2(0f, 0f), color);
        AddVertex(c, new Vector2(0f, 0f), color);
        AddVertex(c, new Vector2(0f, 0f), color);
        _quadCount++;
    }

    public void DrawRect(Texture2D? texture, Vector2 center, Vector2 size, Vector4 color, float rotation = 0f)
    {
        var half = size * 0.5f;
        var corners = new[]
        {
            new Vector2(-half.X, half.Y),
            new Vector2(half.X, half.Y),
            new Vector2(half.X, -half.Y),
            new Vector2(-half.X, -half.Y),
        };

        if (MathF.Abs(rotation) > 0.0001f)
        {
            var cos = MathF.Cos(rotation);
            var sin = MathF.Sin(rotation);
            for (var i = 0; i < corners.Length; i++)
            {
                var p = corners[i];
                corners[i] = new Vector2(p.X * cos - p.Y * sin, p.X * sin + p.Y * cos);
            }
        }

        for (var i = 0; i < corners.Length; i++)
        {
            corners[i] += center;
        }

        DrawQuad(texture, corners[0], corners[1], corners[2], corners[3],
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1), color);
    }

    /// <summary>Dibuja una línea como un cuadro delgado.</summary>
    public void DrawLine(Texture2D? texture, Vector2 from, Vector2 to, float thickness, Vector4 color)
    {
        var delta = to - from;
        var length = delta.Length();
        if (length < 0.00001f)
        {
            return;
        }

        var direction = delta / length;
        var normal = new Vector2(-direction.Y, direction.X) * (thickness * 0.5f);

        DrawQuad(texture,
            from + normal,
            to + normal,
            to - normal,
            from - normal,
            new Vector2(0, 0), new Vector2(1, 0), new Vector2(1, 1), new Vector2(0, 1),
            color);
    }

    private void AddVertex(Vector2 position, Vector2 uv, Vector4 color)
    {
        var index = _vertexOffset * FloatsPerVertex;
        _vertices[index] = position.X;
        _vertices[index + 1] = position.Y;
        _vertices[index + 2] = uv.X;
        _vertices[index + 3] = uv.Y;
        _vertices[index + 4] = color.X;
        _vertices[index + 5] = color.Y;
        _vertices[index + 6] = color.Z;
        _vertices[index + 7] = color.W;
        _vertexOffset++;
    }

    public unsafe void Flush()
    {
        if (_quadCount == 0)
        {
            return;
        }

        var vertexCount = _quadCount * VerticesPerQuad;

        _gl.BindBuffer(BufferTargetARB.ArrayBuffer, _vbo);
        _gl.BufferSubData<float>(BufferTargetARB.ArrayBuffer, 0, _vertices.AsSpan(0, vertexCount * FloatsPerVertex));

        _gl.BindTexture(TextureTarget.Texture2D, _currentTexture?.Handle ?? 0);
        _gl.DrawElements(PrimitiveType.Triangles, (uint)(_quadCount * IndicesPerQuad), DrawElementsType.UnsignedShort, (void*)0);

        LastDrawCalls++;
        LastQuadCount += _quadCount;
        _quadCount = 0;
        _vertexOffset = 0;
        _currentTexture = null;
    }

    public void End()
    {
        Flush();
        _gl.BindVertexArray(0);
        _gl.BindTexture(TextureTarget.Texture2D, 0);
    }

    public void Dispose()
    {
        if (_vbo != 0)
        {
            _gl.DeleteBuffer(_vbo);
        }

        if (_ebo != 0)
        {
            _gl.DeleteBuffer(_ebo);
        }

        if (_vao != 0)
        {
            _gl.DeleteVertexArray(_vao);
        }
    }
}
