using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Pure data-only battlefield generator. Deterministic given the same seed.
/// Produces a BattlefieldData object describing cells, launcher nodes, spawn points, and wall segments.
/// </summary>
public static class BattlefieldGenerator
{
    public static BattlefieldData Generate(int seed, MapSize preset, GenerationOptions opts = null)
    {
        if (opts == null) opts = GenerationOptions.Default();

        UnityEngine.Random.InitState(seed);

        // Map size presets (in world units)
        Vector2 worldSize = GetPresetSize(preset);
        float cellSize = opts.cellSize;

        int cellW = Mathf.FloorToInt(worldSize.x / cellSize);
        int cellH = Mathf.FloorToInt(worldSize.y / cellSize);

        // Ensure minimum sizes
        cellW = Mathf.Max(4, cellW);
        cellH = Mathf.Max(4, cellH);

        // Create battlefield bounds centered at origin
        Bounds bounds = new Bounds(Vector3.zero, new Vector3(cellW * cellSize, cellH * cellSize, 0f));

        GridCell[,] cells = new GridCell[cellW, cellH];
        Vector2 bottomLeft = new Vector2(bounds.center.x - bounds.extents.x, bounds.center.y - bounds.extents.y);

        // Initialize cells as open
        for (int x = 0; x < cellW; x++)
            for (int y = 0; y < cellH; y++)
            {
                Vector2 worldPos = bottomLeft + new Vector2((x + 0.5f) * cellSize, (y + 0.5f) * cellSize);
                cells[x, y] = new GridCell(x, y, worldPos, TerrainType.Open);
            }

        BattlefieldData data = new BattlefieldData
        {
            seed = seed,
            preset = preset,
            cellSize = cellSize,
            bounds = bounds,
            cells = cells
        };  

        // Buffer ring (reserve edge cells for walls)
        int bufferCells = Mathf.Max(1, Mathf.RoundToInt(opts.wallBuffer / cellSize));
        MarkEdgeBuffer(data, bufferCells);

        // Launcher nodes: sample around boundary outside bounds by launcherOffset
        data.launcherNodes = GenerateLauncherNodes(data, opts.launcherNodeSpacing, opts.launcherOffset);

        // Carve large features (rooms, funnel)
        CarveRoomsAndFunnels(data, opts);

        // Place maze-ish obstacles for twistiness
        CarveMazeAndNoise(data, opts);

        // Enforce min/max open cells
        BalanceOpenCells(data, opts);

        // Spawn points
        data.enemySpawns = PickSpawnPoints(data, opts.spawnCount, opts.spawnDistanceFromLauncherExit);

        // Assign wall segments and types
        data.wallSegments = BuildWallSegments(data, opts);

        // Final metadata
        data.openCellCount = CountOpenCells(data);
        data.spawnCount = data.enemySpawns.Count;

        return data;
    }

    public static Vector2 GetPresetSize(MapSize preset)
    {
        switch (preset)
        {
            case MapSize.Small: return new Vector2(12f, 8f);
            case MapSize.Medium: return new Vector2(20f, 12f);
            case MapSize.Large: return new Vector2(30f, 18f);
            case MapSize.BossRoom: return new Vector2(50f, 30f);
            default: return new Vector2(20f, 12f);
        }
    }

