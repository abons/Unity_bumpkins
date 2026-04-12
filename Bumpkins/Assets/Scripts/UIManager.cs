using UnityEngine;
using UnityEngine.SceneManagement;

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
    private GUIStyle _pauseStyle;
    private GUIStyle _seasonLabelStyle;

    private Texture2D _seasonTex;

    // ---- Toolshed panel ----
    private BuildingTag _selectedToolshed;

    // ---- In-game menu overlay ----
    private bool _menuOpen;
    private GUIStyle _menuOverlayStyle;
    private GUIStyle _menuTitleStyle;
    private GUIStyle _menuBtnStyle;

    public void OpenToolshedPanel(BuildingTag tag)  { _selectedToolshed = tag; }
    public void CloseToolshedPanel()                { _selectedToolshed = null; }

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

        _mousePosRef = Event.current.mousePosition;

        var gm = GameManager.Instance;
        bool overGUI = false;

        int x = 10, y = 10, h = 26;
        GUI.Label(new Rect(x, y,       240, h), $"Gold:  {gm.Gold}",       _style);
        GUI.Label(new Rect(x, y + h,   240, h), $"Bread: {gm.Bread}",      _style);
        GUI.Label(new Rect(x, y + h*2, 240, h), $"Milk:  {gm.Milk}",       _style);
        GUI.Label(new Rect(x, y + h*3, 240, h), $"Eggs:  {gm.EggStock}",   _style);
        GUI.Label(new Rect(x, y + h*4, 240, h), $"Wheat: {gm.WheatStored}",_style);

        // ---- Menu button (bottom-left) ----
        var menuRect = new Rect(x, Screen.height - 36, 80, 26);
        overGUI |= menuRect.Contains(_mousePosRef);
        if (_btnStyle == null) BuildBtnStyle(true);
        Color prevColor = _btnStyle.normal.textColor;
        _btnStyle.normal.textColor  = Color.white;
        _btnStyle.focused.textColor = Color.white;
        _btnStyle.hover.textColor   = Color.white;
        if (GUI.Button(menuRect, "Menu", _btnStyle))
        {
            _menuOpen = true;
            Time.timeScale = 0f;
        }
        _btnStyle.normal.textColor  = prevColor;
        _btnStyle.focused.textColor = prevColor;
        _btnStyle.hover.textColor   = prevColor;

        // ---- In-game menu overlay ----
        if (_menuOpen)
        {
            overGUI = true;
            DrawMenuOverlay();
            IsPointerOverGUI = true;
            return;
        }

        // ---- Build menu ----
        overGUI |= DrawBuildMenu(gm);

        // ---- Toolshed panel ----
        if (_selectedToolshed != null)
            overGUI |= DrawToolshedPanel(gm);

        // ---- Map switch buttons ----
        overGUI |= DrawMapButtons();

        // ---- Season icon (top right) ----
        DrawSeasonIcon();

        // ---- Paused label ----
        if (Time.timeScale == 0f && !_menuOpen)
        {
            if (_pauseStyle == null)
            {
                _pauseStyle           = new GUIStyle(GUI.skin.label);
                _pauseStyle.fontSize  = 28;
                _pauseStyle.fontStyle = FontStyle.Bold;
                _pauseStyle.alignment = TextAnchor.UpperCenter;
                _pauseStyle.normal.textColor = Color.yellow;
            }
            GUI.Label(new Rect(0, 6, Screen.width, 40), "GEPAUZEERD", _pauseStyle);
        }

        var sel = SelectionManager.Instance?.SelectedBumpkin;
        if (sel == null) { IsPointerOverGUI = overGUI; return; }

        // Naam + state onderin midden
        string info = $"{sel.name}  [{sel.CurrentState}]";
        GUI.Label(new Rect(Screen.width / 2f - 150, Screen.height - 32, 300, 26), info, _style);

        // Freewill toggle rechtsbovenin
        if (_btnStyle == null) BuildBtnStyle(sel.freeWill);
        _btnStyle.normal.textColor  = sel.freeWill ? Color.green : Color.red;
        _btnStyle.focused.textColor = _btnStyle.normal.textColor;
        _btnStyle.hover.textColor   = _btnStyle.normal.textColor;
        string label = sel.freeWill ? "Freewill: AAN" : "Freewill: UIT";
        var btnRect = new Rect(Screen.width - 160, 10, 150, 32);
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
        int x = (int)(Screen.width / 2f) - (maps.Length * (bw + gap)) / 2;
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

        if (DrawBuildBtn($"Tarweveld   ({cfg.costWheatField}g)",   BuildingType.WheatField,  gm.Gold >= cfg.costWheatField,  3))
        {
            if (bm.InBuildMode && bm.SelectedType == BuildingType.WheatField) bm.ExitBuildMode();
            else bm.EnterBuildMode(BuildingType.WheatField);
        }

        int btnCount = 4;

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

    private void DrawSeasonIcon()
    {
        // Lazy-load texture
        if (_seasonTex == null)
            _seasonTex = Resources.Load<Texture2D>(GraphicsQuality.SpritePath + "/UI/season");
        if (_seasonTex == null) return;

        var dnc = DayNightCycle.Instance;
        Season season = dnc != null ? dnc.CurrentSeason : Season.Spring;

        // Tint per season
        Color tint = season switch
        {
            Season.Spring => new Color(0.6f, 1f, 0.6f),    // soft green
            Season.Summer => new Color(1f, 0.95f, 0.4f),   // warm yellow
            Season.Fall   => new Color(1f, 0.55f, 0.15f),  // orange
            Season.Winter => new Color(0.6f, 0.85f, 1f),   // icy blue
            _             => Color.white
        };

        string label = season switch
        {
            Season.Spring => "Spring",
            Season.Summer => "Summer",
            Season.Fall   => "Fall",
            Season.Winter => "Winter",
            _             => ""
        };

        float iconW = 56f, iconH = 44f;
        float ix = Screen.width - iconW - 10f;
        float iy = 10f;

        GUI.color = tint;
        GUI.DrawTexture(new Rect(ix, iy, iconW, iconH), _seasonTex, ScaleMode.ScaleToFit);
        GUI.color = Color.white;

        if (_seasonLabelStyle == null)
        {
            _seasonLabelStyle           = new GUIStyle(GUI.skin.label);
            _seasonLabelStyle.fontSize  = 14;
            _seasonLabelStyle.fontStyle = FontStyle.Bold;
            _seasonLabelStyle.alignment = TextAnchor.UpperCenter;
            _seasonLabelStyle.normal.textColor = Color.white;
        }
        GUI.Label(new Rect(ix - 10f, iy + iconH, iconW + 20f, 20f), label, _seasonLabelStyle);
    }

    private bool DrawToolshedPanel(GameManager gm)
    {
        const float pw = 270f, ph = 160f;
        float px = (Screen.width  - pw) * 0.5f;
        float py = (Screen.height - ph) * 0.5f;

        var panel = new Rect(px, py, pw, ph);
        bool anyOver = panel.Contains(_mousePosRef);

        GUI.Box(panel, "Schuur — Werknemers");

        if (_btnStyle == null) BuildBtnStyle(true);

        var sel = SelectionManager.Instance?.SelectedBumpkin;

        string bumpkinInfo = sel == null  ? "Geen bumpkin geselecteerd"
                           : sel.IsWorker ? $"{sel.name}  (Arbeider)"
                           : sel.IsMale   ? $"{sel.name}  (Man)"
                                          : $"{sel.name}  — selecteer een man";
        _style.normal.textColor = Color.white;
        GUI.Label(new Rect(px + 10f, py + 26f, pw - 20f, 22f), bumpkinInfo, _style);

        float bx = px + 10f, bw = pw - 20f, bh = 32f;

        // ---- Train Worker ----
        bool canTrain = sel != null && sel.IsMale && gm.Gold >= gm.config.costTrainWorker;
        Color trainCol = canTrain ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        _btnStyle.normal.textColor  = trainCol;
        _btnStyle.focused.textColor = trainCol;
        _btnStyle.hover.textColor   = trainCol;
        var trainRect = new Rect(bx, py + 56f, bw, bh);
        anyOver |= trainRect.Contains(_mousePosRef);
        if (GUI.Button(trainRect, $"Maak Arbeider  ({gm.config.costTrainWorker}g)", _btnStyle) && canTrain)
        {
            gm.Buy(gm.config.costTrainWorker, "Arbeider trainen");
            var toolshed = _selectedToolshed;
            _selectedToolshed = null;          // sluit panel direct
            var bumpkin = sel;
            bumpkin.StartToolshedConversion(toolshed, () =>
            {
                bumpkin.bumpkinType = BumpkinController.BumpkinType.Worker;
                bumpkin.GetComponent<BumpkinAnimator>()?.RefreshForWorker();
            });
        }

        // ---- Retrain Bumpkin ----
        bool canRetrain = sel != null && sel.IsWorker;
        Color retrainCol = canRetrain ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        _btnStyle.normal.textColor  = retrainCol;
        _btnStyle.focused.textColor = retrainCol;
        _btnStyle.hover.textColor   = retrainCol;
        var retrainRect = new Rect(bx, py + 94f, bw, bh);
        anyOver |= retrainRect.Contains(_mousePosRef);
        if (GUI.Button(retrainRect, "Heropleiden  (gratis)", _btnStyle) && canRetrain)
        {
            var toolshed = _selectedToolshed;
            _selectedToolshed = null;          // sluit panel direct
            var bumpkin = sel;
            bumpkin.StartToolshedConversion(toolshed, () =>
            {
                bumpkin.bumpkinType = BumpkinController.BumpkinType.Male;
                bumpkin.GetComponent<BumpkinAnimator>()?.RefreshForMale();
            });
        }

        // ---- Close ----
        _btnStyle.normal.textColor  = Color.white;
        _btnStyle.focused.textColor = Color.white;
        _btnStyle.hover.textColor   = Color.white;
        var closeRect = new Rect(px + pw - 26f, py + 2f, 24f, 22f);
        anyOver |= closeRect.Contains(_mousePosRef);
        if (GUI.Button(closeRect, "X", _btnStyle))
            _selectedToolshed = null;

        return anyOver;
    }

    private void DrawMenuOverlay()
    {
        // Dim background
        if (_menuOverlayStyle == null)
        {
            var tex = new Texture2D(1, 1);
            tex.SetPixel(0, 0, new Color(0, 0, 0, 0.7f));
            tex.Apply();
            _menuOverlayStyle = new GUIStyle();
            _menuOverlayStyle.normal.background = tex;
        }
        GUI.Box(new Rect(0, 0, Screen.width, Screen.height), GUIContent.none, _menuOverlayStyle);

        if (_menuTitleStyle == null)
        {
            _menuTitleStyle           = new GUIStyle(GUI.skin.label);
            _menuTitleStyle.fontSize  = 36;
            _menuTitleStyle.fontStyle = FontStyle.Bold;
            _menuTitleStyle.alignment = TextAnchor.MiddleCenter;
            _menuTitleStyle.normal.textColor = Color.white;
        }
        if (_menuBtnStyle == null)
        {
            _menuBtnStyle           = new GUIStyle(GUI.skin.button);
            _menuBtnStyle.fontSize  = 22;
            _menuBtnStyle.fontStyle = FontStyle.Bold;
            _menuBtnStyle.fixedHeight = 50;
        }

        float pw = Mathf.Min(380f, Screen.width * 0.85f);
        float px = (Screen.width - pw) / 2f;

        // 7 knoppen: bereken stride zodat alles past binnen 80% van de schermhoogte
        // btnY start op 20%, laatste knop eindigt voor 100%
        int   btnCount = 7;
        float available = Screen.height * 0.75f;
        float sp = available / btnCount;             // stride per knop (bh + gap)
        float bh = Mathf.Min(50f, sp * 0.82f);      // knophoogte = 82% van stride
        float btnY = Screen.height * 0.20f;

        int menuFontSize = Mathf.Max(12, Mathf.RoundToInt(bh * 0.44f));
        _menuBtnStyle.fontSize    = menuFontSize;
        _menuBtnStyle.fixedHeight = bh;
        _menuTitleStyle.fontSize  = Mathf.Max(16, Mathf.RoundToInt(Screen.height * 0.055f));

        GUI.Label(new Rect(0, Screen.height * 0.15f, Screen.width, 50), "Beasts & Bumpkins", _menuTitleStyle);

        // Doorgaan
        _menuBtnStyle.normal.textColor = Color.green;
        if (GUI.Button(new Rect(px, btnY, pw, 50), "Doorgaan", _menuBtnStyle))
        {
            _menuOpen = false;
            Time.timeScale = 1f;
        }

        _menuBtnStyle.normal.textColor = Color.white;

        // Opslaan / Laden
        if (GUI.Button(new Rect(px, btnY + sp * 1f, pw, 50), "Opslaan", _menuBtnStyle))
        {
            SaveSystem.Save();
            _menuOpen = false;
            Time.timeScale = 1f;
        }

        bool hasSave = SaveSystem.HasSave;
        _menuBtnStyle.normal.textColor = hasSave ? Color.white : new Color(0.5f, 0.5f, 0.5f);
        if (GUI.Button(new Rect(px, btnY + sp * 2f, pw, 50), "Laden", _menuBtnStyle) && hasSave)
        {
            Time.timeScale = 1f;
            SaveSystem.Load();   // reloads scene with pending data
        }
        _menuBtnStyle.normal.textColor = Color.white;

        // Map buttons
        string[] names   = { "Mission1Layout", "Map2Layout", "EnemyTestLayout" };
        string[] labels  = { "Mission 1",      "Map 2",      "Enemy Test" };
        var builder = FindFirstObjectByType<GridMapBuilder>();
        for (int i = 0; i < names.Length; i++)
        {
            if (GUI.Button(new Rect(px, btnY + sp * (i + 3), pw, 50), labels[i], _menuBtnStyle))
            {
                _menuOpen = false;
                Time.timeScale = 1f;
                MapSelection.Select(names[i]);
                SceneManager.LoadScene("Game");
            }
        }

        // Quit
        _menuBtnStyle.normal.textColor = new Color(1f, 0.4f, 0.4f);
        if (GUI.Button(new Rect(px, btnY + sp * 6.5f, pw, 50), "Afsluiten", _menuBtnStyle))
            Application.Quit();
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
