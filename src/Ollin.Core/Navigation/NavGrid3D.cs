using System.Numerics;
using Ollin.Core.Model.Components;
using Ollin.Core.Scenes;

namespace Ollin.Core.Navigation;

/// <summary>
/// Rejilla de navegación 2.5D generada a partir de las cajas <see cref="Wall3D"/>.
/// Marca como bloqueadas las celdas que tocan una pared sólida y resuelve rutas con A*.
/// </summary>
public sealed class NavGrid3D
{
    private readonly bool[] _blocked;

    public float CellSize { get; }

    public float AgentRadius { get; }

    public Vector2 Origin { get; }

    public int Width { get; }

    public int Height { get; }

    public NavGrid3D(float cellSize, float agentRadius, Vector2 origin, int width, int height)
    {
        CellSize = MathF.Max(0.15f, cellSize);
        AgentRadius = MathF.Max(0.05f, agentRadius);
        Origin = origin;
        Width = System.Math.Max(1, width);
        Height = System.Math.Max(1, height);
        _blocked = new bool[Width * Height];
    }

    public static NavGrid3D Build(Scene scene, float cellSize = 0.5f, float agentRadius = 0.4f)
    {
        var walls = new List<Wall3D>();

        foreach (var entity in scene.Entities)
        {
            if (!entity.Active || entity.GetComponent<Wall3D>() is not { Enabled: true, Visible: true, Collides: true } wall)
            {
                continue;
            }

            var (min, max) = wall.Bounds;

            // Una puerta abierta tiene la base alta: deja de bloquear.
            if (min.Y > 1.4f)
            {
                continue;
            }

            walls.Add(wall);
        }

        float minX = -10f, maxX = 10f, minZ = -10f, maxZ = 10f;

        if (walls.Count > 0)
        {
            minX = float.MaxValue;
            maxX = float.MinValue;
            minZ = float.MaxValue;
            maxZ = float.MinValue;

            foreach (var wall in walls)
            {
                var (min, max) = wall.Bounds;
                minX = MathF.Min(minX, min.X);
                maxX = MathF.Max(maxX, max.X);
                minZ = MathF.Min(minZ, min.Z);
                maxZ = MathF.Max(maxZ, max.Z);
            }
        }

        const float margin = 1.5f;
        minX -= margin;
        maxX += margin;
        minZ -= margin;
        maxZ += margin;

        var width = System.Math.Clamp((int)MathF.Ceiling((maxX - minX) / cellSize), 1, 256);
        var height = System.Math.Clamp((int)MathF.Ceiling((maxZ - minZ) / cellSize), 1, 256);
        var grid = new NavGrid3D(cellSize, agentRadius, new Vector2(minX, minZ), width, height);

        foreach (var wall in walls)
        {
            var (min, max) = wall.Bounds;
            var minCell = grid.WorldToCell(new Vector2(min.X, min.Z));
            var maxCell = grid.WorldToCell(new Vector2(max.X, max.Z));
            var x0 = minCell.X;
            var z0 = minCell.Z;
            var x1 = maxCell.X;
            var z1 = maxCell.Z;

            x0 = System.Math.Clamp(x0, 0, grid.Width - 1);
            z0 = System.Math.Clamp(z0, 0, grid.Height - 1);
            x1 = System.Math.Clamp(x1, 0, grid.Width - 1);
            z1 = System.Math.Clamp(z1, 0, grid.Height - 1);

            for (var x = System.Math.Min(x0, x1); x <= System.Math.Max(x0, x1); x++)
            {
                for (var z = System.Math.Min(z0, z1); z <= System.Math.Max(z0, z1); z++)
                {
                    grid._blocked[z * grid.Width + x] = true;
                }
            }
        }

        // Infla los obstáculos por el radio del agente (una celda alrededor).
        var inflate = System.Math.Max(1, (int)MathF.Ceiling(agentRadius / grid.CellSize));
        var original = (bool[])grid._blocked.Clone();

        for (var z = 0; z < grid.Height; z++)
        {
            for (var x = 0; x < grid.Width; x++)
            {
                if (!original[z * grid.Width + x])
                {
                    continue;
                }

                for (var dz = -inflate; dz <= inflate; dz++)
                {
                    for (var dx = -inflate; dx <= inflate; dx++)
                    {
                        var nx = x + dx;
                        var nz = z + dz;

                        if (nx >= 0 && nz >= 0 && nx < grid.Width && nz < grid.Height)
                        {
                            grid._blocked[nz * grid.Width + nx] = true;
                        }
                    }
                }
            }
        }

        return grid;
    }

    public bool IsWalkable(Vector2 world) => IsWalkableCell(WorldToCell(world));

    public bool IsWalkableCell((int X, int Z) cell) =>
        cell.X >= 0 && cell.Z >= 0 && cell.X < Width && cell.Z < Height &&
        !_blocked[cell.Z * Width + cell.X];

    public (int X, int Z) WorldToCell(Vector2 world) => (
        System.Math.Clamp((int)MathF.Floor((world.X - Origin.X) / CellSize), 0, Width - 1),
        System.Math.Clamp((int)MathF.Floor((world.Y - Origin.Y) / CellSize), 0, Height - 1));

    public Vector2 CellToWorld(int x, int z) => new(
        Origin.X + (x + 0.5f) * CellSize,
        Origin.Y + (z + 0.5f) * CellSize);

