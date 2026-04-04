using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

/// <summary>
/// Editor-only utility to generate the Map1Layout ScriptableObject
/// from the ChatGPT Vision AI analysis of Beasts & Bumpkins Map 1.
///
/// Usage: Tools > Bumpkins > Generate Map1 Layout
/// </summary>
public static class Map1LayoutGenerator
{
    // Grid from analysis: 24 cols × 18 rows, origin bottom-left
    private const int COLS = 24;
    private const int ROWS = 18;

    [MenuItem("Tools/Bumpkins/Generate Map1 Layout")]
    public static void Generate()
    {
        // Find or create asset
        const string path = "Assets/Scripts/Map1Layout.asset";
        var data = AssetDatabase.LoadAssetAtPath<MapLayoutData>(path);
        if (data == null)
        {
            data = ScriptableObject.CreateInstance<MapLayoutData>();
            AssetDatabase.CreateAsset(data, path);
        }

        data.cols = COLS;
        data.rows = ROWS;

        // ---- Terrain ----
        // Start with all grass
        data.terrain = new TileType[COLS * ROWS];

        // Roads (from ASCII analysis)
        // Horizontal road: row 12, cols 12-19
        SetRow(data, row: 12, colStart: 12, colEnd: 19, TileType.Road);
        // Horizontal road: row 9, cols 12-19
        SetRow(data, row: 9,  colStart: 12, colEnd: 19, TileType.Road);
        // Vertical road: col 19, rows 9-12
        SetCol(data, col: 19, rowStart: 9, rowEnd: 12, TileType.Road);

        // Rock terrain
        SetRect(data, col: 4, row: 5, w: 2, h: 2, TileType.Rock);

        // ---- Buildings ----
        //  Farm = enkel drop-off gebouw voor tarwe én melk (Farm + Dairy samengevoegd)
        data.buildings = new BuildingEntry[]
        {
            new BuildingEntry { type = BuildingType.Mill,        position = new Vector2Int(12, 13), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Farm,        position = new Vector2Int( 7,  7), size = new Vector2Int(2, 2) }, // drop-off
            new BuildingEntry { type = BuildingType.WheatField,  position = new Vector2Int(10,  7), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.WheatField,  position = new Vector2Int(13,  7), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.CowPen,      position = new Vector2Int(14,  1), size = new Vector2Int(4, 3) },
            new BuildingEntry { type = BuildingType.ChickenCoop, position = new Vector2Int(17,  9), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.ChickenCoop, position = new Vector2Int(20,  9), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.ChickenCoop, position = new Vector2Int(17, 11), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.House,       position = new Vector2Int( 8, 13), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.House,       position = new Vector2Int( 6, 13), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Toolshed,    position = new Vector2Int( 9,  5), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Rockpile,    position = new Vector2Int( 4,  5), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Woodpile,    position = new Vector2Int(18,  5), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Campfire,    position = new Vector2Int(13, 11), size = new Vector2Int(1, 1) },
        };

        EditorUtility.SetDirty(data);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("Done", $"Map1Layout generated at {path}", "OK");
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
