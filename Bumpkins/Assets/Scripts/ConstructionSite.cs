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
        sr.sprite       = Resources.Load<Sprite>($"Sprites/Units/{sprN}");
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
        var csp   = Resources.Load<Sprite>("Sprites/Units/Chicken");
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

        _pickSprite   = Resources.Load<Sprite>("Sprites/Buildings/pick");
        _sawSprite    = Resources.Load<Sprite>("Sprites/Buildings/saw");
        _vrockSprite  = Resources.Load<Sprite>("Sprites/Buildings/vpick");
        _vsawSprite   = Resources.Load<Sprite>("Sprites/Buildings/vsaw");
        _bricksSprite = Resources.Load<Sprite>("Sprites/Buildings/bricks");
        _planksSprite = Resources.Load<Sprite>("Sprites/Buildings/planks");

        int col = gridPos.x;
        int row = gridPos.y;
        int w = 2, h = 2;  // default (House, Toolshed)
        switch (buildingType)
        {
            case BuildingType.Mill:  w = 3; h = 2; break;
            case BuildingType.Farm:
            case BuildingType.Dairy: w = 3; h = 3; break;
        }

        // Fractional iso world position from grid coords
        Vector3 CellWorld(float cx, float cy) =>
            new Vector3((cx - cy) * layout.isoHalfW, (cx + cy) * layout.isoHalfH, -0.05f);

        int CellSort(float cx, float cy) =>
            -(Mathf.RoundToInt(cx) + Mathf.RoundToInt(cy)) + 1;

        void Spawn(Vector3 worldPos, int sortOrder, WorkCell.CellKind kind)
        {
            var go   = new GameObject($"WorkCell_{_workCells.Count}");
            go.transform.SetParent(transform.parent);
            go.transform.position = worldPos;
            var cell = go.AddComponent<WorkCell>();
            Sprite idle   = kind == WorkCell.CellKind.Corner ? _pickSprite   : _sawSprite;
            Sprite active = kind == WorkCell.CellKind.Corner ? _vrockSprite  : _vsawSprite;
            Sprite done   = kind == WorkCell.CellKind.Corner ? _bricksSprite : _planksSprite;
            var cellSize  = new Vector2(layout.isoHalfW * 2f, layout.isoHalfH * 2f);
            cell.Init(kind, idle, active, done, sortOrder, cellSize);
            _workCells.Add(cell);
        }

        // 4 corners
        Spawn(CellWorld(col - 1,             row - 1    ), CellSort(col - 1, row - 1), WorkCell.CellKind.Corner);
        Spawn(CellWorld(col + w,             row - 1    ), CellSort(col + w, row - 1), WorkCell.CellKind.Corner);
        Spawn(CellWorld(col + w,             row + h    ), CellSort(col + w, row + h), WorkCell.CellKind.Corner);
        Spawn(CellWorld(col - 1,             row + h    ), CellSort(col - 1, row + h), WorkCell.CellKind.Corner);

        // 3 side midpoints (NW / NE / SE) — SW = door side, omitted
        float wMid = col + (w - 1) * 0.5f;
        float hMid = row + (h - 1) * 0.5f;
        Spawn(CellWorld(wMid,    row - 1), CellSort(wMid,    row - 1), WorkCell.CellKind.Side);  // NW
        Spawn(CellWorld(col + w, hMid   ), CellSort(col + w, hMid   ), WorkCell.CellKind.Side);  // NE
        Spawn(CellWorld(wMid,    row + h), CellSort(wMid,    row + h), WorkCell.CellKind.Side);  // SE
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
