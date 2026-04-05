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
    // Grid: 48 cols × 36 rows — doubled to push enemies further from center
    private const int COLS = 48;
    private const int ROWS = 36;

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

        // Central crossroads at map center (24, 18) — 4 arms for buildings to connect to
        // Horizontal: row 18, cols 22-26
        SetRow(data, row: 18, colStart: 22, colEnd: 26, TileType.Road);
        // Vertical: col 24, rows 16-20
        SetCol(data, col: 24, rowStart: 16, rowEnd:  20, TileType.Road);

        // Rock terrain (proportionally scaled from original)
        SetRect(data, col: 8, row: 10, w: 2, h: 2, TileType.Rock);

        // Shore border — 1 tile of sandhill around the island edge
        SetRect(data, col:  0, row:  0, w: COLS, h: 1,    TileType.Water); // south
        SetRect(data, col:  0, row: 35, w: COLS, h: 1,    TileType.Water); // north
        SetRect(data, col:  0, row:  0, w: 1,    h: ROWS, TileType.Water); // west
        SetRect(data, col: 47, row:  0, w: 1,    h: ROWS, TileType.Water); // east

        // ---- Buildings ----
        //  Farm = enkel drop-off gebouw voor tarwe én melk (Farm + Dairy samengevoegd)
        data.buildings = new BuildingEntry[]
        {
            new BuildingEntry { type = BuildingType.Cow,          position = new Vector2Int(27, 16), size = new Vector2Int(4, 3) },
            new BuildingEntry { type = BuildingType.Rockpile,    position = new Vector2Int( 8, 10), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Woodpile,    position = new Vector2Int(36, 10), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Campfire,    position = new Vector2Int(25, 22), size = new Vector2Int(1, 1) },
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
