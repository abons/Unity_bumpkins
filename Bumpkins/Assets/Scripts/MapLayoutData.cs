using UnityEngine;
using System;

public enum EnemyType { Wolf, Wasp, Bat, Ogre, Zombie, Giant, BloodWasp }

[Serializable]
public class EnemySpawnEntry
{
    public EnemyType type;
    [Tooltip("Tile position (col, row)")]
    public Vector2Int position;
}

public enum BumpkinSpawnType { Male, Female, Boy, Girl }

[Serializable]
public class BumpkinSpawnEntry
{
    public BumpkinSpawnType type;
}

/// <summary>
/// One building entry in the map layout.
/// </summary>
[Serializable]
public class BuildingEntry
{
    public BuildingType type;
    [Tooltip("Bottom-left tile position (col, row)")]
    public Vector2Int position;   // (col, row), origin = bottom-left
    public Vector2Int size;       // (cols, rows)
}

/// <summary>
/// Full layout for one map.
/// Create via: Assets > Create > Bumpkins > MapLayoutData
/// </summary>
[CreateAssetMenu(menuName = "Bumpkins/MapLayoutData", fileName = "Map1Layout")]
public class MapLayoutData : ScriptableObject
{
    [Header("Display")]
    [Tooltip("Label shown on the map-switch button")]
    public string displayName;

    [Header("Grid dimensions")]
    public int cols = 24;
    public int rows = 18;

    [Header("Isometric tile size (world units)")]
    [Tooltip("Half-width of one diamond tile")]
    public float isoHalfW = 0.5f;   // original sprite: 78px wide → half = 39
    [Tooltip("Half-height of one diamond tile")]
    public float isoHalfH = 0.256f; // original sprite: 40px high → half = 20 → ratio 20/39 ≈ 0.513 → scaled to 0.5 half-w

    [Header("Terrain — flat array [row * cols + col], row 0 = bottom")]
    [Tooltip("Length must equal cols × rows")]
    public TileType[] terrain;

    [Header("Buildings")]
    public BuildingEntry[] buildings;

    [Header("Enemies")]
    public EnemySpawnEntry[] enemies;

    [Header("Starting Bumpkins")]
    [Tooltip("Leave empty to use the default 1 male + 1 female")]
    public BumpkinSpawnEntry[] startingBumpkins;

    [Header("Starting Gold")]
    [Tooltip("0 = use GameConfig.startGold")]
    public int startGold = 0;

    /// <summary>Get terrain tile at (col, row).</summary>
    public TileType GetTile(int col, int row)
    {
        if (col < 0 || col >= cols || row < 0 || row >= rows) return TileType.Grass;
        return terrain[row * cols + col];
    }

    /// <summary>Isometric world position of tile center at (col, row).</summary>
    public Vector3 TileToWorld(int col, int row, float z = 0f)
    {
        float x = (col - row) * isoHalfW;
        float y = (col + row) * isoHalfH;
        return new Vector3(x, y, z);
    }

    /// <summary>Isometric sort order: tiles further back render first.</summary>
    public int SortOrder(int col, int row) => -(col + row);

    /// <summary>World position for a building footprint bottom-left at (col,row), size (w,h).</summary>
    public Vector3 BuildingToWorld(int col, int row, int w, int h, float z = -0.1f)
    {
        // center of footprint = average of occupied tile centers
        float cx = col + (w - 1) * 0.5f;
        float cy = row + (h - 1) * 0.5f;
        float x = (cx - cy) * isoHalfW;
        float y = (cx + cy) * isoHalfH;
        return new Vector3(x, y, z);
    }

    /// <summary>Sort order for a building at bottom-left (col,row) size (w,h).
    /// +1 offset: boven eigen tegel, maar achter tegels die visueel ervoor liggen (lagere col+row).</summary>
    public int BuildingSortOrder(int col, int row, int w, int h)
        => -(col + row) + 1;

#if UNITY_EDITOR
    [ContextMenu("Reset terrain to Grass")]
    private void ResetTerrain()
    {
        terrain = new TileType[cols * rows];
        UnityEditor.EditorUtility.SetDirty(this);
    }
#endif
}
