using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility to generate the Map2Layout ScriptableObject.
///
/// Test/starter map: every BuildingType placed once with a road tile at its door exit.
/// Adjust positions manually in this file, then re-run Tools > Bumpkins > Generate Map2 Layout.
///
/// Grid: 12 cols × 12 rows
/// Door-exit rule (mirrors BuildManager.DoorExit):
///   Mill      → (col + 1, row - 1)   SE corner
///   Toolshed  → (col,     row - 1)   SE step
///   All others → (col - 1, row)      SW step
///
/// Building layout (bottom-left col, row), interior cols 1–10, rows 1–10:
///
///   Row 1      — 1×1 singles
///     Campfire    ( 1,  1)  1×1  door at water edge, skipped
///     ChickenCoop ( 3,  1)  1×1  door ( 2,  1)
///
///   Rows 2–3   — 2×2 buildings
///     WheatField  ( 1,  2)  2×2  door at water edge, skipped
///     Rockpile    ( 5,  2)  2×2  door ( 4,  2)
///     Woodpile    ( 8,  2)  2×2  door ( 7,  2)
///
///   Rows 5–7   — 3×3 buildings
///     House       ( 1,  5)  3×3  door at water edge, skipped
///     Toolshed    ( 4,  5)  3×3  door ( 4,  4)
///     Dairy       ( 7,  5)  3×3  door ( 6,  5)
///
///   Rows 8–10  — 4×3 buildings
///     Mill        ( 1,  8)  4×3  door ( 2,  7)
///     Cow         ( 6,  8)  4×3  door ( 5,  8)
///
/// No road spine — only individual door-exit road tiles.
/// </summary>
public static class Map2LayoutGenerator
{
    private const int COLS = 12;
    private const int ROWS = 12;

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

        // ---- Door-exit road tiles only (no road spine) ----
        Set(data,  2,  1, TileType.Road); // ChickenCoop door
        Set(data,  4,  2, TileType.Road); // Rockpile    door
        Set(data,  7,  2, TileType.Road); // Woodpile    door
        Set(data,  4,  4, TileType.Road); // Toolshed    door (col, row-1)
        Set(data,  6,  5, TileType.Road); // Dairy       door
        Set(data,  2,  7, TileType.Road); // Mill        door (col+1, row-1)
        Set(data,  5,  8, TileType.Road); // Cow         door

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
            // ── Row 1 — 1×1 singles ───────────────────────────────────────────
            new BuildingEntry { type = BuildingType.Campfire,    position = new Vector2Int( 1,  1), size = new Vector2Int(1, 1) },
            new BuildingEntry { type = BuildingType.ChickenCoop, position = new Vector2Int( 3,  1), size = new Vector2Int(1, 1) },

            // ── Rows 2–3 — 2×2 buildings ─────────────────────────────────────
            new BuildingEntry { type = BuildingType.WheatField,  position = new Vector2Int( 1,  2), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Rockpile,    position = new Vector2Int( 5,  2), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Woodpile,    position = new Vector2Int( 8,  2), size = new Vector2Int(2, 2) },

            // ── Rows 5–7 — 3×3 buildings ─────────────────────────────────────
            new BuildingEntry { type = BuildingType.House,       position = new Vector2Int( 1,  5), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Toolshed,    position = new Vector2Int( 4,  5), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Dairy,       position = new Vector2Int( 7,  5), size = new Vector2Int(3, 3) },

            // ── Rows 8–10 — 4×3 buildings ────────────────────────────────────
            new BuildingEntry { type = BuildingType.Mill,        position = new Vector2Int( 1,  8), size = new Vector2Int(4, 3) },
            new BuildingEntry { type = BuildingType.Cow,         position = new Vector2Int( 6,  8), size = new Vector2Int(4, 3) },
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
