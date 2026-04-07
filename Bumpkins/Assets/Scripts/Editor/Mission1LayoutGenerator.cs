using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility to generate the Mission1Layout ScriptableObject.
///
/// ASCII source (21 rows × 52 cols, top = north):
///   ~ = Water   ^ = Rock (mountain)   T = Wood (tree)
///   . = Grass   = = Road              C = Campfire building
///
/// Coordinate mapping: ASCII top line → Unity row 20 (ROWS-1), ASCII bottom → Unity row 0.
///
/// Usage: Tools > Bumpkins > Generate Mission 1 Layout
/// </summary>
public static class Mission1LayoutGenerator
{
    private const int COLS = 52;
    private const int ROWS = 21;

    [MenuItem("Tools/Bumpkins/Generate Mission 1 Layout")]
    public static void Generate()
    {
        const string path = "Assets/Scripts/Mission1Layout.asset";
        var data = AssetDatabase.LoadAssetAtPath<MapLayoutData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MapLayoutData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.cols        = COLS;
        data.rows        = ROWS;
        data.displayName = "Mission 1";
        data.isoHalfW    = 1.5f;
        data.isoHalfH    = 0.768f;

        // ---- Terrain ---- (row 0 = bottom/south, row 20 = top/north)
        // Default: all Grass
        data.terrain = new TileType[COLS * ROWS];

        // Diagonal coastline using 2D value noise (coarse grid + bilinear interpolation).
        // Each tile gets a smooth noise value from nearby coarse-grid samples, producing
        // large connected blobs (peninsulas/bays) with genuinely bleeding edges.
        const int   coastDiag  = 24;   // col+row base diagonal; increase = more water
        const int   noiseGrid  = 4;    // coarse noise grid spacing in tiles
        const float noiseAmp   = 5.0f; // how far (in tile units) the coast can deviate
        const float sandWidth  = 4.5f; // half-width of the sand transition band

        // Build coarse noise grid: (COLS/noiseGrid+2) × (ROWS/noiseGrid+2) random values in [-1,1]
        var rng = new System.Random(1337);
        int gw = COLS / noiseGrid + 2;
        int gh = ROWS / noiseGrid + 2;
        float[,] coarse = new float[gw, gh];
        for (int gy = 0; gy < gh; gy++)
            for (int gx = 0; gx < gw; gx++)
                coarse[gx, gy] = (float)(rng.NextDouble() * 2.0 - 1.0);

        // Per-tile: bilinear interpolation of coarse grid → smooth 2D noise
        for (int r = 0; r < ROWS; r++)
        {
            for (int c = 0; c < COLS; c++)
            {
                float fx = (float)c / noiseGrid;
                float fy = (float)r / noiseGrid;
                int x0 = (int)fx,  x1 = x0 + 1;
                int y0 = (int)fy,  y1 = y0 + 1;
                float tx = fx - x0, ty = fy - y0;
                // smooth step for less linear look
                tx = tx * tx * (3f - 2f * tx);
                ty = ty * ty * (3f - 2f * ty);
                x0 = Math.Clamp(x0, 0, gw - 1); x1 = Math.Clamp(x1, 0, gw - 1);
                y0 = Math.Clamp(y0, 0, gh - 1); y1 = Math.Clamp(y1, 0, gh - 1);
                float n = coarse[x0, y0] * (1 - tx) * (1 - ty)
                        + coarse[x1, y0] *      tx  * (1 - ty)
                        + coarse[x0, y1] * (1 - tx) *      ty
                        + coarse[x1, y1] *      tx  *      ty;

                // Signed distance from the base diagonal, displaced by noise
                float d = (c + r) - coastDiag + n * noiseAmp;

                if      (d < -sandWidth) Set(data, c, r, TileType.Water);
                else if (d <  sandWidth) Set(data, c, r, TileType.Sand);
                // else: Grass (already default)
            }
        }

        // Rock mountains — east border wall (rows 1-19)
        SetRect(data, 38,  1, 14, 19, TileType.Rock);

        // Road crossroads — vertical at col 25, rows 7-9; horizontal at row 10, cols 20-25
        Set(data,     25,  7,          TileType.Road);
        Set(data,     25,  8,          TileType.Road);
        Set(data,     25,  9,          TileType.Road);
        SetRect(data, 20, 10,  6,  1, TileType.Road);

        // Trees — south cluster (rows 3-5), east of the beach strip
        SetRect(data, 29,  3,  6,  1, TileType.Wood);
        SetRect(data, 28,  4,  7,  1, TileType.Wood);
        SetRect(data, 28,  5,  5,  1, TileType.Wood);

        // Trees — north forest (rows 12-18), between campfire zone and mountains
        SetRect(data, 26, 12,  8,  1, TileType.Wood);
        SetRect(data, 24, 13, 10,  1, TileType.Wood);
        SetRect(data, 24, 14,  9,  1, TileType.Wood);
        SetRect(data, 24, 15,  8,  1, TileType.Wood);
        SetRect(data, 25, 16,  7,  1, TileType.Wood);
        SetRect(data, 26, 17,  6,  1, TileType.Wood);
        SetRect(data, 27, 18,  5,  1, TileType.Wood);

        // ---- Enemies ----
        data.enemies = new EnemySpawnEntry[0]; // no enemies on this map

        // ---- Starting gold ----
        data.startGold = 10000;

        // ---- Starting bumpkins ----
        data.startingBumpkins = new BumpkinSpawnEntry[]
        {
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Male },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Male },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Female },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Female },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Boy },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Girl },
        };

        // ---- Buildings ----
        data.buildings = new BuildingEntry[]
        {
            new BuildingEntry { type = BuildingType.Campfire, position = new Vector2Int(22, 8), size = new Vector2Int(1, 1) },
        };

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Mission1Layout generated at {path}", "OK");
        Selection.activeObject = data;
    }

    // ---- Helpers ----
    private static void Set(MapLayoutData d, int col, int row, TileType t)
    {
        if (col < 0 || col >= COLS || row < 0 || row >= ROWS) return;
        d.terrain[row * COLS + col] = t;
    }

    private static void SetRect(MapLayoutData d, int col, int row, int w, int h, TileType t)
    {
        for (int r = row; r < row + h; r++)
        for (int c = col; c < col + w; c++)
            Set(d, c, r, t);
    }
}
#endif
