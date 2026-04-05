using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility to generate the EnemyTestLayout ScriptableObject.
///
/// Usage: Tools > Bumpkins > Generate Enemy Test Layout
/// </summary>
public static class EnemyTestLayoutGenerator
{
    // Grid: 48 cols × 36 rows
    private const int COLS = 48;
    private const int ROWS = 36;

    [MenuItem("Tools/Bumpkins/Generate Enemy Test Layout")]
    public static void Generate()
    {
        const string path = "Assets/Scripts/EnemyTestLayout.asset";
        var data = AssetDatabase.LoadAssetAtPath<MapLayoutData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MapLayoutData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.cols        = COLS;
        data.rows        = ROWS;
        data.displayName = "Enemy Test";

        // ---- Terrain ----
        data.terrain = new TileType[COLS * ROWS];

        // Central crossroads at map center (24, 18)
        // Horizontal: row 18, cols 22-26
        SetRow(data, row: 18, colStart: 22, colEnd: 26, TileType.Road);
        // Vertical: col 24, rows 16-20
        SetCol(data, col: 24, rowStart: 16, rowEnd: 20, TileType.Road);

        // Rock terrain
        SetRect(data, col: 8, row: 10, w: 2, h: 2, TileType.Rock);

        // Shore border
        SetRect(data, col:  0, row:  0, w: COLS, h: 1,    TileType.Water); // south
        SetRect(data, col:  0, row: 35, w: COLS, h: 1,    TileType.Water); // north
        SetRect(data, col:  0, row:  0, w: 1,    h: ROWS, TileType.Water); // west
        SetRect(data, col: 47, row:  0, w: 1,    h: ROWS, TileType.Water); // east

        // ---- Buildings ----
        data.buildings = new BuildingEntry[]
        {
            new BuildingEntry { type = BuildingType.Cow,      position = new Vector2Int(27, 16), size = new Vector2Int(4, 3) },
            new BuildingEntry { type = BuildingType.Rockpile, position = new Vector2Int( 8, 10), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Woodpile, position = new Vector2Int(36, 10), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Campfire, position = new Vector2Int(25, 22), size = new Vector2Int(1, 1) },
        };

        // ---- Enemies ----
        data.enemies = new EnemySpawnEntry[]
        {
            new EnemySpawnEntry { type = EnemyType.Wolf,      position = new Vector2Int( 2, 18) },
            new EnemySpawnEntry { type = EnemyType.Wasp,      position = new Vector2Int(10,  2) },
            new EnemySpawnEntry { type = EnemyType.Bat,       position = new Vector2Int(38,  2) },
            new EnemySpawnEntry { type = EnemyType.Ogre,      position = new Vector2Int(45, 30) },
            new EnemySpawnEntry { type = EnemyType.Zombie,    position = new Vector2Int( 2, 30) },
            new EnemySpawnEntry { type = EnemyType.Giant,     position = new Vector2Int(24,  2) },
            new EnemySpawnEntry { type = EnemyType.BloodWasp, position = new Vector2Int(44, 33) },
        };

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"EnemyTestLayout generated at {path}", "OK");
        Selection.activeObject = data;
    }

    // ---- Helpers ----
    private static void Set(MapLayoutData d, int col, int row, TileType t)
    {
        if (col < 0 || col >= COLS || row < 0 || row >= ROWS) return;
        d.terrain[row * COLS + col] = t;
    }

    private static void SetRow(MapLayoutData d, int row, int colStart, int colEnd, TileType t)
    {
        for (int c = colStart; c <= colEnd; c++) Set(d, c, row, t);
    }

    private static void SetCol(MapLayoutData d, int col, int rowStart, int rowEnd, TileType t)
    {
        for (int r = rowStart; r <= rowEnd; r++) Set(d, col, r, t);
    }

    private static void SetRect(MapLayoutData d, int col, int row, int w, int h, TileType t)
    {
        for (int r = row; r < row + h; r++)
        for (int c = col; c < col + w; c++)
            Set(d, c, r, t);
    }
}
#endif
