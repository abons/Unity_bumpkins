using UnityEngine;

/// <summary>
/// A single construction workcell placed around a ConstructionSite.
/// Corner cells display a pick icon; side cells display a saw icon.
/// The sprite swaps to an active variant (vrock / vsaw) while a worker occupies it.
/// </summary>
public class WorkCell : MonoBehaviour
{
    public enum CellKind { Corner, Side }

    public CellKind Kind       { get; private set; }
    public bool     IsOccupied { get; private set; }
    public bool     IsDone     { get; private set; }

    private SpriteRenderer _sr;
    private Sprite         _idleSprite;
    private Sprite         _activeSprite;
    private Sprite         _doneSprite;
    private Vector2        _cellSize;

    /// <summary>
    /// Finishes setup after Instantiate.
    /// idle/active/done may be null; a coloured fallback square is used if so.
    /// cellSize is the target world-space size (width x height) to fill — pass the
    /// iso cell dimensions so pick/saw match a 1x1 building cell visually.
    /// </summary>
    public void Init(CellKind kind, Sprite idle, Sprite active, Sprite done, int sortOrder, Vector2 cellSize)
    {
        Kind          = kind;
        _idleSprite   = idle;
        _activeSprite = active;
        _doneSprite   = done;
        _cellSize     = cellSize;

        var vis = new GameObject("Visual");
        vis.transform.SetParent(transform);
        vis.transform.localPosition = Vector3.zero;

        _sr              = vis.AddComponent<SpriteRenderer>();
        _sr.sortingOrder = sortOrder;

        if (idle != null)
        {
            _sr.sprite = idle;
            float sc = Mathf.Min(cellSize.x / idle.bounds.size.x, cellSize.y / idle.bounds.size.y);
            vis.transform.localScale = new Vector3(sc, sc, 1f);
        }
        else
        {
            _sr.sprite = MakeSolidSprite(FallbackColor(kind));
            vis.transform.localScale = new Vector3(cellSize.x * 0.9f, cellSize.y * 0.9f, 1f);
        }
    }

    /// <summary>Try to claim this cell. Returns false if already occupied or done.</summary>
    public bool TryOccupy()
    {
        if (IsOccupied || IsDone) return false;
        IsOccupied = true;
        ApplySprite(_activeSprite, Color.white);
        return true;
    }

    /// <summary>Release back to idle state after the worker finishes.</summary>
    public void Release()
    {
        IsOccupied = false;
        ApplySprite(_idleSprite, FallbackColor(Kind));
    }

    /// <summary>Mark this cell permanently done — hides the visual until a done sprite is available.</summary>
    public void MarkDone()
    {
        IsOccupied = false;
        IsDone     = true;
        if (_doneSprite != null)
            ApplySpriteScaled(_doneSprite, DoneColor(Kind));
        else if (_sr != null)
            _sr.enabled = false;
    }

    /// <summary>Destroy this cell when construction is complete.</summary>
    public void Dissolve() => Destroy(gameObject);

    // ---- Internal helpers ----

    private void ApplySprite(Sprite sp, Color fallback)
    {
        if (_sr == null) return;
        if (sp != null)
        {
            _sr.sprite = sp;
            _sr.color  = Color.white;
        }
        else
        {
            _sr.sprite = MakeSolidSprite(fallback);
            _sr.color  = Color.white;
        }
    }

    private static Color FallbackColor(CellKind kind) =>
        kind == CellKind.Corner
            ? new Color(0.9f, 0.55f, 0.1f, 0.9f)   // orange — pick / rock
            : new Color(0.2f, 0.65f, 0.95f, 0.9f);  // blue  — saw / wood

    private static Color DoneColor(CellKind kind) =>
        kind == CellKind.Corner
            ? new Color(0.65f, 0.35f, 0.20f, 1f)  // brick brown
            : new Color(0.85f, 0.70f, 0.40f, 1f); // plank tan

    // Like ApplySprite but also rescales the visual to fit _cellSize.
    private void ApplySpriteScaled(Sprite sp, Color fallback)
    {
        if (_sr == null) return;
        if (sp != null)
        {
            _sr.sprite = sp;
            _sr.color  = Color.white;
            float sc = Mathf.Min(_cellSize.x / sp.bounds.size.x, _cellSize.y / sp.bounds.size.y);
            _sr.transform.localScale = new Vector3(sc, sc, 1f);
        }
        else
        {
            _sr.sprite = MakeSolidSprite(fallback);
            _sr.color  = Color.white;
            _sr.transform.localScale = new Vector3(_cellSize.x * 0.9f, _cellSize.y * 0.9f, 1f);
        }
    }

    private static Sprite MakeSolidSprite(Color c)
    {
        var t = new Texture2D(4, 4) { filterMode = FilterMode.Point };
        var px = new Color[16];
        for (int i = 0; i < 16; i++) px[i] = c;
        t.SetPixels(px);
        t.Apply();
        return Sprite.Create(t, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f), 100f);
    }
}
