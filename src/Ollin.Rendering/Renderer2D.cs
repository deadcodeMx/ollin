using System.Numerics;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Silk.NET.OpenGLES;

namespace Ollin.Rendering;

/// <summary>
/// Renderer 2D del editor: fondo, cuadrícula, sprites ordenados y resaltado de la
/// selección. Todo el estado de GPU vive aquí; el control de UI solo le pasa el
/// tamaño del viewport y la cámara.
/// </summary>
public sealed class Renderer2D : IDisposable
{
    private const string VertexShaderSource = """
        #version 300 es
        layout(location = 0) in vec2 aPosition;
        layout(location = 1) in vec2 aTexCoord;
        layout(location = 2) in vec4 aColor;

        uniform mat4 uMvp;

        out vec2 vTexCoord;
        out vec4 vColor;

        void main()
        {
            gl_Position = uMvp * vec4(aPosition, 0.0, 1.0);
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

    private static readonly Vector4 GridMinorColor = new(1f, 1f, 1f, 0.055f);
    private static readonly Vector4 GridMajorColor = new(1f, 1f, 1f, 0.12f);
    private static readonly Vector4 AxisXColor = new(0.85f, 0.35f, 0.32f, 0.55f);
    private static readonly Vector4 AxisYColor = new(0.35f, 0.78f, 0.55f, 0.55f);
    private static readonly Vector4 SelectionColor = new(0.91f, 0.64f, 0.23f, 1f);
    private static readonly Vector4 HandleBorderColor = new(0.12f, 0.09f, 0.05f, 0.95f);

    private readonly GL _gl;
    private readonly SpriteBatch _batch;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingTextures = new(StringComparer.OrdinalIgnoreCase);

    private uint _program;
    private int _uniformMvp;
    private int _uniformTexture;
    private Texture2D? _whiteTexture;
    private Texture2D? _checkerTexture;

    public Renderer2D(GL gl)
    {
        _gl = gl;
        _batch = new SpriteBatch(gl);
    }

    /// <summary>Raíz del proyecto para resolver rutas de texturas relativas.</summary>
    public string ProjectRoot { get; set; } = Environment.CurrentDirectory;

    /// <summary>Último error de compilación de shaders, si hubo.</summary>
    public string? LastShaderError { get; private set; }

    public int LastDrawCalls => _batch.LastDrawCalls;

    public int LastQuadCount => _batch.LastQuadCount;

    public void Initialize()
    {
        _program = CreateProgram(VertexShaderSource, FragmentShaderSource);
        _uniformMvp = _gl.GetUniformLocation(_program, "uMvp");
        _uniformTexture = _gl.GetUniformLocation(_program, "uTexture");

        _whiteTexture = Texture2D.CreateWhite(_gl);
        _checkerTexture = Texture2D.CreateChecker(_gl);
        _batch.Initialize(_program, _uniformMvp, _uniformTexture, _whiteTexture);

        _gl.Disable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
    }

    /// <summary>Dibuja un frame completo de la escena.</summary>
    public void RenderFrame(Scene scene, Vector2 cameraPosition, Camera2D camera, int width, int height,
        Guid? selectedEntityId, bool showGrid, bool showHelpers = true)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _gl.Viewport(0, 0, (uint)width, (uint)height);
        _gl.ClearColor(camera.Background.X, camera.Background.Y, camera.Background.Z, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        var viewProjection = CameraMath.GetViewProjection(cameraPosition, camera.OrthographicSize, camera.Zoom, width, height);
        _batch.Begin(viewProjection);

        if (showGrid)
        {
            DrawGrid(camera, cameraPosition, width, height);
        }

        DrawSprites(scene);

        if (selectedEntityId is { } id && showHelpers)
        {
            DrawSelection(scene, id, camera, height);
        }

        _batch.End();
    }

    private void DrawSprites(Scene scene)
    {
        var drawables = new List<(Entity Entity, SpriteRenderer Sprite)>();
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active)
            {
                continue;
            }

            var sprite = entity.GetComponent<SpriteRenderer>();
            if (sprite is { Enabled: true, Visible: true })
            {
                drawables.Add((entity, sprite));
            }
        }

        drawables.Sort((a, b) =>
        {
            var order = a.Sprite.SortingOrder.CompareTo(b.Sprite.SortingOrder);
            return order != 0 ? order : string.CompareOrdinal(a.Entity.Name, b.Entity.Name);
        });

        foreach (var (entity, sprite) in drawables)
        {
            var world = scene.GetWorldMatrix(entity);
            var texture = ResolveTexture(sprite.TexturePath);
            var corners = GetWorldCorners(world, sprite.Size, sprite.Origin);

            var u0 = sprite.FlipX ? 1f : 0f;
            var u1 = sprite.FlipX ? 0f : 1f;
            var v0 = sprite.FlipY ? 1f : 0f;
            var v1 = sprite.FlipY ? 0f : 1f;

            _batch.DrawQuad(texture,
                corners[0], corners[1], corners[2], corners[3],
                new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1),
                sprite.Tint);
        }
    }

    private void DrawSelection(Scene scene, Guid entityId, Camera2D camera, int viewportHeight)
    {
        var entity = scene.Find(entityId);
        if (entity is null)
        {
            return;
        }

        var sprite = entity.GetComponent<SpriteRenderer>();
        var size = sprite?.Size ?? Vector2.One;
        var origin = sprite?.Origin ?? new Vector2(0.5f, 0.5f);
        var corners = GetWorldCorners(scene.GetWorldMatrix(entity), size, origin);

        // Contorno
        for (var i = 0; i < 4; i++)
        {
            var a = corners[i];
            var b = corners[(i + 1) % 4];
            _batch.DrawLine(null, a, b, 0.035f, SelectionColor);
        }

        // Mangos de escala en las esquinas (tamaño constante en píxeles)
        var ppu = CameraMath.PixelsPerUnit(camera.OrthographicSize, camera.Zoom, viewportHeight);
        var handle = 9f / ppu;
        foreach (var corner in corners)
        {
            _batch.DrawRect(null, corner, new Vector2(handle * 1.35f), HandleBorderColor);
            _batch.DrawRect(null, corner, new Vector2(handle), SelectionColor);
        }

        // Mango de rotación sobre el lado superior, alineado con el sprite
        var topCenter = (corners[0] + corners[1]) * 0.5f;
        var bottomCenter = (corners[3] + corners[2]) * 0.5f;
        var up = topCenter - bottomCenter;
        if (up.LengthSquared() < 1e-6f)
        {
            up = Vector2.UnitY;
        }

        up = Vector2.Normalize(up);
        var rotateHandle = topCenter + up * (26f / ppu);
        _batch.DrawLine(null, topCenter, rotateHandle, 0.02f, SelectionColor);
        _batch.DrawRect(null, rotateHandle, new Vector2(handle * 1.5f), HandleBorderColor, MathF.PI / 4f);
        _batch.DrawRect(null, rotateHandle, new Vector2(handle * 1.05f), SelectionColor, MathF.PI / 4f);
    }

    private void DrawGrid(Camera2D camera, Vector2 cameraPosition, int width, int height)
    {
        var ppu = CameraMath.PixelsPerUnit(camera.OrthographicSize, camera.Zoom, height);
        var visibleHalfHeight = height / 2f / ppu;
        var visibleHalfWidth = width / 2f / ppu;

        var left = cameraPosition.X - visibleHalfWidth;
        var right = cameraPosition.X + visibleHalfWidth;
        var bottom = cameraPosition.Y - visibleHalfHeight;
        var top = cameraPosition.Y + visibleHalfHeight;

        // Une las líneas más finas serán más densas, pero nunca menos de 1 unidad.
        var step = 1f;
        while (step * ppu < 18f)
        {
            step *= 5f;
        }

        var startX = MathF.Floor(left / step) * step;
        for (var x = startX; x <= right; x += step)
        {
            var isAxis = MathF.Abs(x) < step * 0.01f;
            var isMajor = MathF.Abs(x % (step * 5f)) < step * 0.01f;
            var color = isAxis ? AxisYColor : isMajor ? GridMajorColor : GridMinorColor;
            var thickness = isAxis ? 0.02f : isMajor ? 0.012f : 0.008f;
            _batch.DrawLine(null, new Vector2(x, bottom), new Vector2(x, top), thickness, color);
        }

        var startY = MathF.Floor(bottom / step) * step;
        for (var y = startY; y <= top; y += step)
        {
            var isAxis = MathF.Abs(y) < step * 0.01f;
            var isMajor = MathF.Abs(y % (step * 5f)) < step * 0.01f;
            var color = isAxis ? AxisXColor : isMajor ? GridMajorColor : GridMinorColor;
            var thickness = isAxis ? 0.02f : isMajor ? 0.012f : 0.008f;
            _batch.DrawLine(null, new Vector2(left, y), new Vector2(right, y), thickness, color);
        }
    }

    /// <summary>Esquinas del sprite en mundo: TL, TR, BR, BL.</summary>
    public static Vector2[] GetWorldCorners(Matrix4x4 world, Vector2 size, Vector2 origin)
    {
        var left = -size.X * origin.X;
        var right = size.X * (1f - origin.X);
        var bottom = -size.Y * origin.Y;
        var top = size.Y * (1f - origin.Y);

        return new[]
        {
            Vector2.Transform(new Vector2(left, top), world),
            Vector2.Transform(new Vector2(right, top), world),
            Vector2.Transform(new Vector2(right, bottom), world),
            Vector2.Transform(new Vector2(left, bottom), world),
        };
    }

    /// <summary>Prueba si un punto de mundo cae dentro de un sprite (útil para seleccionar).</summary>
    public static bool HitTest(Scene scene, Entity entity, Vector2 worldPoint)
    {
        var sprite = entity.GetComponent<SpriteRenderer>();
        var size = sprite?.Size ?? Vector2.One;
        var origin = sprite?.Origin ?? new Vector2(0.5f, 0.5f);
        var corners = GetWorldCorners(scene.GetWorldMatrix(entity), size, origin);
        return PointInQuad(worldPoint, corners);
    }

    public static bool PointInQuad(Vector2 point, Vector2[] quad)
    {
        var sign = 0;
        for (var i = 0; i < 4; i++)
        {
            var a = quad[i];
            var b = quad[(i + 1) % 4];
            var cross = (b.X - a.X) * (point.Y - a.Y) - (b.Y - a.Y) * (point.X - a.X);
            if (MathF.Abs(cross) < 1e-6f)
            {
                continue;
            }

            var currentSign = cross > 0 ? 1 : -1;
            if (sign == 0)
            {
                sign = currentSign;
            }
            else if (sign != currentSign)
            {
                return false;
            }
        }

        return true;
    }

    private Texture2D? ResolveTexture(string? texturePath)
    {
        if (string.IsNullOrWhiteSpace(texturePath))
        {
            return null;
        }

        if (_textureCache.TryGetValue(texturePath, out var cached))
        {
            return cached;
        }

        if (_missingTextures.Contains(texturePath))
        {
            return _checkerTexture;
        }

        try
        {
            var fullPath = Path.IsPathRooted(texturePath) ? texturePath : Path.Combine(ProjectRoot, texturePath);
            if (!File.Exists(fullPath))
            {
                _missingTextures.Add(texturePath);
                return _checkerTexture;
            }

            var texture = Texture2D.FromFile(_gl, fullPath);
            _textureCache[texturePath] = texture;
            return texture;
        }
        catch (Exception exception)
        {
            Core.Diagnostics.Log.Error($"No se pudo cargar la textura '{texturePath}': {exception.Message}", "Renderer2D");
            _missingTextures.Add(texturePath);
            return _checkerTexture;
        }
    }

    public void ClearTextureCache()
    {
        foreach (var texture in _textureCache.Values)
        {
            texture.Dispose();
        }

        _textureCache.Clear();
        _missingTextures.Clear();
    }

    private uint CreateProgram(string vertexSource, string fragmentSource)
    {
        var vertex = CompileShader(ShaderType.VertexShader, vertexSource);
        var fragment = CompileShader(ShaderType.FragmentShader, fragmentSource);

        var program = _gl.CreateProgram();
        _gl.AttachShader(program, vertex);
        _gl.AttachShader(program, fragment);
        _gl.LinkProgram(program);
        _gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var linked);
        if (linked == 0)
        {
            throw new InvalidOperationException($"No se pudo enlazar el programa de shaders: {_gl.GetProgramInfoLog(program)}");
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
            LastShaderError = _gl.GetShaderInfoLog(shader);
            throw new InvalidOperationException($"Error al compilar shader {type}: {LastShaderError}");
        }

        return shader;
    }

    public void Dispose()
    {
        ClearTextureCache();
        _whiteTexture?.Dispose();
        _checkerTexture?.Dispose();
        _batch.Dispose();

        if (_program != 0)
        {
            _gl.DeleteProgram(_program);
        }
    }
}
