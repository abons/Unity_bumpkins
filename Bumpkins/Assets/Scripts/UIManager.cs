using UnityEngine;

/// <summary>
/// HUD via OnGUI — geen Canvas, geen RectTransform, geen artefacten.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    [Header("Maps")]
    [Tooltip("All available map layouts. Shown as buttons at the top-center.")]
    public MapLayoutData[] maps;
    private int _currentMapIndex = 0;

    private GUIStyle _style;
    private GUIStyle _btnStyle;

    /// <summary>True als de muis dit frame over een GUI-element staat (voorkomt doorklikken).</summary>
    public static bool IsPointerOverGUI { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    private const float RefWidth  = 960f;
    private const float RefHeight = 540f;
    // Mouse position scaled to reference space — set once per OnGUI call
    private static Vector2 _mousePosRef;

    void OnGUI()
    {
        if (GameManager.Instance == null) return;
        if (_style == null) BuildStyle();

        // Scale UI to reference resolution
        float sx = Screen.width  / RefWidth;
        float sy = Screen.height / RefHeight;
        float s  = Mathf.Min(sx, sy);
        GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, new Vector3(s, s, 1f));
        _mousePosRef = Event.current.mousePosition / s;

        var gm = GameManager.Instance;
        bool overGUI = false;

        int x = 10, y = 10, h = 26;
        GUI.Label(new Rect(x, y,       240, h), $"Gold:  {gm.Gold}",       _style);
        GUI.Label(new Rect(x, y + h,   240, h), $"Bread: {gm.Bread}",      _style);
        GUI.Label(new Rect(x, y + h*2, 240, h), $"Milk:  {gm.Milk}",       _style);
        GUI.Label(new Rect(x, y + h*3, 240, h), $"Eggs:  {gm.EggStock}",   _style);
        GUI.Label(new Rect(x, y + h*4, 240, h), $"Wheat: {gm.WheatStored}",_style);

        // ---- Build menu ----
        overGUI |= DrawBuildMenu(gm);

        // ---- Map switch buttons ----
        overGUI |= DrawMapButtons();

        var sel = SelectionManager.Instance?.SelectedBumpkin;
        if (sel == null) { IsPointerOverGUI = overGUI; return; }

        // Naam + state onderin midden
        string info = $"{sel.name}  [{sel.CurrentState}]";
        GUI.Label(new Rect(RefWidth / 2f - 150, RefHeight - 32, 300, 26), info, _style);

        // Freewill toggle rechtsbovenin
        if (_btnStyle == null) BuildBtnStyle(sel.freeWill);
        _btnStyle.normal.textColor  = sel.freeWill ? Color.green : Color.red;
        _btnStyle.focused.textColor = _btnStyle.normal.textColor;
        _btnStyle.hover.textColor   = _btnStyle.normal.textColor;
        string label = sel.freeWill ? "Freewill: AAN" : "Freewill: UIT";
        var btnRect = new Rect(RefWidth - 160, 10, 150, 32);
        overGUI |= btnRect.Contains(_mousePosRef);
        if (GUI.Button(btnRect, label, _btnStyle))
            sel.freeWill = !sel.freeWill;

        IsPointerOverGUI = overGUI;
    }

    private bool DrawMapButtons()
    {
        if (maps == null || maps.Length <= 1) return false;
        var builder = FindFirstObjectByType<GridMapBuilder>();
        if (builder == null) return false;

        if (_btnStyle == null) BuildBtnStyle(true);
        bool anyOver = false;
        int bw = 140, bh = 30, gap = 4;
        int x = (int)(RefWidth / 2f) - (maps.Length * (bw + gap)) / 2;
        int y = 10;

        for (int i = 0; i < maps.Length; i++)
        {
            if (maps[i] == null) continue;
            var r = new Rect(x + i * (bw + gap), y, bw, bh);
            anyOver |= r.Contains(_mousePosRef);
            bool active = i == _currentMapIndex;
            _btnStyle.normal.textColor  = active ? Color.yellow : Color.white;
            _btnStyle.focused.textColor = _btnStyle.normal.textColor;
            _btnStyle.hover.textColor   = _btnStyle.normal.textColor;
            string label = string.IsNullOrEmpty(maps[i].displayName) ? maps[i].name : maps[i].displayName;
            if (GUI.Button(r, label, _btnStyle) && !active)
            {
                _currentMapIndex = i;
                builder.LoadMap(maps[i]);
            }
        }
        return anyOver;
    }

    private bool DrawBuildMenu(GameManager gm)
    {
        var bm = BuildManager.Instance;
        if (bm == null) return false;

        var cfg  = gm.config;
        if (_btnStyle == null) BuildBtnStyle(true);

        int bx = 10, by = 10 + 26 * 5 + 8, bw = 180, bh = 34, gap = 4;
        var mousePos = _mousePosRef;
        bool anyOver = false;

        // Helper: draw one build button
        bool DrawBuildBtn(string text, BuildingType type, bool canAfford, int index)
        {
            var r    = new Rect(bx, by + index * (bh + gap), bw, bh);
            anyOver |= r.Contains(mousePos);

            bool active = bm.InBuildMode && bm.SelectedType == type;
            Color fg    = active    ? Color.yellow
                        : canAfford ? Color.white
                        : new Color(0.5f, 0.5f, 0.5f);

            _btnStyle.normal.textColor  = fg;
            _btnStyle.focused.textColor = fg;
            _btnStyle.hover.textColor   = fg;

            bool clicked = GUI.Button(r, text, _btnStyle);
            return clicked;
        }

        if (DrawBuildBtn($"Huis        ({cfg.costHouse}g)",       BuildingType.House,       gm.Gold >= cfg.costHouse,       0))
        {
            if (bm.InBuildMode && bm.SelectedType == BuildingType.House) bm.ExitBuildMode();
            else bm.EnterBuildMode(BuildingType.House);
        }

        if (DrawBuildBtn($"Schuur      ({cfg.costToolshed}g)",     BuildingType.Toolshed,    gm.Gold >= cfg.costToolshed,    1))
        {
            if (bm.InBuildMode && bm.SelectedType == BuildingType.Toolshed) bm.ExitBuildMode();
            else bm.EnterBuildMode(BuildingType.Toolshed);
        }

        if (DrawBuildBtn($"Kippenhok   ({cfg.costChickenCoop}g)", BuildingType.ChickenCoop, gm.Gold >= cfg.costChickenCoop, 2))
        {
            if (bm.InBuildMode && bm.SelectedType == BuildingType.ChickenCoop) bm.ExitBuildMode();
            else bm.EnterBuildMode(BuildingType.ChickenCoop);
        }

        int btnCount = 3;

        if (gm.MillUnlocked)
        {
            if (DrawBuildBtn($"Molen       ({cfg.costMill}g)", BuildingType.Mill, gm.Gold >= cfg.costMill, btnCount))
            {
                if (bm.InBuildMode && bm.SelectedType == BuildingType.Mill) bm.ExitBuildMode();
                else bm.EnterBuildMode(BuildingType.Mill);
            }
            btnCount++;
        }

        if (gm.DairyUnlocked)
        {
            if (DrawBuildBtn($"Zuivel      ({cfg.costDairy}g)", BuildingType.Dairy, gm.Gold >= cfg.costDairy, btnCount))
            {
                if (bm.InBuildMode && bm.SelectedType == BuildingType.Dairy) bm.ExitBuildMode();
                else bm.EnterBuildMode(BuildingType.Dairy);
            }
            btnCount++;
        }

        // Status label wanneer in build mode
        if (bm.InBuildMode)
        {
            int ly = by + btnCount * (bh + gap) + 4;
            _style.normal.textColor = Color.yellow;
            GUI.Label(new Rect(bx, ly, 240, 22), "RMB / ESC = annuleren", _style);
            _style.normal.textColor = Color.white;
        }

        return anyOver;
    }

    private void BuildStyle()
    {
        _style                  = new GUIStyle(GUI.skin.label);
        _style.fontSize         = 18;
        _style.fontStyle        = FontStyle.Bold;
        _style.normal.textColor = Color.white;
    }

    private void BuildBtnStyle(bool on)
    {
        _btnStyle                  = new GUIStyle(GUI.skin.button);
        _btnStyle.fontSize         = 16;
        _btnStyle.fontStyle        = FontStyle.Bold;
        _btnStyle.normal.textColor = on ? Color.green : Color.red;
    }
}
