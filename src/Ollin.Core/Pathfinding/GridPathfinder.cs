namespace Ollin.Core.Pathfinding;

public readonly record struct GridPoint(int X, int Y);

/// <summary>
/// A* sobre una rejilla. La transitabilidad la decide quien llama, de forma que
/// sirve tanto para tilemaps como para cualquier otra cuadrícula.
/// </summary>
public static class GridPathfinder
{
    private static readonly (int X, int Y)[] Directions =
    {
        (1, 0), (-1, 0), (0, 1), (0, -1),
        (1, 1), (1, -1), (-1, 1), (-1, -1),
    };

    /// <summary>
    /// Devuelve el camino desde <paramref name="start"/> hasta <paramref name="goal"/>
    /// (sin incluir el inicio) o null si no hay ruta.
    /// </summary>
    public static List<GridPoint>? FindPath(
        int width,
        int height,
        GridPoint start,
        GridPoint goal,
        Func<int, int, bool> walkable,
        bool allowDiagonal = true)
    {
        if (width <= 0 || height <= 0)
        {
            return null;
        }

        if (!InBounds(start, width, height) || !InBounds(goal, width, height))
        {
            return null;
        }

        if (start == goal)
        {
            return new List<GridPoint>();
        }

        if (!walkable(goal.X, goal.Y))
        {
            return null;
        }

        var open = new PriorityQueue<GridPoint, float>();
        var cameFrom = new Dictionary<GridPoint, GridPoint>();
        var cost = new Dictionary<GridPoint, float> { [start] = 0f };
        var closed = new HashSet<GridPoint>();

        open.Enqueue(start, Heuristic(start, goal));

        while (open.Count > 0)
        {
            var current = open.Dequeue();

            if (current == goal)
            {
                return Reconstruct(cameFrom, start, goal);
            }

            if (!closed.Add(current))
            {
                continue;
            }

            foreach (var (dx, dy) in Directions)
            {
                if (!allowDiagonal && dx != 0 && dy != 0)
                {
                    continue;
                }

                var next = new GridPoint(current.X + dx, current.Y + dy);

                if (!InBounds(next, width, height) || closed.Contains(next) || !walkable(next.X, next.Y))
                {
                    continue;
                }

                // Sin cortar esquinas: en diagonal, ambos lados deben ser libres.
                if (dx != 0 && dy != 0 &&
                    (!walkable(current.X + dx, current.Y) || !walkable(current.X, current.Y + dy)))
                {
                    continue;
                }

                var step = dx != 0 && dy != 0 ? 1.41421356f : 1f;
                var tentative = cost[current] + step;

                if (cost.TryGetValue(next, out var existing) && tentative >= existing)
                {
                    continue;
                }

                cost[next] = tentative;
                cameFrom[next] = current;
                open.Enqueue(next, tentative + Heuristic(next, goal));
            }
        }

        return null;
    }

    private static bool InBounds(GridPoint point, int width, int height) =>
        point.X >= 0 && point.Y >= 0 && point.X < width && point.Y < height;

    private static float Heuristic(GridPoint a, GridPoint b)
    {
        var dx = MathF.Abs(a.X - b.X);
        var dy = MathF.Abs(a.Y - b.Y);
        return dx + dy + (1.41421356f - 2f) * MathF.Min(dx, dy);
    }

    private static List<GridPoint> Reconstruct(
        Dictionary<GridPoint, GridPoint> cameFrom,
        GridPoint start,
        GridPoint goal)
    {
        var path = new List<GridPoint>();
        var current = goal;

        while (current != start)
        {
            path.Add(current);

            if (!cameFrom.TryGetValue(current, out current))
            {
                break;
            }
        }

        path.Reverse();
        return path;
    }
}
