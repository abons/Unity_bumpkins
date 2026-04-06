using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Multi-stage construction: Blueprint → Resources → Roofing → Done.
/// Attach to the root GO created by BuildManager.  Start() will find the
/// SpriteRenderer child and manage WorkCells around the footprint.
/// </summary>
public class ConstructionSite : MonoBehaviour
{
    public enum Stage { Resources, Roofing, Done }

    [Header("Config")]
    public BuildingType buildingType;
    public float workDuration    = 5f;   // seconds per worker trip
    public int   maxWorkers       = 2;

    public Stage CurrentStage { get; private set; }
    public bool  CanBeWorked  => CurrentStage != Stage.Done
                              && _currentWorkers < maxWorkers
                              && GetFreeCell() != null;

    private int _currentWorkers;
    private SpriteRenderer       _buildingSr;
    private List<WorkCell>       _workCells  = new List<WorkCell>();
    private Sprite _pickSprite, _sawSprite, _vrockSprite, _vsawSprite, _bricksSprite, _planksSprite;

    // ---- Unity ----

    void Start()
    {
        _buildingSr = GetComponentInChildren<SpriteRenderer>();

        // Blueprint tint: light blue, semi-transparent
        if (_buildingSr != null)
            _buildingSr.color = new Color(0.65f, 0.85f, 1f, 0.55f);

        CurrentStage = Stage.Resources;

        // Auto-assign idle males
        StartCoroutine(AutoAssignNextFrame());
    }

    // Auto-assign one frame later so all Start()s have run
    private IEnumerator AutoAssignNextFrame()
    {
        yield return null;
        TryAutoAssignWorkers();
    }

    // ---- Worker reservation ----

    /// <summary>
    /// Try to assign a male bumpkin to a free workcell.
    /// Returns the reserved WorkCell, or null if none available.
    /// </summary>
    public WorkCell TryReserveWorker(BumpkinController b)
    {
        if (!b.IsMale)    return null;
        if (!CanBeWorked) return null;
        var cell = GetFreeCell();
        if (cell == null) return null;
        cell.TryOccupy();
        _currentWorkers++;
        return cell;
    }

    /// <summary>Called by bumpkin after finishing one work trip.</summary>
    public void DeliverWork(WorkCell cell)
    {
        _currentWorkers = Mathf.Max(0, _currentWorkers - 1);
        cell?.MarkDone();

        if (AllCellsDone())
            Complete();
        else
            TryAutoAssignWorkers();
    }

    private bool AllCellsDone()
    {
        if (_workCells.Count == 0) return false;
        foreach (var c in _workCells)
            if (c != null && !c.IsDone) return false;
        return true;
    }

    // ---- Stage transitions ----

    private void Complete()
    {
        CurrentStage = Stage.Done;

        if (_buildingSr != null)
            _buildingSr.color = Color.white;

        // Dissolve all workcells — construction is finished
        foreach (var cell in _workCells)
            if (cell != null) cell.Dissolve();
        _workCells.Clear();

        ActivateBuilding();
        Debug.Log($"[Construction] {buildingType} aan {name}: KLAAR!");
    }

    // ---- Activate final building components ----

    private void ActivateBuilding()
    {
        switch (buildingType)
        {
            case BuildingType.House:
                var tag = gameObject.AddComponent<BuildingTag>();
                tag.isHouse = true;
                SpawnBumpkin();
                break;

            case BuildingType.Toolshed:
                var tag2 = gameObject.AddComponent<BuildingTag>();
                tag2.isHouse = false;
                GameManager.Instance?.UnlockMill();
                break;

            case BuildingType.Mill:
                _buildingSr?.gameObject.AddComponent<MillAnimator>();
                var millDrop = gameObject.AddComponent<DropOffNode>();
                millDrop.dropOffType = DropOffNode.DropOffType.Bakery;
                GameManager.Instance?.UnlockDairy();
                break;

            case BuildingType.ChickenCoop:
                SpawnChicken();
                break;
        }
    }

    private void SpawnBumpkin()
    {
        bool male   = Random.value > 0.5f;
        string sprN = male ? "m_still" : "f_still";
        var go = new GameObject(male ? "Bumpkin_Male" : "Bumpkin_Female");
        go.transform.position   = transform.position + new Vector3(0.35f, -0.25f, 0f);
        go.transform.localScale = Vector3.one;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/{sprN}");
        sr.sortingOrder = 10;

        go.AddComponent<CircleCollider2D>().radius = 0.3f;

        var bc = go.AddComponent<BumpkinController>();
        bc.bumpkinType = male ? BumpkinController.BumpkinType.Male : BumpkinController.BumpkinType.Female;

        go.AddComponent<BumpkinClick>();
        go.AddComponent<BumpkinAnimator>();
    }

