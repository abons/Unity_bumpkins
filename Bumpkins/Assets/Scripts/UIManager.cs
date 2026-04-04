using UnityEngine;

/// <summary>
/// HUD via OnGUI — geen Canvas, geen RectTransform, geen artefacten.
/// </summary>
public class UIManager : MonoBehaviour
{
    public static UIManager Instance { get; private set; }

    private GUIStyle _style;
    private GUIStyle _btnStyle;

    /// <summary>True als de muis dit frame over een GUI-element staat (voorkomt doorklikken).</summary>
    public static bool IsPointerOverGUI { get; private set; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void OnGUI()
    {
        if (GameManager.Instance == null) return;
        if (_style == null) BuildStyle();

        var gm = GameManager.Instance;

        int x = 10, y = 10, h = 26;
        GUI.Label(new Rect(x, y,       240, h), $"Gold:  {gm.Gold}",       _style);
        GUI.Label(new Rect(x, y + h,   240, h), $"Bread: {gm.Bread}",      _style);
        GUI.Label(new Rect(x, y + h*2, 240, h), $"Milk:  {gm.Milk}",       _style);
        GUI.Label(new Rect(x, y + h*3, 240, h), $"Eggs:  {gm.EggStock}",   _style);
        GUI.Label(new Rect(x, y + h*4, 240, h), $"Wheat: {gm.WheatStored}",_style);

        var sel = SelectionManager.Instance?.SelectedBumpkin;
        if (sel == null) { IsPointerOverGUI = false; return; }

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
        IsPointerOverGUI = btnRect.Contains(Event.current.mousePosition);
        if (GUI.Button(btnRect, label, _btnStyle))
            sel.freeWill = !sel.freeWill;
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
