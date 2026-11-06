using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Runtime;

/// <summary>Servidor de áreas 2D: detecta solapamientos y emite entrada/salida.</summary>
public sealed class PhysicsServer2D
{
    private readonly Dictionary<Guid, HashSet<Guid>> _overlaps = new();

    public void Step(Scene scene)
    {
        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Area2D>() is not { Enabled: true } area) continue;
            if (!_overlaps.TryGetValue(entity.Id, out var previous))
            {
                previous = new HashSet<Guid>();
                _overlaps[entity.Id] = previous;
            }

            var current = new HashSet<Guid>();
            var areaBounds = Bounds(scene, entity, area);
            foreach (var other in scene.Entities)
            {
                if (!other.Active || other.Id == entity.Id) continue;
                if (other.GetComponent<Collider2D>() is not { Enabled: true } collider) continue;
                if (!area.CanCollideWith(collider)) continue;
                if (area.IgnoreTags.Any(tag => other.Tags.Contains(tag))) continue;
                var otherBounds = Bounds(scene, other, collider);
                if (Overlaps(areaBounds, otherBounds)) current.Add(other.Id);
            }

            foreach (var id in current)
            {
                if (previous.Contains(id)) continue;
                if (scene.Find(id) is { } body) area.NotifyEnter(body);
            }
            foreach (var id in previous)
            {
                if (current.Contains(id)) continue;
                if (scene.Find(id) is { } body) area.NotifyExit(body);
            }

            previous.Clear();
            foreach (var id in current) previous.Add(id);
            area.Overlapping.Clear();
            foreach (var id in current) area.Overlapping.Add(id);
        }
    }

    private static (Vector2 Min, Vector2 Max) Bounds(Scene scene, Entity entity, Collider2D collider)
    {
        var corners = collider.GetWorldCorners(scene.GetWorldMatrix(entity));
        var min = new Vector2(float.MaxValue, float.MaxValue);
        var max = new Vector2(float.MinValue, float.MinValue);
        foreach (var corner in corners) { min = Vector2.Min(min, corner); max = Vector2.Max(max, corner); }
        return (min, max);
    }

    private static bool Overlaps((Vector2 Min, Vector2 Max) a, (Vector2 Min, Vector2 Max) b) =>
        a.Min.X <= b.Max.X && a.Max.X >= b.Min.X && a.Min.Y <= b.Max.Y && a.Max.Y >= b.Min.Y;
}
