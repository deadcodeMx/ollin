using System.Numerics;
using Ollin.Core.Diagnostics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;
using Ollin.Scripting;

namespace Ollin.Runtime;

/// <summary>
/// Física 2D mínima: integra velocidades, resuelve solapamientos entre cajas
/// alineadas a los ejes y avisa a los scripts de colisiones y disparadores.
/// La rotación no se tiene en cuenta (las cajas se tratan como AABB).
/// </summary>
internal sealed class PhysicsSystem
{
    private readonly HashSet<(Guid A, Guid B)> _overlapping = new();

    public void Reset() => _overlapping.Clear();

    public void Step(Scene scene, IReadOnlyList<OllinScript> scripts, float deltaTime)
    {
        var bodies = new List<(Entity Entity, Collider2D Collider, Rigidbody2D? Body)>();

        foreach (var characterEntity in scene.Entities)
        {
            if (characterEntity.Active) characterEntity.GetComponent<CharacterBody2D>()?.BeginPhysicsStep();
        }

        foreach (var entity in scene.Entities.ToArray())
        {
            if (!entity.Active)
            {
                continue;
            }

            var collider = entity.GetComponent<Collider2D>();
            if (collider is not { Enabled: true })
            {
                continue;
            }

            var body = entity.GetComponent<Rigidbody2D>();
            if (body is { Enabled: true, IsStatic: false } && body.Velocity != Vector2.Zero)
            {
                entity.Transform.Position += body.Velocity * deltaTime;
            }

            bodies.Add((entity, collider, body));
        }

        var current = new HashSet<(Guid A, Guid B)>();

        for (var i = 0; i < bodies.Count; i++)
        {
            for (var j = i + 1; j < bodies.Count; j++)
            {
                var a = bodies[i];
                var b = bodies[j];

                if (a.Body is { IsStatic: true } && b.Body is { IsStatic: true })
                {
                    continue;
                }

                var layerA = Math.Clamp(a.Collider.Layer, 0, 31);
                var layerB = Math.Clamp(b.Collider.Layer, 0, 31);
                if ((a.Collider.Mask & (1 << layerB)) == 0 || (b.Collider.Mask & (1 << layerA)) == 0)
                {
                    continue;
                }

                var aPosition = scene.GetWorldPosition(a.Entity);
                var bPosition = scene.GetWorldPosition(b.Entity);
                if ((a.Collider.OneWay && bPosition.Y <= aPosition.Y) ||
                    (b.Collider.OneWay && aPosition.Y <= bPosition.Y))
                {
                    continue;
                }

                GetBounds(scene, a.Entity, a.Collider, out var minA, out var maxA);
                GetBounds(scene, b.Entity, b.Collider, out var minB, out var maxB);

                if (maxA.X <= minB.X || minA.X >= maxB.X || maxA.Y <= minB.Y || minA.Y >= maxB.Y)
                {
                    continue;
                }

                var key = Ordered(a.Entity.Id, b.Entity.Id);
                current.Add(key);

                var isTrigger = a.Collider.IsTrigger || b.Collider.IsTrigger;
                var isNew = _overlapping.Add(key);

                if (isTrigger)
                {
                    if (isNew)
                    {
                        Notify(scripts, a.Entity, b.Entity, entered: true);
                    }

                    continue;
                }

                if (isNew)
                {
                    Notify(scripts, a.Entity, b.Entity, entered: true);
                }

                if (!CollisionMath.TryGetPenetration(
                        a.Collider, scene.GetWorldMatrix(a.Entity),
                        b.Collider, scene.GetWorldMatrix(b.Entity),
                        out var normal, out var penetration) || penetration <= 0.0001f)
                {
                    continue;
                }

                Push(a, b, normal * penetration);

                var platformDeltaA = b.Entity.GetComponent<MovingPlatform2D>()?.DeltaThisFrame ?? Vector2.Zero;
                var platformDeltaB = a.Entity.GetComponent<MovingPlatform2D>()?.DeltaThisFrame ?? Vector2.Zero;
                a.Entity.GetComponent<CharacterBody2D>()?.RegisterContact(normal, b.Entity);
                b.Entity.GetComponent<CharacterBody2D>()?.RegisterContact(-normal, a.Entity);
            }
        }

        // Colisión contra celdas sólidas de los tilemaps (marcadas con Collides).
        var tilemaps = new List<(Entity Entity, Tilemap Map)>();
        foreach (var entity in scene.Entities)
        {
            var map = entity.GetComponent<Tilemap>();
            if (entity.Active && map is { Enabled: true, Collides: true })
            {
                tilemaps.Add((entity, map));
            }
        }

        if (tilemaps.Count > 0)
        {
            foreach (var body in bodies)
            {
                if (body.Body is not { IsStatic: false })
                {
                    continue;
                }

                GetBounds(scene, body.Entity, body.Collider, out var bodyMin, out var bodyMax);

                foreach (var (mapEntity, map) in tilemaps)
                {
                    var world = scene.GetWorldMatrix(mapEntity);
                    var hit = false;

                    for (var y = 0; y < map.Height; y++)
                    {
                        for (var x = 0; x < map.Width; x++)
                        {
                            if (map.TileAt(x, y) < 0)
                            {
                                continue;
                            }

                            var origin = map.CellOrigin(x, y);
                            var size = new Vector2(map.TileSize, map.TileSize);
                            var cellMin = new Vector2(float.MaxValue);
                            var cellMax = new Vector2(float.MinValue);

                            foreach (var corner in new[]
                            {
                                origin,
                                origin + new Vector2(size.X, 0f),
                                origin + size,
                                origin + new Vector2(0f, size.Y),
                            })
                            {
                                var worldCorner = Vector2.Transform(corner, world);
                                cellMin = Vector2.Min(cellMin, worldCorner);
                                cellMax = Vector2.Max(cellMax, worldCorner);
                            }

                            if (bodyMax.X <= cellMin.X || bodyMin.X >= cellMax.X || bodyMax.Y <= cellMin.Y || bodyMin.Y >= cellMax.Y)
                            {
                                continue;
                            }

                            hit = true;

                            var overlapX = MathF.Min(bodyMax.X - cellMin.X, cellMax.X - bodyMin.X);
                            var overlapY = MathF.Min(bodyMax.Y - cellMin.Y, cellMax.Y - bodyMin.Y);

                            Vector2 tileNormal;
                            if (overlapX < overlapY)
                            {
                                var direction = body.Entity.Transform.Position.X < (cellMin.X + cellMax.X) * 0.5f ? -1f : 1f;
                                body.Entity.Transform.Position += new Vector2(overlapX * direction, 0f);
                                CancelVelocity(body, new Vector2(direction, 0f));
                                tileNormal = new Vector2(direction, 0f);
                            }
                            else
                            {
                                var direction = body.Entity.Transform.Position.Y < (cellMin.Y + cellMax.Y) * 0.5f ? -1f : 1f;
                                body.Entity.Transform.Position += new Vector2(0f, overlapY * direction);
                                CancelVelocity(body, new Vector2(0f, direction));
                                tileNormal = new Vector2(0f, direction);
                            }

                            GetBounds(scene, body.Entity, body.Collider, out bodyMin, out bodyMax);
                            body.Entity.GetComponent<CharacterBody2D>()?.RegisterContact(tileNormal, mapEntity);
                        }
                    }

                    if (hit)
                    {
                        var key = Ordered(body.Entity.Id, mapEntity.Id);
                        current.Add(key);

                        if (_overlapping.Add(key))
                        {
                            Notify(scripts, body.Entity, mapEntity, entered: true);
                        }
                    }
                }
            }
        }

        // Pares que dejaron de solaparse: se avisa solo a los disparadores.
        foreach (var pair in _overlapping)
        {
            if (current.Contains(pair))
            {
                continue;
            }

            var a = scene.Find(pair.A);
            var b = scene.Find(pair.B);
            if (a is null || b is null)
            {
                continue;
            }

            if (a.GetComponent<Collider2D>()?.IsTrigger == true || b.GetComponent<Collider2D>()?.IsTrigger == true)
            {
                NotifyExit(scripts, a, b);
            }
        }

        _overlapping.Clear();
        _overlapping.UnionWith(current);
    }

