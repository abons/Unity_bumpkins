using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Static A* pathfinding grid. Built from MapLayoutData terrain + building footprints.
/// Rock, Wood and Water tiles are blocked. Building footprints are blocked.
/// Call NavGrid.Build() after the map loads; call SetBlocked() when a building is placed at runtime.
/// </summary>
public static class NavGrid
{
    private static bool[]  _walkable;
    private static int     _cols, _rows;
    private static float   _isoHalfW, _isoHalfH;

    private static readonly Vector2Int[] Dirs8 =
    {
        new Vector2Int( 1,  0), new Vector2Int(-1,  0),
        new Vector2Int( 0,  1), new Vector2Int( 0, -1),
        new Vector2Int( 1,  1), new Vector2Int( 1, -1),
        new Vector2Int(-1,  1), new Vector2Int(-1, -1),
    };

    public static bool IsBuilt => _walkable != null;

    // ---- Initialisation ----

    public static void Build(MapLayoutData layout)
    {
        _cols     = layout.cols;
        _rows     = layout.rows;
        _isoHalfW = layout.isoHalfW;
        _isoHalfH = layout.isoHalfH;
        _walkable  = new bool[_cols * _rows];

        // Terrain walkability
        for (int r = 0; r < _rows; r++)
        for (int c = 0; c < _cols; c++)
        {
            var tile = layout.GetTile(c, r);
            _walkable[r * _cols + c] = tile != TileType.Rock
                                    && tile != TileType.Wood
                                    && tile != TileType.Water;
        }

        // Block pre-placed building footprints
        if (layout.buildings != null)
        foreach (var b in layout.buildings)
            BlockFootprint(b.position.x, b.position.y, b.size.x, b.size.y);
    }

    private static void BlockFootprint(int col, int row, int w, int h)
    {
        for (int dr = 0; dr < h; dr++)
        for (int dc = 0; dc < w; dc++)
            SetBlocked(col + dc, row + dr, true);
    }

    /// <summary>Mark a single grid cell as blocked (true) or open (false). Use after runtime building placement.</summary>
    public static void SetBlocked(int col, int row, bool blocked)
    {
        if (col < 0 || col >= _cols || row < 0 || row >= _rows) return;
        _walkable[row * _cols + col] = !blocked;
    }

    // ---- Coordinate helpers ----

    public static Vector2Int WorldToCell(Vector2 worldPos)
    {
        float a = worldPos.x / _isoHalfW;
        float b = worldPos.y / _isoHalfH;
        return new Vector2Int(Mathf.RoundToInt((a + b) * 0.5f), Mathf.RoundToInt((b - a) * 0.5f));
    }

    private static Vector2 CellToWorld(Vector2Int cell) =>
        new Vector2((cell.x - cell.y) * _isoHalfW, (cell.x + cell.y) * _isoHalfH);

    // ---- Walkability ----

    private static bool IsWalkable(int col, int row)
    {
        if (col < 0 || col >= _cols || row < 0 || row >= _rows) return false;
        return _walkable[row * _cols + col];
    }

    private static Vector2Int NearestWalkable(Vector2Int cell)
    {
        if (IsWalkable(cell.x, cell.y)) return cell;
        var queue   = new Queue<Vector2Int>();
        var visited = new HashSet<Vector2Int>();
        queue.Enqueue(cell);
        visited.Add(cell);
        while (queue.Count > 0)
        {
            var c = queue.Dequeue();
            if (IsWalkable(c.x, c.y)) return c;
            foreach (var d in Dirs8)
            {
                var n = new Vector2Int(c.x + d.x, c.y + d.y);
                if (!visited.Contains(n)) { visited.Add(n); queue.Enqueue(n); }
            }
        }
        return cell;
    }

    // ---- Path finding ----

    /// <summary>
    /// Returns world-space waypoints from startWorld to endWorld avoiding terrain/building obstacles.
    /// Returns null if no path exists, in which case the caller should fall back to direct movement.
    /// </summary>
    public static List<Vector2> FindPath(Vector2 startWorld, Vector2 endWorld)
    {
        if (!IsBuilt) return null;

        var start = NearestWalkable(WorldToCell(startWorld));
        var goal  = NearestWalkable(WorldToCell(endWorld));

        if (start == goal)
            return new List<Vector2> { endWorld };

        // A* on the grid
        var openSet  = new List<Vector2Int> { start };
        var cameFrom = new Dictionary<Vector2Int, Vector2Int>();
        var gScore   = new Dictionary<Vector2Int, float> { [start] = 0f };
        var fScore   = new Dictionary<Vector2Int, float> { [start] = Heuristic(start, goal) };

        while (openSet.Count > 0)
        {
            // Pick node with lowest fScore (linear scan — grid is tiny)
            var current = openSet[0];
            foreach (var n in openSet)
                if (GetScore(fScore, n) < GetScore(fScore, current)) current = n;

            if (current == goal)
                return Reconstruct(cameFrom, current, endWorld);

            openSet.Remove(current);
            float g = gScore[current];

            foreach (var d in Dirs8)
            {
                var nb = new Vector2Int(current.x + d.x, current.y + d.y);
                if (!IsWalkable(nb.x, nb.y)) continue;

                // Prevent diagonal corner-cutting through solid tiles
                if (d.x != 0 && d.y != 0
                    && !IsWalkable(current.x + d.x, current.y)
                    && !IsWalkable(current.x, current.y + d.y))
                    continue;

                float cost = (d.x != 0 && d.y != 0) ? 1.414f : 1f;
                float ng   = g + cost;

                if (ng < GetScore(gScore, nb))
                {
                    cameFrom[nb] = current;
                    gScore[nb]   = ng;
                    fScore[nb]   = ng + Heuristic(nb, goal);
                    if (!openSet.Contains(nb)) openSet.Add(nb);
                }
            }
        }

        return null; // no path found
    }

    private static float GetScore(Dictionary<Vector2Int, float> d, Vector2Int k) =>
        d.TryGetValue(k, out var v) ? v : float.MaxValue;

    private static float Heuristic(Vector2Int a, Vector2Int b)
    {
        int dx = Mathf.Abs(a.x - b.x), dy = Mathf.Abs(a.y - b.y);
        return Mathf.Max(dx, dy) + (1.414f - 1f) * Mathf.Min(dx, dy); // octile distance
    }

    private static List<Vector2> Reconstruct(
        Dictionary<Vector2Int, Vector2Int> cameFrom, Vector2Int goal, Vector2 exactEnd)
    {
        var cells = new List<Vector2Int> { goal };
        var cur   = goal;
        while (cameFrom.ContainsKey(cur)) { cur = cameFrom[cur]; cells.Add(cur); }
        cells.Reverse();

        var path = new List<Vector2>(cells.Count);
        for (int i = 1; i < cells.Count; i++)   // skip start cell
            path.Add(CellToWorld(cells[i]));

        // Replace last waypoint with exact sub-tile target for smooth arrival
        if (path.Count > 0)
            path[path.Count - 1] = exactEnd;
        else
            path.Add(exactEnd);

        return path;
    }
}
