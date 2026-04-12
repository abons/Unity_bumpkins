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

        // ---- Door-exit road tiles only (no road spine) ----
        Set(data,  7,  2, TileType.Road); // Woodpile    door
        Set(data,  5,  4, TileType.Road); // Toolshed    door (col, row-1)
        Set(data,  1,  5, TileType.Road); // House       door
        Set(data,  8,  5, TileType.Road); // Dairy       door
        Set(data,  2,  8, TileType.Road); // Mill        door (col+1, row-1)

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
            new BuildingEntry { type = BuildingType.WheatField,  position = new Vector2Int( 1,  2), size = new Vector2Int(1, 1) },
            new BuildingEntry { type = BuildingType.Rockpile,    position = new Vector2Int( 5,  2), size = new Vector2Int(2, 2) },
            new BuildingEntry { type = BuildingType.Woodpile,    position = new Vector2Int( 8,  2), size = new Vector2Int(2, 2) },

            // ── Rows 5–7 — 3×3 buildings ─────────────────────────────────────
            new BuildingEntry { type = BuildingType.House,       position = new Vector2Int( 2,  5), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Toolshed,    position = new Vector2Int( 5,  5), size = new Vector2Int(3, 3) },
            new BuildingEntry { type = BuildingType.Dairy,       position = new Vector2Int( 9,  5), size = new Vector2Int(3, 3) },

            // ── Rows 8–10 — 4×3 buildings ────────────────────────────────────
            new BuildingEntry { type = BuildingType.Mill,        position = new Vector2Int( 1,  9), size = new Vector2Int(4, 3) },
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

/// <summary>
/// Play-mode debug window for DayNightCycle.
/// Open via Tools > Bumpkins > Day-Night Debug.
/// </summary>
public class DayNightDebugWindow : EditorWindow
{
    float _speedMultiplier = 1f;
    bool  _paused          = false;
    bool  _allDoorsOpen    = false;

    [MenuItem("Tools/Bumpkins/Day-Night Debug")]
    static void Open() => GetWindow<DayNightDebugWindow>("Day-Night Debug");

    void OnEnable()  => EditorApplication.update += Repaint;
    void OnDisable() => EditorApplication.update -= Repaint;

