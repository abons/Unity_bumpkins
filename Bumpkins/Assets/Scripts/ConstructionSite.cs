using System.Collections;
using UnityEngine;

/// <summary>
/// Multi-stage construction: Blueprint → Resources → Roofing → Done.
/// Attach to the root GO created by BuildManager.  Start() will find the
/// SpriteRenderer child and spawn Logpile / Rockpile props.
/// </summary>
public class ConstructionSite : MonoBehaviour
{
    public enum Stage { Resources, Roofing, Done }

    [Header("Config")]
    public BuildingType buildingType;
    public float workDuration    = 4f;   // seconds per worker trip
    public int   workForResources = 3;   // trips needed for walls stage
    public int   workForRoof      = 2;   // trips needed for complete
    public int   maxWorkers       = 2;

    public Stage CurrentStage   { get; private set; }
    public bool  CanBeWorked    => CurrentStage != Stage.Done && _currentWorkers < maxWorkers;
    /// <summary>World position to walk towards when working here.</summary>
    public Vector2 WorkPosition => CurrentStage == Stage.Resources && _logpile != null
        ? (Vector2)_logpile.transform.position
        : (Vector2)transform.position + new Vector2(0f, -0.3f);

    private int _workDone;
    private int _currentWorkers;
    private GameObject     _logpile;
    private GameObject     _rockpile;
    private SpriteRenderer _buildingSr;

    // ---- Unity ----

    void Start()
    {
        _buildingSr = GetComponentInChildren<SpriteRenderer>();

        // Blueprint tint: light blue, semi-transparent
        if (_buildingSr != null)
            _buildingSr.color = new Color(0.65f, 0.85f, 1f, 0.55f);

        SpawnProps();
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

    public bool TryReserveWorker(BumpkinController b)
    {
        if (!CanBeWorked) return false;
        if (!b.IsMale)   return false;
        _currentWorkers++;
        return true;
    }

    public void ReleaseWorker()
    {
        _currentWorkers = Mathf.Max(0, _currentWorkers - 1);
    }

    /// <summary>Called by bumpkin after finishing one work trip.</summary>
    public void DeliverWork()
    {
        _currentWorkers = Mathf.Max(0, _currentWorkers - 1);
        _workDone++;

        if (CurrentStage == Stage.Resources && _workDone >= workForResources)
        {
            _workDone = 0;
            AdvanceToRoofing();
        }
        else if (CurrentStage == Stage.Roofing && _workDone >= workForRoof)
        {
            _workDone = 0;
            Complete();
        }
        else
        {
            // More work needed in this stage — try to pull in another worker
            TryAutoAssignWorkers();
        }
    }

    // ---- Stage transitions ----

    private void AdvanceToRoofing()
    {
        CurrentStage = Stage.Roofing;

        if (_logpile  != null) Destroy(_logpile);
        if (_rockpile != null) Destroy(_rockpile);

        // Walls: sepia/brown — "house without roof"
        if (_buildingSr != null)
            _buildingSr.color = new Color(0.85f, 0.72f, 0.50f, 1f);

        Debug.Log($"[Construction] {buildingType} aan {name}: muren klaar, begin dak…");
        TryAutoAssignWorkers();
    }

    private void Complete()
    {
        CurrentStage = Stage.Done;

        if (_buildingSr != null)
            _buildingSr.color = Color.white;

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

    // ---- Props ----

    private void SpawnProps()
    {
        var layout = FindFirstObjectByType<GridMapBuilder>()?.layout;
        float offset = layout != null ? layout.isoHalfW * 0.9f : 0.3f;
        int   sr     = _buildingSr != null ? _buildingSr.sortingOrder + 2 : 12;

        _logpile  = SpawnProp("Sprites/Buildings/Logpile",  new Vector3(-offset, -0.05f, 0f), offset, sr);
        _rockpile = SpawnProp("Sprites/Buildings/Rockpile", new Vector3( offset, -0.05f, 0f), offset, sr);
    }

    private GameObject SpawnProp(string path, Vector3 localOffset, float targetW, int sortOrder)
    {
        var go = new GameObject(System.IO.Path.GetFileNameWithoutExtension(path));
        go.transform.SetParent(transform);
        go.transform.localPosition = localOffset;

        var sr = go.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>(path);
        if (sp != null)
        {
            sr.sprite = sp;
            float sc  = targetW * 0.45f / sp.bounds.size.x;
            go.transform.localScale = new Vector3(sc, sc, 1f);
        }
        else
        {
            sr.color = new Color(0.6f, 0.5f, 0.3f);
            go.transform.localScale = new Vector3(targetW * 0.4f, targetW * 0.4f, 1f);
        }
        sr.sortingOrder = sortOrder;
        return go;
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
            if (TryReserveWorker(b))
                b.AssignToConstruction(this);
        }
    }
}
