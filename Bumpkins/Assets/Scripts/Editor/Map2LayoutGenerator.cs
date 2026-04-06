using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility to generate the Map2Layout ScriptableObject.
///
/// Test/starter map: every BuildingType placed once with a road tile at its door exit.
/// Adjust positions manually in this file, then re-run Tools > Bumpkins > Generate Map2 Layout.
///
/// Grid: 48 cols × 36 rows  (same iso params as Map1)
/// Door-exit rule (mirrors BuildManager.DoorExit):
///   Mill      → (col + 1, row - 1)   SE corner
///   Toolshed  → (col,     row - 1)   SE step
///   All others → (col - 1, row)      SW step
///
/// Building layout (bottom-left col, row):
///
///   Row band A  — row 6  (small/1×1 and 2×2 buildings)
///     Campfire    ( 5,  6)  1×1  door ( 4,  6)
///     ChickenCoop (12,  6)  1×1  door (11,  6)
///     Rockpile    (18,  6)  2×2  door (17,  6)
///     Woodpile    (25,  6)  2×2  door (24,  6)  ← on vertical road spine
///     WheatField  (32,  6)  2×2  door (31,  6)
///
///   Row band B  — row 14  (medium 3×3 / 4×3 buildings)
///     House       ( 4, 14)  3×3  door ( 3, 14)
///     Toolshed    (13, 14)  2×2  door (13, 13)
///     Dairy       (27, 14)  3×3  door (26, 14)
///     Cow         (39, 14)  4×3  door (38, 14)
///
///   Row band C  — row 22  (Mill only)
///     Mill        (20, 22)  4×3  door (21, 21)   ← Mill: col+1, row-1
///
/// Road spine: horizontal row 18 (cols 2–45), vertical col 24 (rows 2–33)
/// </summary>
public static class Map2LayoutGenerator
{
    private const int COLS = 48;
    private const int ROWS = 36;

    [MenuItem("Tools/Bumpkins/Generate Map2 Layout (All Buildings Test)")]
    public static void Generate()
    {
        const string path = "Assets/Scripts/Map2Layout.asset";
        var data = AssetDatabase.LoadAssetAtPath<MapLayoutData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MapLayoutData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.cols        = COLS;
        data.rows        = ROWS;
        data.displayName = "Test — All Buildings";
        data.isoHalfW    = 1.5f;
        data.isoHalfH    = 0.768f;

        // ---- Terrain: all Grass by default ----
        data.terrain = new TileType[COLS * ROWS];  // TileType.Grass == 0

        // Shore border — 1-tile Water on all 4 edges
        SetRect(data,  0,        0, COLS,      1, TileType.Water); // bottom row
        SetRect(data,  0, ROWS - 1, COLS,      1, TileType.Water); // top row
        SetRect(data,  0,        1,    1, ROWS-2, TileType.Water); // left col
        SetRect(data, COLS-1,    1,    1, ROWS-2, TileType.Water); // right col

        // Road spine: horizontal row 18 and vertical col 24
        SetRect(data,  2,      18, COLS-4,      1, TileType.Road); // cols 2–45
        SetRect(data, 24,       2,      1, ROWS-4, TileType.Road); // rows 2–33

        // ---- Door-exit road tiles ----
        // Row band A
        Set(data,  4,  6, TileType.Road); // Campfire    door
        Set(data, 11,  6, TileType.Road); // ChickenCoop door
        Set(data, 17,  6, TileType.Road); // Rockpile    door
        Set(data, 24,  6, TileType.Road); // Woodpile    door  (already on spine)
        Set(data, 31,  6, TileType.Road); // WheatField  door

        // Row band B
        Set(data,  3, 14, TileType.Road); // House       door
        Set(data, 13, 13, TileType.Road); // Toolshed    door
        Set(data, 26, 14, TileType.Road); // Dairy       door
        Set(data, 38, 14, TileType.Road); // Cow         door

        // Row band C
        Set(data, 21, 21, TileType.Road); // Mill        door  (col+1, row-1)

        // ---- Enemies: none on this test map ----
        data.enemies = new EnemySpawnEntry[0];

        // ---- Starting gold ----
        data.startGold = 99999;

        // ---- Starting bumpkins ----
        data.startingBumpkins = new BumpkinSpawnEntry[]
        {
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Male   },
            new BumpkinSpawnEntry { type = BumpkinSpawnType.Female },
        };

        // ---- Buildings: every BuildingType, one each ----
        data.buildings = new BuildingEntry[]
        {
            // ── Row band A (row 6) — small buildings ──────────────────────────
            new BuildingEntry { type = BuildingType.Campfire,    position = new Vector2Int( 5,  6), size = new Vector2Int(1, 1) },
            new BuildingEntry { type = BuildingType.ChickenCoop, position = new Vector2Int(12,  6), size = new Vector2Int(1, 1) },
            new BuildingEntry { type = BuildingType.Rockpile,    position = new Vector2Int(18,  6), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Woodpile,    position = new Vector2Int(25,  6), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.WheatField,  position = new Vector2Int(32,  6), size = new Vector2Int(2, 2) },

            // ── Row band B (row 14) — medium/large buildings ──────────────────
            new BuildingEntry { type = BuildingType.House,       position = new Vector2Int( 4, 14), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Toolshed,    position = new Vector2Int(13, 14), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Dairy,       position = new Vector2Int(27, 14), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Cow,         position = new Vector2Int(39, 14), size = new Vector2Int(4, 3) },

            // ── Row band C (row 22) ───────────────────────────────────────────
            new BuildingEntry { type = BuildingType.Mill,        position = new Vector2Int(20, 22), size = new Vector2Int(4, 3) },
        };

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done",
            $"Map2Layout (All Buildings Test) generated at {path}", "OK");
        Selection.activeObject = data;
    }

    // ── Helpers ─────────────────────────────────────────────────────────────

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
