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
        var bodies = new List<(Entity Entity, BoxCollider Collider, Rigidbody2D? Body)>();

        foreach (var entity in scene.Entities.ToArray())
        {
            if (!entity.Active)
            {
                continue;
            }

            var collider = entity.GetComponent<BoxCollider>();
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

                var overlapX = MathF.Min(maxA.X - minB.X, maxB.X - minA.X);
                var overlapY = MathF.Min(maxA.Y - minB.Y, maxB.Y - minA.Y);

                if (overlapX < overlapY)
                {
                    var direction = a.Entity.Transform.Position.X < b.Entity.Transform.Position.X ? -1f : 1f;
                    Push(a, b, new Vector2(overlapX * direction, 0f));
                }
                else
                {
                    var direction = a.Entity.Transform.Position.Y < b.Entity.Transform.Position.Y ? -1f : 1f;
                    Push(a, b, new Vector2(0f, overlapY * direction));
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

            if (a.GetComponent<BoxCollider>()?.IsTrigger == true || b.GetComponent<BoxCollider>()?.IsTrigger == true)
            {
                NotifyExit(scripts, a, b);
            }
        }

        _overlapping.Clear();
        _overlapping.UnionWith(current);
    }

    private static void Push(
        (Entity Entity, BoxCollider Collider, Rigidbody2D? Body) a,
        (Entity Entity, BoxCollider Collider, Rigidbody2D? Body) b,
        Vector2 delta)
    {
        var aDynamic = a.Body is { IsStatic: false };
        var bDynamic = b.Body is { IsStatic: false };

        if (aDynamic && bDynamic)
        {
            a.Entity.Transform.Position += delta * 0.5f;
            b.Entity.Transform.Position -= delta * 0.5f;
        }
        else if (aDynamic)
        {
            a.Entity.Transform.Position += delta;
        }
        else if (bDynamic)
        {
            b.Entity.Transform.Position -= delta;
        }
    }

    private static void GetBounds(Scene scene, Entity entity, BoxCollider collider, out Vector2 min, out Vector2 max)
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
            var isTrigger = script.Entity.GetComponent<BoxCollider>()?.IsTrigger == true ||
                            other.GetComponent<BoxCollider>()?.IsTrigger == true;

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