    void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to use this debug window.", MessageType.Info);
            return;
        }

        var cycle = DayNightCycle.Instance;
        if (cycle == null)
        {
            EditorGUILayout.HelpBox("DayNightCycle not found in scene.", MessageType.Warning);
            return;
        }

        // ── State readout ──────────────────────────────────────────────
        GUILayout.Label("State", EditorStyles.boldLabel);

        string phase = cycle.IsNight ? "NIGHT" : "DAY";
        GUI.color = cycle.IsNight ? new Color(0.5f, 0.65f, 1f) : new Color(1f, 0.95f, 0.5f);
        GUILayout.Label($"  Phase  : {phase}", EditorStyles.largeLabel);
        GUI.color = Color.white;
        GUILayout.Label($"  Season : {cycle.CurrentSeason}");

        Rect progressRect = GUILayoutUtility.GetRect(0, 20, GUILayout.ExpandWidth(true));
        EditorGUI.ProgressBar(progressRect, cycle.NightBlend, $"Night blend: {cycle.NightBlend:F2}");

        EditorGUILayout.Space(8);

        // ── Speed control ──────────────────────────────────────────────
        GUILayout.Label("Speed", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        _speedMultiplier = EditorGUILayout.Slider("Multiplier", _speedMultiplier, 0.1f, 50f);
        if (GUILayout.Button("1×", GUILayout.Width(32))) _speedMultiplier = 1f;
        if (GUILayout.Button("10×", GUILayout.Width(36))) _speedMultiplier = 10f;
        if (GUILayout.Button("30×", GUILayout.Width(36))) _speedMultiplier = 30f;

        EditorGUILayout.EndHorizontal();
        cycle.DebugSpeedMultiplier = _speedMultiplier;

        EditorGUILayout.Space(8);

        // ── On / Off ───────────────────────────────────────────────────
        GUILayout.Label("Cycle", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();

        bool shouldBePaused = GUILayout.Toggle(_paused, _paused ? "Paused" : "Running",
            "Button", GUILayout.Height(28));
        if (shouldBePaused != _paused)
        {
            _paused = shouldBePaused;
            cycle.DebugPaused = _paused;
        }

        if (GUILayout.Button("Jump to Noon",     GUILayout.Height(28))) cycle.DebugJumpToDay();
        if (GUILayout.Button("Jump to Midnight", GUILayout.Height(28))) cycle.DebugJumpToNight();
        if (GUILayout.Button("Next Season",      GUILayout.Height(28))) cycle.DebugNextSeason();

        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ── Doors ──────────────────────────────────────────────────────
        GUILayout.Label("Doors", EditorStyles.boldLabel);
        bool newDoorsOpen = GUILayout.Toggle(_allDoorsOpen, _allDoorsOpen ? "All Doors: OPEN" : "All Doors: CLOSED",
            "Button", GUILayout.Height(28));
        if (newDoorsOpen != _allDoorsOpen)
        {
            _allDoorsOpen = newDoorsOpen;
            if (_allDoorsOpen)
            {
                foreach (var a in Object.FindObjectsByType<HouseAnimator>   (FindObjectsSortMode.None)) a.OpenDoor();
                foreach (var a in Object.FindObjectsByType<MillAnimator>    (FindObjectsSortMode.None)) a.OpenDoor();
                foreach (var a in Object.FindObjectsByType<DairyAnimator>   (FindObjectsSortMode.None)) a.OpenDoor();
                foreach (var a in Object.FindObjectsByType<ToolshedAnimator>(FindObjectsSortMode.None)) a.OpenDoor();
            }
            else
            {
                foreach (var a in Object.FindObjectsByType<HouseAnimator>   (FindObjectsSortMode.None)) a.CloseDoor();
                foreach (var a in Object.FindObjectsByType<MillAnimator>    (FindObjectsSortMode.None)) a.CloseDoor();
                foreach (var a in Object.FindObjectsByType<DairyAnimator>   (FindObjectsSortMode.None)) a.CloseDoor();
                foreach (var a in Object.FindObjectsByType<ToolshedAnimator>(FindObjectsSortMode.None)) a.CloseDoor();
            }
        }

        EditorGUILayout.Space(8);

        // ── Fall Grass Color ───────────────────────────────────────────
        GUILayout.Label("Fall Grass Tint  (blends toward target colour)", EditorStyles.boldLabel);

        // Presets
        GUILayout.Label("Presets:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Jouw kleur\n#384100", GUILayout.Height(36))) SetFall(0.22f, 0.25f, 0.00f, 0.65f);
        if (GUILayout.Button("Olijf\n#4A5500",      GUILayout.Height(36))) SetFall(0.29f, 0.33f, 0.00f, 0.60f);
        if (GUILayout.Button("Droog geel\n#6B6200",  GUILayout.Height(36))) SetFall(0.42f, 0.38f, 0.00f, 0.55f);
        if (GUILayout.Button("Laat zomer\n#2D4200",  GUILayout.Height(36))) SetFall(0.18f, 0.26f, 0.00f, 0.40f);
        if (GUILayout.Button("Geen tint",            GUILayout.Height(36))) SetFall(0f, 0f, 0f, 0f);
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(4);
        EditorGUILayout.HelpBox("RGB = doelkleur. Alpha = blendsterkte (0 = origineel gras, 1 = puur doelkleur).", MessageType.None);
        EditorGUI.BeginChangeCheck();
        _fallR = EditorGUILayout.Slider("R",              _fallR, 0f, 1f);
        _fallG = EditorGUILayout.Slider("G",              _fallG, 0f, 1f);
        _fallB = EditorGUILayout.Slider("B",              _fallB, 0f, 1f);
        _fallA = EditorGUILayout.Slider("Blend sterkte",  _fallA, 0f, 1f);

        // Preview swatch: lerp from grass green toward target
        var swatchRect = GUILayoutUtility.GetRect(0, 28, GUILayout.ExpandWidth(true));
        var grassGreen = new Color(0.1f, 0.47f, 0.08f);
        var target     = new Color(_fallR, _fallG, _fallB);
        var previewCol = Color.Lerp(grassGreen, target, _fallA);
        EditorGUI.DrawRect(swatchRect, previewCol);
        var labelStyle = new GUIStyle(EditorStyles.label) { normal = { textColor = Color.white } };
        GUI.Label(new Rect(swatchRect.x + 4, swatchRect.y + 4, swatchRect.width, 20), "preview op gras", labelStyle);

        if (EditorGUI.EndChangeCheck() || GUILayout.Button("Toepassen op scene tiles", GUILayout.Height(28)))
            ApplyFallColor(new Color(_fallR, _fallG, _fallB, _fallA));

        if (GUILayout.Button("Log waarden voor GridMapBuilder", GUILayout.Height(24)))
        {
            string line = string.Format(System.Globalization.CultureInfo.InvariantCulture,
                "seasonal.FallAddColor = new Color({0:F2}f, {1:F2}f, {2:F2}f, {3:F2}f);",
                _fallR, _fallG, _fallB, _fallA);
            Debug.Log(line);
        }
    }

    void SetFall(float r, float g, float b, float a)
    {
        _fallR = r; _fallG = g; _fallB = b; _fallA = a;
        ApplyFallColor(new Color(r, g, b, a));
    }

    float _fallR = 0.22f;
    float _fallG = 0.25f;
    float _fallB = 0.00f;
    float _fallA = 0.65f;

    static void ApplyFallColor(Color c)
    {
        foreach (var t in Object.FindObjectsByType<SeasonalTreeTile>(FindObjectsSortMode.None))
            t.PreviewFallColor(c);
    }
}
#endif