    private void SpawnChicken()
    {
        var go = new GameObject("Chicken");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var csr   = go.AddComponent<SpriteRenderer>();
        var csp   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/Chicken");
        float bw  = _buildingSr != null ? _buildingSr.bounds.size.x : 0.5f;
        if (csp != null)
        {
            csr.sprite = csp;
            float sc   = bw * 0.075f / csp.bounds.size.x;
            go.transform.localScale = new Vector3(sc, sc, 1f);
        }
        csr.sortingOrder = (_buildingSr != null ? _buildingSr.sortingOrder : 10) + 1;
        go.AddComponent<ChickenAnimator>();
    }

    // ---- WorkCells ----

    private WorkCell GetFreeCell()
    {
        foreach (var c in _workCells)
            if (c != null && !c.IsOccupied && !c.IsDone) return c;
        return null;
    }

    /// <summary>
    /// Called by BuildManager immediately after placement.
    /// Spawns 7 workcells: 4 corners (pick icon) + 3 sides (saw icon, NW/NE/SE).
    /// The SW face is the door side — no cell there.
    /// </summary>
    public void InitWorkCells(Vector2Int gridPos, MapLayoutData layout)
    {
        if (layout == null) return;

        _pickSprite   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/pick");
        _sawSprite    = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/saw");
        _vrockSprite  = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/vpick");
        _vsawSprite   = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/vsaw");
        _bricksSprite = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/bricks");
        _planksSprite = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Buildings/planks");

        int col = gridPos.x;
        int row = gridPos.y;
        int w = 2, h = 2;  // default (Toolshed, etc.)
        switch (buildingType)
        {
            case BuildingType.House:  w = 3; h = 3; break;
            case BuildingType.Mill:   w = 3; h = 2; break;
            case BuildingType.Dairy:  w = 3; h = 3; break;
        }

        // Each workcell sits on a specific integer tile (c, r) within the footprint.
        // World position = centre of that iso tile; sort order is per-tile so cells
        // render above their own terrain tile (+1) and above the building sprite (+2).
        void Spawn(int c, int r, WorkCell.CellKind kind)
        {
            var worldPos = new Vector3((c - r) * layout.isoHalfW, (c + r) * layout.isoHalfH, -0.05f);
            var go   = new GameObject($"WorkCell_{_workCells.Count}");
            go.transform.SetParent(transform.parent);
            go.transform.position = worldPos;
            var cell = go.AddComponent<WorkCell>();
            Sprite idle   = kind == WorkCell.CellKind.Corner ? _pickSprite   : _sawSprite;
            Sprite active = kind == WorkCell.CellKind.Corner ? _vrockSprite  : _vsawSprite;
            Sprite done   = kind == WorkCell.CellKind.Corner ? _bricksSprite : _planksSprite;
            var cellSize  = new Vector2(layout.isoHalfW * 2f, layout.isoHalfH * 2f);
            cell.Init(kind, idle, active, done, -(c + r) + 2, cellSize);
            _workCells.Add(cell);
        }

        // 4 corners — one per corner tile of the footprint (always integer coords)
        Spawn(col,         row,         WorkCell.CellKind.Corner);
        Spawn(col + w - 1, row,         WorkCell.CellKind.Corner);
        Spawn(col + w - 1, row + h - 1, WorkCell.CellKind.Corner);
        Spawn(col,         row + h - 1, WorkCell.CellKind.Corner);

        // Side midpoints: only when the dimension is ≥ 3 so the centre column/row
        // is an integer tile coordinate.  For w=2 or h=2 the midpoint would fall
        // between two tiles (fractional coord), so no side cell is added there.
        if (w >= 3)
        {
            int cMid = col + (w - 1) / 2;
            Spawn(cMid, row,         WorkCell.CellKind.Side);  // NW face mid
            Spawn(cMid, row + h - 1, WorkCell.CellKind.Side);  // SE face mid
        }
        if (h >= 3)
        {
            int rMid = row + (h - 1) / 2;
            Spawn(col + w - 1, rMid, WorkCell.CellKind.Side);  // NE face mid
        }
    }

    // ---- Auto-assign ----

    private void TryAutoAssignWorkers()
    {
        if (!CanBeWorked) return;
        var bumpkins = FindObjectsByType<BumpkinController>(FindObjectsSortMode.None);
        foreach (var b in bumpkins)
        {
            if (!CanBeWorked) break;
            if (!b.IsMale)   continue;
            if (b.CurrentState != "Idle") continue;
            var cell = TryReserveWorker(b);
            if (cell != null)
                b.AssignToConstruction(this, cell);
        }
    }
}