    static void MarkEdgeBuffer(BattlefieldData data, int buffer)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (x < buffer || y < buffer || x >= w - buffer || y >= h - buffer)
                    data.cells[x, y].terrain = TerrainType.BlockedWallBuffer;
            }
    }

    static List<LauncherNode> GenerateLauncherNodes(BattlefieldData data, float spacingWorld, float launcherOffset)
    {
        List<LauncherNode> nodes = new List<LauncherNode>();
        // Sample nodes along 4 edges at spacingWorld intervals, positioned just outside bounds by launcherOffset
        float halfW = data.bounds.extents.x;
        float halfH = data.bounds.extents.y;
        float cell = data.cellSize;

        // We'll sample along edges using world spacing
        float perimeterTop = data.bounds.size.x;
        float perimeterSide = data.bounds.size.y;

        // Top edge (x varies)
        for (float x = -halfW + spacingWorld / 2f; x < halfW; x += spacingWorld)
        {
            Vector2 pos = new Vector2(x, halfH + launcherOffset);
            nodes.Add(new LauncherNode(pos, (Vector2.down)));
        }
        // Right edge (y varies)
        for (float y = halfH - spacingWorld / 2f; y > -halfH; y -= spacingWorld)
        {
            Vector2 pos = new Vector2(halfW + launcherOffset, y);
            nodes.Add(new LauncherNode(pos, (Vector2.left)));
        }
        // Bottom
        for (float x = halfW - spacingWorld / 2f; x > -halfW; x -= spacingWorld)
        {
            Vector2 pos = new Vector2(x, -halfH - launcherOffset);
            nodes.Add(new LauncherNode(pos, (Vector2.up)));
        }
        // Left
        for (float y = -halfH + spacingWorld / 2f; y < halfH; y += spacingWorld)
        {
            Vector2 pos = new Vector2(-halfW - launcherOffset, y);
            nodes.Add(new LauncherNode(pos, (Vector2.right)));
        }

        return nodes;
    }

    static void CarveRoomsAndFunnels(BattlefieldData data, GenerationOptions opts)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        // Place 1-3 pockets (rooms) — deterministic by seed
        int pocketCount = UnityEngine.Random.Range(1, 3 + (int)opts.complexity);
        for (int i = 0; i < pocketCount; i++)
        {
            int roomW = UnityEngine.Random.Range(2, Mathf.Max(3, w / 6));
            int roomH = UnityEngine.Random.Range(2, Mathf.Max(3, h / 6));
            int rx = UnityEngine.Random.Range(2, w - roomW - 2);
            int ry = UnityEngine.Random.Range(2, h - roomH - 2);

            // Carve room as OPEN explicitly (rooms are clear spaces)
            for (int x = rx; x < rx + roomW; x++)
                for (int y = ry; y < ry + roomH; y++)
                    data.cells[x, y].terrain = TerrainType.OpenPocket;
        }

        // Simple funnel: pick an entrance on edge and grow towards center
        int funnels = UnityEngine.Random.Range(0, 1 + (int)opts.cohesion);
        for (int f = 0; f < funnels; f++)
        {
            CreateFunnel(data, opts);
        }
    }

    static void CreateFunnel(BattlefieldData data, GenerationOptions opts)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        Vector2Int entrance = new Vector2Int(UnityEngine.Random.Range(1, w - 1), 0); // bottom edge entrance
        Vector2Int target = new Vector2Int(w / 2 + UnityEngine.Random.Range(-w / 6, w / 6), h / 2 + UnityEngine.Random.Range(-h / 6, h / 6));

        // Carve path with biased walk
        Vector2Int cur = entrance;
        for (int steps = 0; steps < w * h / 2; steps++)
        {
            // make cell open
            data.cells[cur.x, cur.y].terrain = TerrainType.Funnel;
            if (cur == target) break;

            // choose next neighbor biased toward target
            Vector2Int best = cur;
            float bestScore = float.NegativeInfinity;
            Vector2Int[] dirs = new Vector2Int[] { Vector2Int.up, Vector2Int.left, Vector2Int.right };
            foreach (var d in dirs)
            {
                Vector2Int n = cur + d;
                if (n.x < 1 || n.y < 1 || n.x >= w - 1 || n.y >= h - 1) continue;
                float score = -Vector2Int.Distance(n, target) + UnityEngine.Random.Range(-opts.cohesion, opts.cohesion);
                if (score > bestScore) { bestScore = score; best = n; }
            }
            cur = best;
        }
    }

    static void CarveMazeAndNoise(BattlefieldData data, GenerationOptions opts)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);

        // Seed some blocked islands; density depends on complexity
        float blockChance = 0.08f + 0.02f * opts.complexity;
        for (int x = 2; x < w - 2; x++)
            for (int y = 2; y < h - 2; y++)
            {
                if (data.cells[x, y].terrain == TerrainType.Open)
                {
                    if (UnityEngine.Random.value < blockChance)
                        data.cells[x, y].terrain = TerrainType.BlockedRock;
                }
            }

        // Smooth islands a bit — cellular automata style
        int passes = 2 + opts.complexity;
        for (int p = 0; p < passes; p++)
        {
            TerrainType[,] copy = new TerrainType[w, h];
            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    copy[x, y] = data.cells[x, y].terrain;

            for (int x = 1; x < w - 1; x++)
                for (int y = 1; y < h - 1; y++)
                {
                    int blockedNeighbors = 0;
                    for (int nx = x - 1; nx <= x + 1; nx++)
                        for (int ny = y - 1; ny <= y + 1; ny++)
                            if (data.cells[nx, ny].terrain != TerrainType.Open && data.cells[nx, ny].terrain != TerrainType.OpenPocket)
                                blockedNeighbors++;

                    if (blockedNeighbors >= 5) copy[x, y] = TerrainType.BlockedRock;
                    else if (blockedNeighbors <= 2) copy[x, y] = TerrainType.Open;
                }

            for (int x = 0; x < w; x++)
                for (int y = 0; y < h; y++)
                    data.cells[x, y].terrain = copy[x, y];
        }
    }

    static void BalanceOpenCells(BattlefieldData data, GenerationOptions opts)
    {
        int open = CountOpenCells(data);
        int attempts = 0;
        while (open < opts.minOpenCells && attempts < 1000)
        {
            // Try to clear small rock clusters first
            if (ClearSmallBlockedCluster(data)) open = CountOpenCells(data);
            else
            {
                // Widen random funnel paths
                WidenRandomPaths(data);
                open = CountOpenCells(data);
            }
            attempts++;
        }

        // If still too many open cells, add blockers
        attempts = 0;
        while (open > opts.maxOpenCells && attempts < 1000)
        {
            AddRandomBlocker(data);
            open = CountOpenCells(data);
            attempts++;
        }
    }

    static bool ClearSmallBlockedCluster(BattlefieldData data)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        for (int x = 1; x < w - 1; x++)
            for (int y = 1; y < h - 1; y++)
            {
                if (data.cells[x, y].terrain == TerrainType.BlockedRock)
                {
                    int size = FloodSize(data, x, y, TerrainType.BlockedRock);
                    if (size <= 4)
                    {
                        ClearCluster(data, x, y, TerrainType.BlockedRock);
                        return true;
                    }
                }
            }
        return false;
    }

    static int FloodSize(BattlefieldData data, int sx, int sy, TerrainType type)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        bool[,] seen = new bool[w, h];
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(sx, sy));
        int count = 0;
        while (q.Count > 0)
        {
            var v = q.Dequeue();
            if (v.x < 0 || v.y < 0 || v.x >= w || v.y >= h) continue;
            if (seen[v.x, v.y]) continue;
            seen[v.x, v.y] = true;
            if (data.cells[v.x, v.y].terrain != type) continue;
            count++;
            q.Enqueue(v + Vector2Int.up);
            q.Enqueue(v + Vector2Int.down);
            q.Enqueue(v + Vector2Int.left);
            q.Enqueue(v + Vector2Int.right);
        }
        return count;
    }

    static void ClearCluster(BattlefieldData data, int sx, int sy, TerrainType type)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        Queue<Vector2Int> q = new Queue<Vector2Int>();
        q.Enqueue(new Vector2Int(sx, sy));
        while (q.Count > 0)
        {
            var v = q.Dequeue();
            if (v.x < 0 || v.y < 0 || v.x >= w || v.y >= h) continue;
            if (data.cells[v.x, v.y].terrain != type) continue;
            data.cells[v.x, v.y].terrain = TerrainType.Open;
            q.Enqueue(v + Vector2Int.up);
            q.Enqueue(v + Vector2Int.down);
            q.Enqueue(v + Vector2Int.left);
            q.Enqueue(v + Vector2Int.right);
        }
    }

    static void WidenRandomPaths(BattlefieldData data)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        int rx = UnityEngine.Random.Range(2, w - 2);
        int ry = UnityEngine.Random.Range(2, h - 2);
        for (int dx = -1; dx <= 1; dx++)
            for (int dy = -1; dy <= 1; dy++)
                if (InBounds(data, rx + dx, ry + dy) && data.cells[rx + dx, ry + dy].terrain != TerrainType.BlockedWallBuffer)
                    data.cells[rx + dx, ry + dy].terrain = TerrainType.Open;
    }

    static void AddRandomBlocker(BattlefieldData data)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        int rx = UnityEngine.Random.Range(2, w - 2);
        int ry = UnityEngine.Random.Range(2, h - 2);
        data.cells[rx, ry].terrain = TerrainType.BlockedRock;
    }

    static List<Vector2> PickSpawnPoints(BattlefieldData data, int spawnCount, int spawnDistanceFromLauncher)
    {
        List<Vector2> candidates = new List<Vector2>();
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1);
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
            {
                if (data.cells[x, y].terrain == TerrainType.Open || data.cells[x, y].terrain == TerrainType.OpenPocket)
                {
                    // distance from center or launcher exit roughly
                    Vector2 world = data.cells[x, y].worldPos;
                    float dist = Vector2.Distance(world, Vector2.zero);
                    if (dist > spawnDistanceFromLauncher) candidates.Add(world);
                }
            }

        List<Vector2> picks = new List<Vector2>();
        // shuffle candidates deterministically
        for (int i = 0; i < candidates.Count; i++)
        {
            int j = UnityEngine.Random.Range(i, candidates.Count);
            var tmp = candidates[i];
            candidates[i] = candidates[j];
            candidates[j] = tmp;
        }

        int take = Mathf.Min(spawnCount, candidates.Count);
        for (int i = 0; i < take; i++) picks.Add(candidates[i]);
        return picks;
    }

    static List<WallSegment> BuildWallSegments(BattlefieldData data, GenerationOptions opts)
    {
        List<WallSegment> walls = new List<WallSegment>();
        float w = data.bounds.extents.x;
        float h = data.bounds.extents.y;
        float t = opts.wallThickness;

        // Top wall
        walls.Add(new WallSegment(
            WallType.Normal,
            new Vector2(0, h + t / 2f),
            new Vector2(w * 2f + t, t)
        ));

        // Bottom wall
        walls.Add(new WallSegment(
            WallType.Normal,
            new Vector2(0, -h - t / 2f),
            new Vector2(w * 2f + t, t)
        ));

        // Right wall
        walls.Add(new WallSegment(
            WallType.Bouncy,
            new Vector2(w + t / 2f, 0),
            new Vector2(t, h * 2f + t)
        ));

        // Left wall
        walls.Add(new WallSegment(
            WallType.Dampen,
            new Vector2(-w - t / 2f, 0),
            new Vector2(t, h * 2f + t)
        ));

        return walls;
    }


    static int CountOpenCells(BattlefieldData data)
    {
        int w = data.cells.GetLength(0), h = data.cells.GetLength(1), count = 0;
        for (int x = 0; x < w; x++)
            for (int y = 0; y < h; y++)
                if (data.cells[x, y].terrain == TerrainType.Open || data.cells[x, y].terrain == TerrainType.OpenPocket || data.cells[x, y].terrain == TerrainType.Funnel)
                    count++;
        return count;
    }

    static bool InBounds(BattlefieldData data, int x, int y)
    {
        return x >= 0 && y >= 0 && x < data.cells.GetLength(0) && y < data.cells.GetLength(1);
    }
}

