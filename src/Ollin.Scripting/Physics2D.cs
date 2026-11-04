using System.Numerics;
using Ollin.Core.Model;
using Ollin.Core.Physics;
using Ollin.Core.Scenes;

namespace Ollin.Scripting;

/// <summary>Consultas físicas 2D para scripts.</summary>
public static class Physics2D
{
    public static RaycastHit2D? Raycast(Scene scene, Vector2 origin, Vector2 direction, float distance,
        int layerMask = ~0, Entity? ignore = null, bool includeTriggers = false) =>
        PhysicsQuery.Raycast(scene, origin, direction, distance, layerMask, ignore, includeTriggers);

    public static RaycastHit2D? BoxCast(Scene scene, Vector2 center, Vector2 size, Vector2 direction, float distance,
        int layerMask = ~0, Entity? ignore = null, bool includeTriggers = false) =>
        PhysicsQuery.BoxCast(scene, center, size, direction, distance, layerMask, ignore, includeTriggers);
}
