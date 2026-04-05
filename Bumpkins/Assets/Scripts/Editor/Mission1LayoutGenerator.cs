using UnityEngine;
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

        // Row 0 — bottom border: all Water
        SetRect(data,  0,  0, COLS,  1, TileType.Water);

        // Row 1 — left water, right rock
        SetRect(data,  0,  1,  7,   1, TileType.Water);
        SetRect(data, 36,  1, 16,   1, TileType.Rock);

        // Row 2 — left water, right rock
        SetRect(data,  0,  2,  6,   1, TileType.Water);
        SetRect(data, 37,  2, 15,   1, TileType.Rock);

        // Row 3 — left water, trees (TTTTT), right rock
        SetRect(data,  0,  3,  5,   1, TileType.Water);
        SetRect(data, 17,  3,  5,   1, TileType.Wood);
        SetRect(data, 36,  3, 16,   1, TileType.Rock);

        // Row 4 — left water, trees (TTT), right rock
        SetRect(data,  0,  4,  4,   1, TileType.Water);
        SetRect(data, 18,  4,  3,   1, TileType.Wood);
        SetRect(data, 36,  4, 16,   1, TileType.Rock);

        // Row 5 — left water, single tree, right rock
        SetRect(data,  0,  5,  4,   1, TileType.Water);
        Set(data,     19,  5,           TileType.Wood);
        SetRect(data, 36,  5, 16,   1, TileType.Rock);

        // Row 6 — left water, right rock
        SetRect(data,  0,  6,  3,   1, TileType.Water);
        SetRect(data, 37,  6, 15,   1, TileType.Rock);

        // Row 7 — left water, vertical road at col 25, right rock
        SetRect(data,  0,  7,  3,   1, TileType.Water);
        Set(data,     25,  7,           TileType.Road);
        SetRect(data, 37,  7, 15,   1, TileType.Rock);

        // Row 8 — left water, road at col 25, right rock (campfire building at col 22 via BuildingEntry)
        SetRect(data,  0,  8,  2,   1, TileType.Water);
        Set(data,     25,  8,           TileType.Road);
        SetRect(data, 37,  8, 15,   1, TileType.Rock);

        // Row 9 — left water, vertical road at col 25, right rock
        SetRect(data,  0,  9,  2,   1, TileType.Water);
        Set(data,     25,  9,           TileType.Road);
        SetRect(data, 37,  9, 15,   1, TileType.Rock);

        // Row 10 — left water, horizontal road (cols 20-25), right rock
        SetRect(data,  0, 10,  2,   1, TileType.Water);
        SetRect(data, 20, 10,  6,   1, TileType.Road);
        SetRect(data, 36, 10, 16,   1, TileType.Rock);

        // Row 11 — left water, right rock
        SetRect(data,  0, 11,  2,   1, TileType.Water);
        SetRect(data, 37, 11, 15,   1, TileType.Rock);

        // Row 12 — left mountain, trees (TTT), right mountain
        SetRect(data,  0, 12,  3,   1, TileType.Rock);
        SetRect(data, 16, 12,  3,   1, TileType.Wood);
        SetRect(data, 37, 12, 15,   1, TileType.Rock);

        // Row 13 — left mountain, trees (TTTTTTT), right mountain
        SetRect(data,  0, 13,  3,   1, TileType.Rock);
        SetRect(data, 14, 13,  7,   1, TileType.Wood);
        SetRect(data, 37, 13, 15,   1, TileType.Rock);

        // Row 14 — left mountain, trees (TTTTTTT), right mountain
        SetRect(data,  0, 14,  4,   1, TileType.Rock);
        SetRect(data, 14, 14,  7,   1, TileType.Wood);
        SetRect(data, 37, 14, 15,   1, TileType.Rock);

        // Row 15 — left mountain, trees (TTTTTTT), right mountain
        SetRect(data,  0, 15,  5,   1, TileType.Rock);
        SetRect(data, 14, 15,  7,   1, TileType.Wood);
        SetRect(data, 37, 15, 15,   1, TileType.Rock);

        // Row 16 — left mountain, two tree groups (TTTTT....TTTTT), right mountain
        SetRect(data,  0, 16,  5,   1, TileType.Rock);
        SetRect(data, 15, 16,  5,   1, TileType.Wood);
        SetRect(data, 24, 16,  5,   1, TileType.Wood);
        SetRect(data, 37, 16, 15,   1, TileType.Rock);

        // Row 17 — left mountain, trees (TTTTTTT), right mountain
        SetRect(data,  0, 17,  6,   1, TileType.Rock);
        SetRect(data, 22, 17,  7,   1, TileType.Wood);
        SetRect(data, 37, 17, 15,   1, TileType.Rock);

        // Row 18 — left mountain, trees (TTTTT), right mountain
        SetRect(data,  0, 18,  7,   1, TileType.Rock);
        SetRect(data, 24, 18,  5,   1, TileType.Wood);
        SetRect(data, 38, 18, 14,   1, TileType.Rock);

        // Row 19 — full rock band
        SetRect(data,  0, 19, COLS,  1, TileType.Rock);

        // Row 20 — top border: all Water
        SetRect(data,  0, 20, COLS,  1, TileType.Water);

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