/// <summary>
/// Data containers and helpers
/// </summary>
[Serializable]
public class BattlefieldData
{
    public int seed;
    public MapSize preset;
    public Bounds bounds;
    public float cellSize;
    public GridCell[,] cells;
    public List<LauncherNode> launcherNodes;
    public List<Vector2> enemySpawns;
    public List<WallSegment> wallSegments;

    // metadata
    public int openCellCount;
    public int spawnCount;
}

[Serializable]
public struct GridCell
{
    public int x, y;
    public Vector2 worldPos;
    public TerrainType terrain;

    public GridCell(int x_, int y_, Vector2 pos, TerrainType t)
    {
        x = x_; y = y_; worldPos = pos; terrain = t;
    }
}

[Serializable]
public class LauncherNode
{
    public Vector2 position;
    public Vector2 inwardNormal;
    public LauncherNode(Vector2 p, Vector2 n) { position = p; inwardNormal = n.normalized; }
}

[Serializable]
public class WallSegment
{
    public WallType type;
    public Vector2 center;
    public Vector2 size;
    public WallSegment(WallType t, Vector2 c, Vector2 s) { type = t; center = c; size = s; }
}

public enum TerrainType { Open, OpenPocket, Funnel, BlockedRock, BlockedWallBuffer }
public enum WallType { Normal, Bouncy, Dampen, Breakable }
public enum MapSize { Small, Medium, Large, BossRoom }

[Serializable]
public class GenerationOptions
{
    public float cellSize = 1f;
    public float wallBuffer = 1f;
    public float launcherNodeSpacing = 2f;
    public float launcherOffset = 1.5f; // how far outside bounds nodes are
    public float wallThickness = 0.5f;
    public int minOpenCells = 20;
    public int maxOpenCells = 400;
    public int spawnCount = 8;
    public int spawnDistanceFromLauncherExit = 3;
    public int complexity = 2; // 0..4
    public int cohesion = 1;  // 0..3

    public static GenerationOptions Default() => new GenerationOptions();
}
