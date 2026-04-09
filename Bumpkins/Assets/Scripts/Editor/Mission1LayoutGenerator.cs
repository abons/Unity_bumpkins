using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility to generate the Mission1Layout ScriptableObject.
///
/// Island shape — ellipse centred at (cx=13, cy=14), semi-axes rx=10, ry=11,
/// displaced by smooth value-noise for an organic coastline.
/// Mountains ring the north + east.  Pine forest (FarmPlot floor) in the NW.
/// V-shaped road opens southward from the campfire apex.
/// Two rockpiles flank the campfire.
///
/// Coordinate mapping: row 0 = south/bottom, row 27 = north/top.
///
/// Usage: Tools > Bumpkins > Generate Mission 1 Layout
/// </summary>
public static class Mission1LayoutGenerator
{
    private const int COLS = 28;
    private const int ROWS = 28;

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

        // ---- Terrain ---- (row 0 = south/bottom, row 27 = north/top)
        data.terrain = new TileType[COLS * ROWS];

        // ── Noise setup ────────────────────────────────────────────────
        const int   noiseGrid = 4;
        const float noiseAmp  = 1.8f;   // displacement in ellipse-dist units * normFactor
        const float sandWidth = 1.5f;   // half-width of sand band in tile units

        var rng = new System.Random(1337);
        int gw = COLS / noiseGrid + 2;
        int gh = ROWS / noiseGrid + 2;
        float[,] coarse = new float[gw, gh];
        for (int gy = 0; gy < gh; gy++)
            for (int gx = 0; gx < gw; gx++)
                coarse[gx, gy] = (float)(rng.NextDouble() * 2.0 - 1.0);

        float Noise(int c, int r)
        {
            float fx = (float)c / noiseGrid;
            float fy = (float)r / noiseGrid;
            int x0 = (int)fx, x1 = x0 + 1;
            int y0 = (int)fy, y1 = y0 + 1;
            float tx = fx - x0, ty = fy - y0;
            tx = tx * tx * (3f - 2f * tx);
            ty = ty * ty * (3f - 2f * ty);
            x0 = Math.Clamp(x0, 0, gw - 1); x1 = Math.Clamp(x1, 0, gw - 1);
            y0 = Math.Clamp(y0, 0, gh - 1); y1 = Math.Clamp(y1, 0, gh - 1);
            return coarse[x0, y0] * (1 - tx) * (1 - ty)
                 + coarse[x1, y0] *      tx  * (1 - ty)
                 + coarse[x0, y1] * (1 - tx) *      ty
                 + coarse[x1, y1] *      tx  *      ty;
        }

        // ── Island coast — noise-displaced ellipse ─────────────────────
        // Centre slightly north of grid-centre so mountains have room.
        const float cx = 13f, cy = 14f;
        const float rx = 10f, ry = 11f;
        float normF = Mathf.Max(rx, ry);   // 11 — normalises noise to ellipse space

        for (int r = 0; r < ROWS; r++)
        {
            for (int c = 0; c < COLS; c++)
            {
                float n = Noise(c, r);
                float dc = (c - cx) / rx;
                float dr = (r - cy) / ry;
                // Positive noise expands island outward; negative contracts it.
                float dist = Mathf.Sqrt(dc * dc + dr * dr) - n * noiseAmp / normF;

                if      (dist > 1f + sandWidth / normF) Set(data, c, r, TileType.Water);
                else if (dist > 1f - sandWidth / normF) Set(data, c, r, TileType.Sand);
                // else: Grass (default)
            }
        }

        // ── Mountain arc: north wall + NE corner + east wall ──────────
        // North wall: rows 22-26, cols 5-15
        SetRect(data,  5, 22, 11, 5, TileType.Rock);
        // NW spur wrapping upper-left corner
        SetRect(data,  3, 20,  3, 2, TileType.Rock);
        // NE connector: rows 18-21, cols 15-20
        SetRect(data, 15, 18,  6, 4, TileType.Rock);
        // East wall: cols 20-24, rows 8-18
        SetRect(data, 20,  8,  5, 11, TileType.Rock);
        // East wall lower taper
        SetRect(data, 21,  6,  4,  2, TileType.Rock);

        // ── Pine forest — upper-left / NW of campfire clearing ────────
        // FarmPlot gives the brown soil visible between tree sprites.
        SetRect(data,  5, 15,  8,  1, TileType.FarmPlot);
        SetRect(data,  5, 16,  8,  1, TileType.FarmPlot);
        SetRect(data,  5, 17,  8,  1, TileType.FarmPlot);
        SetRect(data,  5, 18,  7,  1, TileType.FarmPlot);
        SetRect(data,  6, 19,  5,  1, TileType.FarmPlot);
        SetRect(data,  7, 20,  4,  1, TileType.FarmPlot);

        SetRect(data,  5, 15,  8,  1, TileType.Wood);
        SetRect(data,  5, 16,  8,  1, TileType.Wood);
        SetRect(data,  5, 17,  8,  1, TileType.Wood);
        SetRect(data,  5, 18,  7,  1, TileType.Wood);
        SetRect(data,  6, 19,  5,  1, TileType.Wood);
        SetRect(data,  7, 20,  4,  1, TileType.Wood);

        // ── NE clearing — scattered trees between forest and cliffs ───
        Set(data, 17, 15, TileType.Wood);
        Set(data, 18, 13, TileType.Wood);
        Set(data, 16, 12, TileType.Wood);

        // ── Scattered trees — south grass ─────────────────────────────
        Set(data, 12,  4, TileType.Wood);
        Set(data, 14,  5, TileType.Wood);
        Set(data, 11,  5, TileType.Wood);
        Set(data, 15,  4, TileType.Wood);
        Set(data, 10,  4, TileType.Wood);

        // ── V-shaped road — campfire at apex, arms open southward ─────
        // Left arm:  row 12, cols 5-11 (runs lower-left on screen)
        SetRect(data,  5, 12,  7,  1, TileType.Road);
        // Apex tile under campfire
        Set(data,     12, 12,          TileType.Road);
        // Right arm: row 12, cols 13-17 (runs lower-right on screen)
        SetRect(data, 13, 12,  5,  1, TileType.Road);
        // Right arm descends south along col 17
        Set(data,     17, 11,          TileType.Road);
        Set(data,     17, 10,          TileType.Road);
        Set(data,     17,  9,          TileType.Road);

        // ---- Enemies ----
        data.enemies = new EnemySpawnEntry[0];

        // ---- Starting gold ----
        data.startGold = 10000;

        // ---- Starting bumpkins ----
        data.startingBumpkins = new BumpkinSpawnEntry[]
        {
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Male   },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Male   },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Female },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Female },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Boy    },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Girl   },
        };

        // ---- Buildings ----
        data.buildings = new BuildingEntry[]
        {
            // Campfire at the apex of the V-road
            new BuildingEntry { type = BuildingType.Campfire, position = new Vector2Int(12, 12), size = new Vector2Int(1, 1) },
            // Rockpiles flanking the campfire on each road arm
            new BuildingEntry { type = BuildingType.Rockpile, position = new Vector2Int( 4, 12), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Rockpile, position = new Vector2Int(17,  8), size = new Vector2Int(2, 2) },
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