    private static void Push(
        (Entity Entity, Collider2D Collider, Rigidbody2D? Body) a,
        (Entity Entity, Collider2D Collider, Rigidbody2D? Body) b,
        Vector2 delta)
    {
        var aDynamic = a.Body is { IsStatic: false, IsKinematic: false };
        var bDynamic = b.Body is { IsStatic: false, IsKinematic: false };

        if (aDynamic && bDynamic)
        {
            a.Entity.Transform.Position += delta * 0.5f;
            b.Entity.Transform.Position -= delta * 0.5f;
            CancelVelocity(a, delta);
            CancelVelocity(b, -delta);
        }
        else if (aDynamic)
        {
            a.Entity.Transform.Position += delta;
            CancelVelocity(a, delta);
        }
        else if (bDynamic)
        {
            b.Entity.Transform.Position -= delta;
            CancelVelocity(b, -delta);
        }
    }

    /// <summary>
    /// Anula la velocidad en el eje del empuje: evita que el cuerpo vuelva a
    /// penetrar el sólido en el siguiente paso (y que acabe atravesándolo).
    /// </summary>
    private static void CancelVelocity((Entity Entity, Collider2D Collider, Rigidbody2D? Body) body, Vector2 push)
    {
        if (body.Body is not { IsStatic: false } rigidbody)
        {
            return;
        }

        var velocity = rigidbody.Velocity;

        if (MathF.Abs(push.X) > 0.0001f)
        {
            velocity.X = 0f;
        }

        if (MathF.Abs(push.Y) > 0.0001f)
        {
            velocity.Y = 0f;
        }

        rigidbody.Velocity = velocity;
    }

