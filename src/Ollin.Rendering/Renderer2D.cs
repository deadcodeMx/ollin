using System.Numerics;
using Ollin.Core.Math;
using Ollin.Core.Model;
using Ollin.Core.Diagnostics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Silk.NET.OpenGLES;

namespace Ollin.Rendering;

public enum GizmoMode
{
    All,
    Move,
    Rotate,
    Scale,
}

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
    private static readonly Vector4 ColliderColor = new(0.3f, 0.85f, 1f, 0.9f);

    private readonly GL _gl;
    private readonly SpriteBatch _batch;
    private readonly Dictionary<string, Texture2D> _textureCache = new(StringComparer.OrdinalIgnoreCase);
    private readonly HashSet<string> _missingTextures = new(StringComparer.OrdinalIgnoreCase);

    private uint _program;
    private int _uniformMvp;
    private int _uniformTexture;
    private Texture2D? _whiteTexture;
    private readonly Ollin.Core.Rendering.RenderServer2D _renderServer = new();
    private readonly Dictionary<string, uint> _materialPrograms = new(StringComparer.Ordinal);
    private Texture2D? _checkerTexture;
    private Texture2D? _radialLight;
    private Texture2D? _vignette;
    private readonly Dictionary<int, Texture2D> _coneLights = new();


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
        CreateLightTextures();

        _gl.Disable(EnableCap.DepthTest);
        _gl.Enable(EnableCap.Blend);
        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _gl.Disable(EnableCap.CullFace);
    }

    /// <summary>Dibuja un frame completo de la escena.</summary>
    public RenderTarget2D CreateRenderTarget(int width, int height) =>
        new(_gl, System.Math.Max(1, width), System.Math.Max(1, height));

    public void DrawScreenTexture(Texture2D texture, Vector4 rect, int width, int height, Vector4 tint)
    {
        _batch.ResetProgram();
        _batch.SetBlendMode(BlendMode2D.Normal);
        _gl.Disable(EnableCap.ScissorTest);
        _gl.Viewport(0, 0, (uint)System.Math.Max(1, width), (uint)System.Math.Max(1, height));
        var projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
        _batch.Begin(projection);
        var left = rect.X * width;
        var top = rect.Y * height;
        var right = (rect.X + rect.Z) * width;
        var bottom = (rect.Y + rect.W) * height;
        var (u0, v0, u1, v1) = texture.IsRenderTarget ? (0f, 1f, 1f, 0f) : (0f, 0f, 1f, 1f);
        _batch.DrawQuad(texture, new Vector2(left, top), new Vector2(right, top), new Vector2(right, bottom), new Vector2(left, bottom),
            new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1), tint);
        _batch.End();
    }
    public void DrawScreenBorder(Vector4 rect, int width, int height, Vector4 color)
    {
        _batch.ResetProgram();
        _batch.SetBlendMode(BlendMode2D.Normal);
        _gl.Disable(EnableCap.ScissorTest);
        _gl.Viewport(0, 0, (uint)System.Math.Max(1, width), (uint)System.Math.Max(1, height));
        var projection = Matrix4x4.CreateOrthographicOffCenter(0f, width, height, 0f, -1f, 1f);
        _batch.Begin(projection);
        var left = rect.X * width;
        var top = rect.Y * height;
        var right = (rect.X + rect.Z) * width;
        var bottom = (rect.Y + rect.W) * height;
        const float thickness = 2f;
        _batch.DrawLine(null, new Vector2(left, top), new Vector2(right, top), thickness, color);
        _batch.DrawLine(null, new Vector2(right, top), new Vector2(right, bottom), thickness, color);
        _batch.DrawLine(null, new Vector2(right, bottom), new Vector2(left, bottom), thickness, color);
        _batch.DrawLine(null, new Vector2(left, bottom), new Vector2(left, top), thickness, color);
        _batch.End();
    }
    public void ResetViewport(int width, int height)
    {
        _gl.Disable(EnableCap.ScissorTest);
        _gl.Viewport(0, 0, (uint)System.Math.Max(1, width), (uint)System.Math.Max(1, height));
    }

    public void RenderFrame(Scene scene, Vector2 cameraPosition, Camera2D camera, int width, int height,
        Guid? selectedEntityId, bool showGrid, bool showHelpers = true, IReadOnlyCollection<Guid>? selectedEntityIds = null, GizmoMode gizmoMode = GizmoMode.All)
    {
        if (width <= 0 || height <= 0)
        {
            return;
        }

        _gl.Viewport(0, 0, (uint)width, (uint)height);
        var viewportX = (int)(camera.Viewport.X * width);
        var viewportY = (int)(camera.Viewport.Y * height);
        var viewportW = System.Math.Max(1, (int)(camera.Viewport.Z * width));
        var viewportH = System.Math.Max(1, (int)(camera.Viewport.W * height));
        _gl.Enable(EnableCap.ScissorTest);
        _gl.Scissor(viewportX, viewportY, (uint)viewportW, (uint)viewportH);
        _gl.Viewport(viewportX, viewportY, (uint)viewportW, (uint)viewportH);
        _gl.ClearColor(camera.Background.X, camera.Background.Y, camera.Background.Z, 1f);
        _gl.Clear(ClearBufferMask.ColorBufferBit);

        if (camera.PixelPerfect)
        {
            var ppu = CameraMath.PixelsPerUnit(camera.OrthographicSize, camera.Zoom, height);
            cameraPosition = new Vector2(MathF.Round(cameraPosition.X * ppu) / ppu, MathF.Round(cameraPosition.Y * ppu) / ppu);
        }
        var viewProjection = CameraMath.GetViewProjection(cameraPosition, camera.OrthographicSize, camera.Zoom, width, height);
        _batch.Begin(viewProjection);

        if (showGrid)
        {
            DrawGrid(camera, cameraPosition, width, height);
        }

        DrawTilemaps(scene);
        var hasYSort = scene.Entities.Any(entity => entity.Active && entity.GetComponents<TileMapLayer>().Any(map => map.YSort && map.Visible));
        if (hasYSort) DrawYSorted(scene, cameraPosition); else DrawSprites(scene, cameraPosition);
        DrawDebugDraw(camera, height);

        if (showHelpers)
        {
            var ids = selectedEntityIds is { Count: > 0 }
                ? selectedEntityIds
                : selectedEntityId is { } selected ? new[] { selected } : Array.Empty<Guid>();

            foreach (var id in ids.Distinct())
            {
                DrawSelection(scene, id, camera, height);
            }
        }
        _batch.End();
        Ollin.Core.Diagnostics.Profiler.DrawCalls = _batch.LastDrawCalls;
        _gl.Disable(EnableCap.ScissorTest);
        _gl.Viewport(0, 0, (uint)width, (uint)height);
        DrawLighting(scene, camera, cameraPosition, width, height, viewProjection);
    }

    private void DrawSkeletons(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Skeleton2D>() is not { Enabled: true } skeleton) continue;
            var points = skeleton.WorldPoints;
            for (var i = 0; i < points.Count; i++)
            {
                var bone = skeleton.Bones[i];
                var start = bone.Parent >= 0 && bone.Parent < points.Count
                    ? points[bone.Parent]
                    : scene.GetWorldPosition(entity);
                _batch.DrawLine(null, start, points[i], 0.035f, new Vector4(0.9f, 0.4f, 1f, 1f));
                _batch.DrawRect(null, points[i], new Vector2(0.08f), new Vector4(1f, 0.8f, 0.3f, 1f));
            }
        }
    }
    private void DrawParticles(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<ParticleEmitter2D>() is not { Enabled: true } emitter) continue;
            var size = new Vector2(System.Math.Max(0.01f, emitter.Size));
            foreach (var particle in emitter.Particles)
            {
                if (particle.Life <= 0f) continue;
                _batch.DrawRect(null, particle.Position, size, particle.Color);
            }
        }
    }

    private void DrawTrails(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Trail2D>() is not { Enabled: true } trail) continue;
            for (var i = 0; i < trail.Points.Count - 1; i++)
            {
                var point = trail.Points[i];
                var next = trail.Points[i + 1];
                var color = trail.Color;
                color.W = System.Math.Clamp(point.Life / System.Math.Max(0.001f, point.MaxLife), 0f, 1f) * trail.Color.W;
                _batch.DrawLine(null, point.Position, next.Position, System.Math.Max(0.01f, trail.Width), color);
            }
        }
    }
    private void DrawMeshes(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            var world = scene.GetWorldMatrix(entity);
            var canvasTint = CanvasTint(entity);
            if (entity.GetComponent<Mesh2D>() is { Enabled: true, Visible: true } mesh)
                DrawMesh(mesh.Mesh, world, Vector2.Zero, 1f, canvasTint);
            if (entity.GetComponent<MultiMesh2D>() is { Enabled: true, Visible: true } multi)
                foreach (var offset in multi.Offsets)
                    DrawMesh(multi.Mesh, world, offset, multi.Scale, canvasTint);
        }
    }

    private void DrawMesh(Mesh2DData mesh, Matrix4x4 world, Vector2 offset, float scale, Vector4 tint)
    {
        for (var i = 0; i + 2 < mesh.Triangles.Count; i += 3)
        {
            var a = Vector2.Transform(mesh.Vertices[mesh.Triangles[i]] * scale + offset, world);
            var b = Vector2.Transform(mesh.Vertices[mesh.Triangles[i + 1]] * scale + offset, world);
            var c = Vector2.Transform(mesh.Vertices[mesh.Triangles[i + 2]] * scale + offset, world);
            _batch.DrawTriangle(null, a, b, c, mesh.Color * tint);
        }
    }
    private void DrawSkinnedMeshes(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            if (entity.GetComponent<SkinnedMesh2D>() is not { Enabled: true, Visible: true } mesh) continue;
            var tint = CanvasTint(entity);
            if (entity.GetComponent<Skeleton2D>() is not { } skeleton) continue;
            if (skeleton.BindPoints.Count < 2 || skeleton.WorldPoints.Count < 2) continue;
            for (var i = 0; i + 2 < mesh.Triangles.Count; i += 3)
            {
                var a = SkinVertex(entity, mesh, skeleton, mesh.Triangles[i]);
                var b = SkinVertex(entity, mesh, skeleton, mesh.Triangles[i + 1]);
                var c = SkinVertex(entity, mesh, skeleton, mesh.Triangles[i + 2]);
                _batch.DrawTriangle(null, a, b, c, mesh.Color * tint);
            }
        }
    }

    private static Vector2 SkinVertex(Entity entity, SkinnedMesh2D mesh, Skeleton2D skeleton, int vertexIndex)
    {
        if (vertexIndex < 0 || vertexIndex >= mesh.Vertices.Count) return Vector2.Zero;
        var vertex = mesh.Vertices[vertexIndex];
        var boneA = vertexIndex < mesh.BoneA.Count ? mesh.BoneA[vertexIndex] : 0;
        var boneB = vertexIndex < mesh.BoneB.Count ? mesh.BoneB[vertexIndex] : boneA;
        var boneC = vertexIndex < mesh.BoneC.Count ? mesh.BoneC[vertexIndex] : boneA;
        var boneD = vertexIndex < mesh.BoneD.Count ? mesh.BoneD[vertexIndex] : boneA;
        var weightB = vertexIndex < mesh.Weight.Count ? System.Math.Clamp(mesh.Weight[vertexIndex], 0f, 1f) : 1f;
        var weightC = vertexIndex < mesh.WeightC.Count ? System.Math.Clamp(mesh.WeightC[vertexIndex], 0f, 1f) : 0f;
        var weightD = vertexIndex < mesh.WeightD.Count ? System.Math.Clamp(mesh.WeightD[vertexIndex], 0f, 1f) : 0f;
        var remaining = System.Math.Max(0f, 1f - weightB - weightC - weightD);
        return SkinBone(skeleton, boneA, vertex) * remaining +
               SkinBone(skeleton, boneB, vertex) * weightB +
               SkinBone(skeleton, boneC, vertex) * weightC +
               SkinBone(skeleton, boneD, vertex) * weightD;
    }

    private static Vector2 SkinBone(Skeleton2D skeleton, int bone, Vector2 vertex)
    {
        if (skeleton.BindPoints.Count < 2) return vertex;
        var index = System.Math.Clamp(bone, 0, System.Math.Min(skeleton.BindPoints.Count, skeleton.WorldPoints.Count) - 1);
        var bindStart = skeleton.BindPoints[System.Math.Max(0, index - 1)];
        var bindEnd = skeleton.BindPoints[index];
        var currentStart = skeleton.WorldPoints[System.Math.Max(0, index - 1)];
        var currentEnd = skeleton.WorldPoints[index];
        var bindDelta = bindEnd - bindStart;
        var currentDelta = currentEnd - currentStart;
        if (bindDelta.LengthSquared() < 0.000001f) return currentStart + (vertex - bindStart);
        var angle = MathF.Atan2(currentDelta.Y, currentDelta.X) - MathF.Atan2(bindDelta.Y, bindDelta.X);
        var scale = currentDelta.Length() / bindDelta.Length();
        var local = vertex - bindStart;
        var cos = MathF.Cos(angle);
        var sin = MathF.Sin(angle);
        var rotated = new Vector2(local.X * cos - local.Y * sin, local.X * sin + local.Y * cos) * scale;
        return currentStart + rotated;
    }
    private static Vector4 CanvasTint(Entity entity) =>
        entity.GetComponent<CanvasItem2D>() is { } item ? item.EffectiveModulate : Vector4.One;

    private static bool CanvasVisible(Entity entity) =>
        entity.GetComponent<CanvasItem2D>() is not { Visible: false };
    private uint GetMaterialProgram(string source)
    {
        if (string.IsNullOrWhiteSpace(source)) return 0;
        if (_materialPrograms.TryGetValue(source, out var cached)) return cached;
        try
        {
            var program = CreateProgram(VertexShaderSource, source);
            _materialPrograms[source] = program;
            return program;
        }
        catch (Exception exception)
        {
            Log.Error($"Shader de material inválido: {exception.Message}", "Render");
            _materialPrograms[source] = 0;
            return 0;
        }
    }

    private void DrawShapes(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            var world = scene.GetWorldMatrix(entity);
            var canvasTint = CanvasTint(entity);
            if (entity.GetComponent<Line2D>() is { Enabled: true, Visible: true, Points.Count: > 1 } line)
            {
                for (var i = 0; i < line.Points.Count - 1; i++)
                    _batch.DrawLine(null, Vector2.Transform(line.Points[i], world), Vector2.Transform(line.Points[i + 1], world), line.Width, line.Color * canvasTint);
                if (line.Loop && line.Points.Count > 2)
                    _batch.DrawLine(null, Vector2.Transform(line.Points[^1], world), Vector2.Transform(line.Points[0], world), line.Width, line.Color * canvasTint);
            }
            if (entity.GetComponent<Polygon2D>() is { Enabled: true, Visible: true, Points.Count: > 2 } polygon)
            {
                if (polygon.Filled)
                {
                    var worldPoints = polygon.Points.Select(point => Vector2.Transform(point, world)).ToList();
                    foreach (var triangle in PolygonTriangulator.Triangulate(worldPoints))
                        _batch.DrawTriangle(null, triangle.A, triangle.B, triangle.C, polygon.Color * canvasTint);
                }
                for (var i = 0; i < polygon.Points.Count; i++)
                {
                    var a = Vector2.Transform(polygon.Points[i], world);
                    var b = Vector2.Transform(polygon.Points[(i + 1) % polygon.Points.Count], world);
                    _batch.DrawLine(null, a, b, polygon.Width, polygon.Color * canvasTint);
                }
            }
        }
    }
    private void DrawYSorted(Scene scene, Vector2 cameraPosition)
    {
        var entries = new List<(int Layer, float Y, int Sort, Action Draw)>();
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;
            var canvas = entity.GetComponent<CanvasLayer>();
            var item = entity.GetComponent<CanvasItem2D>();
            if (canvas is { Visible: false } || item is { Visible: false }) continue;
            var layer = canvas?.Order ?? 0;
            if (entity.GetComponent<SpriteRenderer>() is { Enabled: true, Visible: true } sprite)
                entries.Add((layer, entity.Transform.Position.Y, sprite.SortingOrder, () => DrawYSprite(scene, entity, sprite, cameraPosition, item)));

            foreach (var map in entity.GetComponents<TileMapLayer>())
            {
                if (!map.Enabled || !map.Visible || !map.YSort || string.IsNullOrWhiteSpace(map.EffectiveTexture)) continue;
                var texture = ResolveTexture(map.EffectiveTexture);
                if (texture is null || texture.Width == 0 || texture.Height == 0) continue;
                var columns = System.Math.Max(1, map.EffectiveColumns);
                var cellWidth = map.TileSet is { TileWidth: > 0 } set ? set.TileWidth : texture.Width / (float)columns;
                var cellHeight = map.TileSet is { TileHeight: > 0 } set2 ? set2.TileHeight : cellWidth;
                var size = MathF.Max(0.0001f, map.TileSize);
                for (var y = 0; y < map.Height; y++)
                for (var x = 0; x < map.Width; x++)
                {
                    if (map.DisplayTileAt(x, y) < 0) continue;
                    var worldY = -((y + 0.5f) * size) + map.Offset.Y;
                    entries.Add((layer, worldY, map.SortingOrder, () => DrawYTile(scene, entity, map, x, y, texture, columns, cellWidth, cellHeight, size)));
                }
            }
        }

        entries.Sort((a, b) =>
        {
            var layer = a.Layer.CompareTo(b.Layer);
            if (layer != 0) return layer;
            var y = b.Y.CompareTo(a.Y);
            return y != 0 ? y : a.Sort.CompareTo(b.Sort);
        });
        foreach (var entry in entries) entry.Draw();
    }

    private void DrawYSprite(Scene scene, Entity entity, SpriteRenderer sprite, Vector2 cameraPosition, CanvasItem2D? item)
    {
        var world = scene.GetWorldMatrix(entity);
        var parallax = ParallaxOffset(cameraPosition, sprite.Parallax);
        world.M41 += parallax.X;
        world.M42 += parallax.Y;
        var texture = ResolveTexture(sprite.TexturePath);
        var corners = GetWorldCorners(world, sprite.Size, sprite.Origin);
        var frame = sprite.FrameRect;
        if (frame.Z <= 0f && entity.GetComponent<SpriteAnimator>() is { Enabled: true } animator)
        {
            var columns = Math.Max(1, animator.Columns);
            var start = Math.Clamp(animator.StartFrame, 0, Math.Max(0, animator.FrameCount - 1));
            frame = new Vector4(start % columns * animator.FrameWidth, start / columns * animator.FrameHeight, animator.FrameWidth, animator.FrameHeight);
        }
        var (u0, v0, u1, v1) = (0f, 0f, 1f, 1f);
        if (frame.Z > 0f && texture is not null && texture.Width > 0 && texture.Height > 0)
        {
            u0 = frame.X / texture.Width; v0 = frame.Y / texture.Height;
            u1 = (frame.X + frame.Z) / texture.Width; v1 = (frame.Y + frame.W) / texture.Height;
        }
        if (sprite.FlipX) (u0, u1) = (u1, u0);
        if (sprite.FlipY) (v0, v1) = (v1, v0);
        _batch.SetBlendMode(sprite.BlendMode);
        var tint = sprite.Tint;
        if (item is not null) tint *= item.EffectiveModulate;
        _batch.DrawQuad(texture, corners[0], corners[1], corners[2], corners[3],
            new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1), tint);
    }

    private void DrawYTile(Scene scene, Entity entity, TileMapLayer map, int x, int y,
        Texture2D texture, int columns, float cellWidth, float cellHeight, float size)
    {
        var tile = map.DisplayTileAt(x, y);
        var left = x * size + map.Offset.X;
        var right = (x + 1) * size + map.Offset.X;
        var top = -y * size + map.Offset.Y;
        var bottom = -(y + 1) * size + map.Offset.Y;
        var world = scene.GetWorldMatrix(entity);
        var tl = Vector2.Transform(new Vector2(left, top), world);
        var tr = Vector2.Transform(new Vector2(right, top), world);
        var br = Vector2.Transform(new Vector2(right, bottom), world);
        var bl = Vector2.Transform(new Vector2(left, bottom), world);
        var region = map.TileSet is { } set ? set.RegionFor(tile) : new Vector4((tile % columns) * cellWidth, (tile / columns) * cellHeight, cellWidth, cellHeight);
        var u0 = region.X / texture.Width; var v0 = region.Y / texture.Height;
        var u1 = (region.X + region.Z) / texture.Width; var v1 = (region.Y + region.W) / texture.Height;
        _batch.DrawQuad(texture, tl, tr, br, bl, new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1), Vector4.One);
    }
    private void DrawSpriteShape(SpriteRenderer sprite, Matrix4x4 world, Vector4 tint)
    {
        var half = sprite.Size * 0.5f;
        var points = new List<Vector2>();
        if (sprite.Shape == SpriteShape2D.Cone)
        {
            points.Add(new Vector2(-half.X, -half.Y));
            points.Add(new Vector2(half.X, -half.Y));
            points.Add(new Vector2(0f, half.Y));
        }
        else
        {
            var radius = MathF.Max(0.01f, MathF.Min(half.X, half.Y));
            for (var i = 0; i <= 12; i++)
            {
                var angle = MathF.PI * i / 12f;
                points.Add(new Vector2(MathF.Cos(angle) * radius, half.Y - radius + MathF.Sin(angle) * radius));
            }
            for (var i = 0; i <= 12; i++)
            {
                var angle = MathF.PI + MathF.PI * i / 12f;
                points.Add(new Vector2(MathF.Cos(angle) * radius, -half.Y + radius + MathF.Sin(angle) * radius));
            }
        }

        var center = Vector2.Transform(new Vector2(0f, sprite.Shape == SpriteShape2D.Cone ? -half.Y / 3f : 0f), world);
        for (var i = 0; i < points.Count; i++)
        {
            var a = Vector2.Transform(points[i], world);
            var b = Vector2.Transform(points[(i + 1) % points.Count], world);
            _batch.DrawTriangle(null, center, a, b, tint);
        }
    }
    private void DrawSprites(Scene scene, Vector2 cameraPosition)
    {
        _renderServer.Submit(scene);
        var drawables = _renderServer.Sprites;
        foreach (var (entity, sprite, _, item) in drawables)
        {
            var world = scene.GetWorldMatrix(entity);
            var parallax = ParallaxOffset(cameraPosition, sprite.Parallax);
            world.M41 += parallax.X;
            world.M42 += parallax.Y;
            var texture = ResolveTexture(sprite.TexturePath);
            var corners = GetWorldCorners(world, sprite.Size, sprite.Origin);
            var tint = sprite.Tint;
            if (item is not null) tint *= item.EffectiveModulate;

            if (sprite.Shape != SpriteShape2D.Quad)
            {
                DrawSpriteShape(sprite, world, tint);
                continue;
            }

            // Recorte de animación; en modo edición se usa el cuadro inicial del animador.
            var frame = sprite.FrameRect;
            if (frame.Z <= 0f && entity.GetComponent<SpriteAnimator>() is { Enabled: true } animator)
            {
                var columns = Math.Max(1, animator.Columns);
                var start = Math.Clamp(animator.StartFrame, 0, Math.Max(0, animator.FrameCount - 1));
                frame = new Vector4(
                    start % columns * animator.FrameWidth,
                    start / columns * animator.FrameHeight,
                    animator.FrameWidth,
                    animator.FrameHeight);
            }

            var (u0, v0, u1, v1) = (0f, 0f, 1f, 1f);
            if (frame.Z > 0f && texture is not null && texture.Width > 0 && texture.Height > 0)
            {
                u0 = frame.X / texture.Width;
                v0 = frame.Y / texture.Height;
                u1 = (frame.X + frame.Z) / texture.Width;
                v1 = (frame.Y + frame.W) / texture.Height;
            }

            if (sprite.FlipX)
            {
                (u0, u1) = (u1, u0);
            }

            if (sprite.FlipY)
            {
                (v0, v1) = (v1, v0);
            }

            var material = entity.GetComponent<CanvasMaterial2D>();
            if (material is { Enabled: true })
            {
                var program = GetMaterialProgram(material.FragmentSource);
                if (program != 0) _batch.SetProgram(program, _uniformMvp, _uniformTexture);
                _batch.SetUniform4("uTint", material.Tint);
                foreach (var parameter in material.Parameters) _batch.SetUniform4(parameter.Key, parameter.Value);
            }
            _batch.DrawQuad(texture,
                corners[0], corners[1], corners[2], corners[3],
                new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1),
                tint);
            if (material is { Enabled: true }) _batch.ResetProgram();
        }
    }

    private void DrawTilemaps(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active) continue;

            foreach (var map in entity.GetComponents<TileMapLayer>())
            {
                if (!map.Enabled || !map.Visible || map.YSort || string.IsNullOrWhiteSpace(map.EffectiveTexture)) continue;

                var texture = ResolveTexture(map.EffectiveTexture);
                if (texture is null || texture.Width == 0 || texture.Height == 0) continue;

                var columns = System.Math.Max(1, map.EffectiveColumns);
                var cellWidth = map.TileSet is { TileWidth: > 0 } set ? set.TileWidth : texture.Width / (float)columns;
                var cellHeight = map.TileSet is { TileHeight: > 0 } set2 ? set2.TileHeight : cellWidth;
                var world = scene.GetWorldMatrix(entity);
                var size = MathF.Max(0.0001f, map.TileSize);

                for (var y = 0; y < map.Height; y++)
                {
                    for (var x = 0; x < map.Width; x++)
                    {
                        var tile = map.DisplayTileAt(x, y);
                        if (tile < 0) continue;

                        var left = x * size + map.Offset.X;
                        var right = (x + 1) * size + map.Offset.X;
                        var top = -y * size + map.Offset.Y;
                        var bottom = -(y + 1) * size + map.Offset.Y;

                        var tl = Vector2.Transform(new Vector2(left, top), world);
                        var tr = Vector2.Transform(new Vector2(right, top), world);
                        var br = Vector2.Transform(new Vector2(right, bottom), world);
                        var bl = Vector2.Transform(new Vector2(left, bottom), world);

                        var region = map.TileSet is { } tileSet
                            ? tileSet.RegionFor(tile)
                            : new Vector4((tile % columns) * cellWidth, (tile / columns) * cellHeight, cellWidth, cellHeight);
                        var u0 = region.X / texture.Width;
                        var v0 = region.Y / texture.Height;
                        var u1 = (region.X + region.Z) / texture.Width;
                        var v1 = (region.Y + region.W) / texture.Height;

                        _batch.DrawQuad(texture, tl, tr, br, bl,
                            new Vector2(u0, v0), new Vector2(u1, v0), new Vector2(u1, v1), new Vector2(u0, v1),
                            Vector4.One);
                    }
                }
            }
        }
    }
    private void DrawDebugDraw(Camera2D camera, int viewportHeight)
    {
        var commands = DebugDraw.Snapshot();
        if (commands.Length == 0)
        {
            return;
        }

        var ppu = CameraMath.PixelsPerUnit(camera.OrthographicSize, camera.Zoom, viewportHeight);
        var thickness = 2f / MathF.Max(1f, ppu);

        foreach (var command in commands)
        {
            if (command.Shape == DebugShape.Line)
            {
                _batch.DrawLine(null, command.A, command.B, thickness, command.Color);
            }
        }
    }
    private void DrawSelection(Scene scene, Guid entityId, Camera2D camera, int viewportHeight, GizmoMode gizmoMode = GizmoMode.All)
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

        // Caja de colisión, si la tiene
        var collider = entity.GetComponent<Collider2D>();
        if (collider is { Enabled: true })
        {
            var box = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
            for (var i = 0; i < box.Length; i++)
            {
                _batch.DrawLine(null, box[i], box[(i + 1) % box.Length], 1f / ppu, ColliderColor);
            }
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

        // Secuencia 1, 2, 5, 10… para que la rejilla no salte de golpe al hacer zoom.
        var step = 1f;
        while (step * ppu < 24f)
        {
            step = step < 2f ? 2f : step < 5f ? 5f : step * 2f;
        }

        var minorThickness = 0.8f / ppu;
        var majorThickness = 1.2f / ppu;
        var axisThickness = 2f / ppu;

        var startX = MathF.Floor(left / step) * step;
        for (var x = startX; x <= right; x += step)
        {
            if (MathF.Abs(x) < step * 0.01f) continue;
            var isMajor = MathF.Abs(x % (step * 5f)) < step * 0.01f;
            var color = isMajor ? GridMajorColor : GridMinorColor;
            var thickness = isMajor ? majorThickness : minorThickness;
            _batch.DrawLine(null, new Vector2(x, bottom), new Vector2(x, top), thickness, color);
        }

        var startY = MathF.Floor(bottom / step) * step;
        for (var y = startY; y <= top; y += step)
        {
            if (MathF.Abs(y) < step * 0.01f) continue;
            var isMajor = MathF.Abs(y % (step * 5f)) < step * 0.01f;
            var color = isMajor ? GridMajorColor : GridMinorColor;
            var thickness = isMajor ? majorThickness : minorThickness;
            _batch.DrawLine(null, new Vector2(left, y), new Vector2(right, y), thickness, color);
        }

        // Ejes X/Y siempre visibles y con grosor constante en pantalla.
        if (left <= 0f && right >= 0f)
        {
            _batch.DrawLine(null, new Vector2(0f, bottom), new Vector2(0f, top), axisThickness, AxisYColor);
        }

        if (bottom <= 0f && top >= 0f)
        {
            _batch.DrawLine(null, new Vector2(left, 0f), new Vector2(right, 0f), axisThickness, AxisXColor);
        }
    }
    /// <summary>Esquinas del sprite en mundo: TL, TR, BR, BL.</summary>
    /// <summary>Desplazamiento de un sprite con paralaje respecto a la cámara.</summary>
    /// <summary>
    /// Frame del puente 2.5D: cámara en perspectiva, paredes de caja y sprites
    /// billboard ordenados de lejos a cerca (pintor). Sin búfer de profundidad.
    /// </summary>

    public static Vector2 ParallaxOffset(Vector2 cameraPosition, float parallax) =>
        cameraPosition * (1f - Math.Clamp(parallax, 0f, 1f));

    // ---------- Iluminación ----------

    private static readonly int[] ConeAngles = { 30, 45, 60, 90, 120 };

    private void CreateLightTextures()
    {
        _radialLight = CreateRadialTexture(128);
        foreach (var angle in ConeAngles)
        {
            _coneLights[angle] = CreateConeTexture(128, angle);
        }

        _vignette = CreateVignetteTexture(128);
    }

    private Texture2D CreateRadialTexture(int size)
    {
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                var alpha = distance >= 1f ? 0f : MathF.Pow(1f - distance, 2f);
                var index = (y * size + x) * 4;
                pixels[index] = 255;
                pixels[index + 1] = 255;
                pixels[index + 2] = 255;
                pixels[index + 3] = (byte)(alpha * 255f);
            }
        }

        return Texture2D.FromPixels(_gl, pixels, size, size);
    }

    private Texture2D CreateConeTexture(int size, float degrees)
    {
        var pixels = new byte[size * size * 4];
        var halfAngle = degrees * MathF.PI / 360f;

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                var index = (y * size + x) * 4;

                if (distance >= 1f)
                {
                    continue;
                }

                // El cono apunta hacia arriba en la textura.
                var angle = MathF.Abs(MathF.Atan2(dx, -dy));
                if (angle > halfAngle)
                {
                    continue;
                }

                var edge = Math.Clamp((halfAngle - angle) / MathF.Max(0.0001f, halfAngle * 0.18f), 0f, 1f);
                var falloff = MathF.Pow(1f - distance, 2f);
                var alpha = falloff * edge;

                pixels[index] = 255;
                pixels[index + 1] = 255;
                pixels[index + 2] = 255;
                pixels[index + 3] = (byte)(alpha * 255f);
            }
        }

        return Texture2D.FromPixels(_gl, pixels, size, size);
    }

    private Texture2D CreateVignetteTexture(int size)
    {
        var pixels = new byte[size * size * 4];

        for (var y = 0; y < size; y++)
        {
            for (var x = 0; x < size; x++)
            {
                var dx = (x + 0.5f) / size * 2f - 1f;
                var dy = (y + 0.5f) / size * 2f - 1f;
                var distance = MathF.Sqrt(dx * dx + dy * dy);
                var alpha = Math.Clamp((distance - 0.55f) / 0.55f, 0f, 1f);
                alpha = MathF.Pow(alpha, 1.6f) * 0.9f;
                var index = (y * size + x) * 4;
                pixels[index] = 0;
                pixels[index + 1] = 0;
                pixels[index + 2] = 0;
                pixels[index + 3] = (byte)(alpha * 255f);
            }
        }

        return Texture2D.FromPixels(_gl, pixels, size, size);
    }

    private Texture2D? ConeTexture(float degrees)
    {
        var closest = ConeAngles[0];
        var best = float.MaxValue;
        foreach (var candidate in ConeAngles)
        {
            var difference = MathF.Abs(candidate - degrees);
            if (difference < best)
            {
                best = difference;
                closest = candidate;
            }
        }

        return _coneLights.TryGetValue(closest, out var texture) ? texture : null;
    }

    private void DrawLighting(Scene scene, Camera2D camera, Vector2 cameraPosition, int width, int height,
        Matrix4x4 viewProjection)
    {
        var lights = new List<(Entity Entity, Light2D Light)>();
        foreach (var entity in scene.Entities)
        {
            if (entity.Active && entity.GetComponent<Light2D>() is { Enabled: true } light && light.Radius > 0f)
            {
                lights.Add((entity, light));
            }
        }

        var ambient = camera.Ambient;
        var hasAmbient = ambient.X < 0.999f || ambient.Y < 0.999f || ambient.Z < 0.999f;

        if (lights.Count == 0 && !hasAmbient)
        {
            return;
        }

        var halfHeight = camera.OrthographicSize / MathF.Max(0.01f, camera.Zoom);
        var halfWidth = halfHeight * width / MathF.Max(1, height);
        var extents = new Vector2(halfWidth + 1f, halfHeight + 1f);

        if (hasAmbient)
        {
            _gl.BlendFunc(BlendingFactor.DstColor, BlendingFactor.Zero);
            _batch.Begin(viewProjection);
            DrawViewQuad(cameraPosition, extents, new Vector4(ambient.X, ambient.Y, ambient.Z, 1f), null, false);
            _batch.End();
        Ollin.Core.Diagnostics.Profiler.DrawCalls = _batch.LastDrawCalls;


        }

        if (lights.Count > 0)
        {
            _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.One);
            _batch.Begin(viewProjection);
            foreach (var (entity, light) in lights)
            {
                DrawLightQuad(scene, entity, light);
            }

            _batch.End();
        Ollin.Core.Diagnostics.Profiler.DrawCalls = _batch.LastDrawCalls;



            DrawShadows(scene, lights, viewProjection);
        }

        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
    }

    /// <summary>
    /// Sombras aproximadas: proyecta las aristas de colliders y celdas sólidas y
    /// oscurece el volumen detrás de cada una. Se dibujan después de las luces.
    /// </summary>
    private void DrawShadows(Scene scene, List<(Entity Entity, Light2D Light)> lights, Matrix4x4 viewProjection)
    {
        var edges = BuildOccluderEdges(scene);
        if (edges.Count == 0)
        {
            return;
        }

        var any = lights.Exists(entry => entry.Light.CastShadows && entry.Light.Kind == LightKind.Point);
        if (!any)
        {
            return;
        }

        _gl.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);
        _batch.Begin(viewProjection);

        foreach (var (entity, light) in lights)
        {
            if (!light.CastShadows || light.Kind != LightKind.Point)
            {
                continue;
            }

            var center = scene.GetWorldPosition(entity);
            var shadow = new Vector4(0f, 0f, 0f, 0.5f);

            foreach (var (a, b) in edges)
            {
                if (ShadowGeometry.DistanceToEdge(center, a, b) > light.Radius)
                {
                    continue;
                }

                // Solo las aristas que miran en contra de la luz proyectan sombra.
                var edge = b - a;
                var normal = new Vector2(-edge.Y, edge.X);
                var midpoint = (a + b) * 0.5f;

                if (Vector2.Dot(normal, center - midpoint) > 0f)
                {
                    continue;
                }

                var quad = ShadowGeometry.ProjectEdge(center, a, b, light.Radius);
                _batch.DrawQuad(null, quad.A, quad.B, quad.Bp, quad.Ap,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), shadow);
            }
        }

        _batch.End();
        Ollin.Core.Diagnostics.Profiler.DrawCalls = _batch.LastDrawCalls;


    }

    private void DrawLightQuad(Scene scene, Entity entity, Light2D light)
    {
        var intensity = light.CurrentIntensity > 0f ? light.CurrentIntensity : light.Intensity;
        if (intensity <= 0f)
        {
            return;
        }

        var texture = light.Kind == LightKind.Cone ? ConeTexture(light.ConeAngle) : _radialLight;
        if (texture is null)
        {
            return;
        }

        var world = scene.GetWorldMatrix(entity);
        var corners = GetWorldCorners(world, new Vector2(light.Radius * 2f, light.Radius * 2f), new Vector2(0.5f, 0.5f));
        var tint = new Vector4(
            light.Color.X * intensity,
            light.Color.Y * intensity,
            light.Color.Z * intensity,
            System.Math.Clamp(intensity, 0f, 1f));

        _batch.DrawQuad(texture, corners[0], corners[1], corners[2], corners[3],
            new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(1f, 1f), new Vector2(0f, 1f), tint);
    }

    private static List<(Vector2 A, Vector2 B)> BuildOccluderEdges(Scene scene)
    {
        var edges = new List<(Vector2 A, Vector2 B)>();

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active)
            {
                continue;
            }

            var world = scene.GetWorldMatrix(entity);

            if (entity.GetComponent<Collider2D>() is { Enabled: true, IsTrigger: false } collider)
            {
                AddEdges(edges, collider.GetWorldCorners(world));
            }
        }

        return edges;
    }

    private static void AddEdges(List<(Vector2 A, Vector2 B)> edges, Vector2[] corners)
    {
        for (var i = 0; i < corners.Length; i++)
        {
            edges.Add((corners[i], corners[(i + 1) % corners.Length]));
        }
    }
    /// <summary>Dibuja un cuadro que cubre todo el viewport (ambiente, viñeta).</summary>
    private void DrawViewQuad(Vector2 cameraPosition, Vector2 extents, Vector4 color, Texture2D? texture, bool flipV)
    {
        var topLeft = cameraPosition + new Vector2(-extents.X, extents.Y);
        var topRight = cameraPosition + new Vector2(extents.X, extents.Y);
        var bottomRight = cameraPosition + new Vector2(extents.X, -extents.Y);
        var bottomLeft = cameraPosition + new Vector2(-extents.X, -extents.Y);

        var v0 = flipV ? 1f : 0f;
        var v1 = flipV ? 0f : 1f;
        _batch.DrawQuad(texture, topLeft, topRight, bottomRight, bottomLeft,
            new Vector2(0f, v0), new Vector2(1f, v0), new Vector2(1f, v1), new Vector2(0f, v1), color);
    }

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
        _radialLight?.Dispose();
        foreach (var cone in _coneLights.Values)
        {
            cone.Dispose();
        }

        _coneLights.Clear();
        _vignette?.Dispose();
        _batch.Dispose();

        if (_program != 0)
        {
            _gl.DeleteProgram(_program);
        }
    }
}