    public bool TryFindPath(Vector2 start, Vector2 end, out List<Vector2> path)
    {
        path = new List<Vector2>();
        var startCell = NearestFree(WorldToCell(start));
        var endCell = NearestFree(WorldToCell(end));

        if (startCell is null || endCell is null)
        {
            return false;
        }

        var (sx, sz) = startCell.Value;
        var (ex, ez) = endCell.Value;

        if (sx == ex && sz == ez)
        {
            path.Add(end);
            return true;
        }

        var count = Width * Height;
        var gScore = new float[count];
        var fScore = new float[count];
        var cameFrom = new int[count];
        var closed = new bool[count];
        Array.Fill(gScore, float.PositiveInfinity);
        Array.Fill(fScore, float.PositiveInfinity);
        Array.Fill(cameFrom, -1);

        var startIndex = sz * Width + sx;
        var endIndex = ez * Width + ex;
        gScore[startIndex] = 0f;
        fScore[startIndex] = Heuristic(sx, sz, ex, ez);

        var open = new PriorityQueue<int, float>();
        open.Enqueue(startIndex, fScore[startIndex]);

        while (open.TryDequeue(out var current, out _))
        {
            if (current == endIndex)
            {
                Reconstruct(cameFrom, current, end, path);
                return path.Count > 0;
            }

            if (closed[current])
            {
                continue;
            }

            closed[current] = true;
            var cx = current % Width;
            var cz = current / Width;

            for (var dz = -1; dz <= 1; dz++)
            {
                for (var dx = -1; dx <= 1; dx++)
                {
                    if (dx == 0 && dz == 0)
                    {
                        continue;
                    }

                    var nx = cx + dx;
                    var nz = cz + dz;

                    if (!IsWalkableCell((nx, nz)))
                    {
                        continue;
                    }

                    // Sin cortar esquinas en diagonal.
                    if (dx != 0 && dz != 0 &&
                        (!IsWalkableCell((cx + dx, cz)) || !IsWalkableCell((cx, cz + dz))))
                    {
                        continue;
                    }

                    var next = nz * Width + nx;
                    var step = dx != 0 && dz != 0 ? 1.4142135f : 1f;
                    var tentative = gScore[current] + step;

                    if (tentative >= gScore[next])
                    {
                        continue;
                    }

                    cameFrom[next] = current;
                    gScore[next] = tentative;
                    fScore[next] = tentative + Heuristic(nx, nz, ex, ez);

                    if (!closed[next])
                    {
                        open.Enqueue(next, fScore[next]);
                    }
                }
            }
        }

        return false;
    }

    public List<Vector2> Smooth(List<Vector2> path)
    {
        if (path.Count <= 2)
        {
            return path;
        }

        var result = new List<Vector2> { path[0] };
        var anchor = 0;

        while (anchor < path.Count - 1)
        {
            var furthest = anchor + 1;

            for (var next = path.Count - 1; next > anchor; next--)
            {
                if (LineWalkable(path[anchor], path[next]))
                {
                    furthest = next;
                    break;
                }
            }

            result.Add(path[furthest]);
            anchor = furthest;
        }

        return result;
    }

    private bool LineWalkable(Vector2 a, Vector2 b)
    {
        var distance = Vector2.Distance(a, b);
        var steps = System.Math.Max(1, (int)MathF.Ceiling(distance / (CellSize * 0.5f)));
        var previous = WorldToCell(a);

        for (var i = 1; i <= steps; i++)
        {
            var point = Vector2.Lerp(a, b, i / (float)steps);
            var cell = WorldToCell(point);

            if (!IsWalkableCell(cell))
            {
                return false;
            }

            if (cell != previous &&
                (!IsWalkableCell((cell.X, previous.Z)) || !IsWalkableCell((previous.X, cell.Z))))
            {
                return false;
            }

            previous = cell;
        }

        return true;
    }

    private (int X, int Z)? NearestFree((int X, int Z) cell)
    {
        if (IsWalkableCell(cell))
        {
            return cell;
        }

        for (var radius = 1; radius <= 6; radius++)
        {
            for (var dz = -radius; dz <= radius; dz++)
            {
                for (var dx = -radius; dx <= radius; dx++)
                {
                    if (System.Math.Abs(dx) != radius && System.Math.Abs(dz) != radius)
                    {
                        continue;
                    }

                    var candidate = (cell.X + dx, cell.Z + dz);

                    if (IsWalkableCell(candidate))
                    {
                        return candidate;
                    }
                }
            }
        }

        return null;
    }

    private static float Heuristic(int x, int z, int ex, int ez)
    {
        var dx = System.Math.Abs(x - ex);
        var dz = System.Math.Abs(z - ez);
        return MathF.Max(dx, dz) + 0.4142135f * MathF.Min(dx, dz);
    }

    private void Reconstruct(int[] cameFrom, int current, Vector2 end, List<Vector2> path)
    {
        var cells = new List<Vector2>();

        while (current >= 0)
        {
            cells.Add(CellToWorld(current % Width, current / Width));
            current = cameFrom[current];
        }

        cells.Reverse();
        path.AddRange(Smooth(cells));

        if (path.Count > 0)
        {
            path[^1] = end;
        }
    }
}