    private static void GetBounds(Scene scene, Entity entity, Collider2D collider, out Vector2 min, out Vector2 max)
    {
        var corners = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
        min = new Vector2(float.MaxValue, float.MaxValue);
        max = new Vector2(float.MinValue, float.MinValue);

        foreach (var corner in corners)
        {
            min = Vector2.Min(min, corner);
            max = Vector2.Max(max, corner);
        }
    }

    private static (Guid A, Guid B) Ordered(Guid a, Guid b) =>
        a.CompareTo(b) < 0 ? (a, b) : (b, a);

    private static void Notify(IReadOnlyList<OllinScript> scripts, Entity a, Entity b, bool entered)
    {
        foreach (var script in scripts)
        {
            if (script.Entity == a)
            {
                Call(script, b, entered);
            }
            else if (script.Entity == b)
            {
                Call(script, a, entered);
            }
        }
    }

    private static void NotifyExit(IReadOnlyList<OllinScript> scripts, Entity a, Entity b)
    {
        foreach (var script in scripts)
        {
            try
            {
                if (script.Entity == a)
                {
                    script.OnTriggerExit(b);
                }
                else if (script.Entity == b)
                {
                    script.OnTriggerExit(a);
                }
            }
            catch (Exception exception)
            {
                Log.Error($"Error en OnTriggerExit de {script.GetType().Name}: {exception.Message}", "Physics");
            }
        }
    }

    private static void Call(OllinScript script, Entity other, bool entered)
    {
        try
        {
            var isTrigger = script.Entity.GetComponent<Collider2D>()?.IsTrigger == true ||
                            other.GetComponent<Collider2D>()?.IsTrigger == true;

            if (isTrigger)
            {
                if (entered)
                {
                    script.OnTriggerEnter(other);
                }
            }
            else if (entered)
            {
                script.OnCollisionEnter(other);
            }
        }
        catch (Exception exception)
        {
            Log.Error($"Error en el callback de física de {script.GetType().Name}: {exception.Message}", "Physics");
        }
    }
}
