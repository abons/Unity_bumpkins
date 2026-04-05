using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Manages build mode: ghost preview, tile validation, building placement.
/// Singleton — created by TestBumpkinSetup.
/// </summary>
public class BuildManager : MonoBehaviour
{
    public static BuildManager Instance { get; private set; }

    public bool         InBuildMode  { get; private set; }
    public BuildingType SelectedType { get; private set; }

    private MapLayoutData       _layout;
    private HashSet<Vector2Int> _occupiedTiles = new HashSet<Vector2Int>();

    private GameObject     _ghost;
    private SpriteRenderer _ghostSr;
    private Camera         _cam;
    private GameObject     _gridOverlay;      // single tile highlight that follows cursor
    private SpriteRenderer _tileHighlightSr;
    private GameObject     _ghostRoadParent;  // preview road tiles
    private Vector2Int     _lastGhostGridPos = new Vector2Int(int.MinValue, 0);

    private static readonly Color _validColor   = new Color(0f, 1f, 0f, 0.55f);
    private static readonly Color _invalidColor = new Color(1f, 0f, 0f, 0.55f);

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Start()
    {
        _cam = Camera.main;
        var builder = FindFirstObjectByType<GridMapBuilder>();
        if (builder != null)
        {
            _layout = builder.layout;
            InitOccupiedTiles();
        }
        else
        {
            Debug.LogWarning("[BuildManager] Geen GridMapBuilder gevonden — occupiedTiles niet geïnitialiseerd.");
        }
    }

    void Update()
    {
        if (!InBuildMode) return;
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame) { ExitBuildMode(); return; }
        UpdateGhost();
    }

    // ---- Public API ----

    public void EnterBuildMode(BuildingType type)
    {
        if (InBuildMode) ExitBuildMode();
        SelectedType = type;
        InBuildMode  = true;
        CreateGhost(type);
        CreateGridOverlay();
    }

    public void ExitBuildMode()
    {
        InBuildMode = false;
        if (_ghost != null) Destroy(_ghost);
        _ghost   = null;
        _ghostSr = null;
        if (_gridOverlay != null) Destroy(_gridOverlay);
        _gridOverlay     = null;
        _tileHighlightSr = null;
        ClearGhostRoad();
        _lastGhostGridPos = new Vector2Int(int.MinValue, 0);
    }

    /// <summary>Called by ClickHandler on left-click in build mode.</summary>
    public void HandleBuildClick(Vector2 worldPos)
    {
        var gridPos = WorldToTile(worldPos);
        if (!IsValidPlacement(gridPos, log: true)) return;

        int cost = CostFor(SelectedType);
        if (!GameManager.Instance.Buy(cost, SelectedType.ToString())) return;

        // Mark occupied tiles based on building footprint
        var (fpW, fpH) = FootprintFor(SelectedType);
        for (int dr = 0; dr < fpH; dr++)
        for (int dc = 0; dc < fpW; dc++)
            _occupiedTiles.Add(new Vector2Int(gridPos.x + dc, gridPos.y + dr));

        PlaceBuilding(gridPos, SelectedType);
    }

    public Vector2Int WorldToTile(Vector2 worldPos)
    {
        if (_layout == null) return Vector2Int.zero;
        float a = worldPos.x / _layout.isoHalfW;
        float b = worldPos.y / _layout.isoHalfH;
        int col = Mathf.RoundToInt((a + b) / 2f);
        int row = Mathf.RoundToInt((b - a) / 2f);
        return new Vector2Int(col, row);
    }

    // ---- Ghost ----

    private void CreateGhost(BuildingType type)
    {
        _ghost = new GameObject("GhostPreview");
        var visual = new GameObject("Visual");
        visual.transform.SetParent(_ghost.transform);
        visual.transform.localPosition = Vector3.zero;

        _ghostSr = visual.AddComponent<SpriteRenderer>();
        _ghostSr.sortingOrder = 50;

        if (_layout != null)
        {
            var (gw, gh) = FootprintFor(type);
            float isoW = (gw + gh) * _layout.isoHalfW;
            float isoH = (gw + gh) * _layout.isoHalfH;
            var size   = new Vector2(isoW, isoH);
            var sp     = Resources.Load<Sprite>(SpritePath(type));
            if (sp != null)
            {
                _ghostSr.sprite = sp;
                float scale = Mathf.Min(size.x / sp.bounds.size.x, size.y / sp.bounds.size.y);
                visual.transform.localScale = new Vector3(scale, scale, 1f);
            }
            else
            {
                _ghostSr.sprite = MakeSquareSprite();
                visual.transform.localScale = new Vector3(size.x, size.y, 1f);
            }
        }
        else
        {
            _ghostSr.sprite = MakeSquareSprite();
            visual.transform.localScale = new Vector3(0.5f, 0.5f, 1f);
        }

        _ghostSr.color = _validColor;
    }

    private void UpdateGhost()
    {
        if (_cam == null || _ghost == null || _layout == null) return;

        Vector2 worldPos = _cam.ScreenToWorldPoint(Mouse.current.position.ReadValue());
        var     gridPos  = WorldToTile(worldPos);
        bool    valid    = IsValidPlacement(gridPos);

        // Ghost + highlight snap to footprint center
        bool isCoop = SelectedType == BuildingType.ChickenCoop;
        var (ugw, ugh) = FootprintFor(SelectedType);
        Vector3 snapPos = isCoop
            ? _layout.TileToWorld(gridPos.x, gridPos.y)
            : _layout.BuildingToWorld(gridPos.x, gridPos.y, ugw, ugh);
        snapPos.z = -0.5f;
        _ghost.transform.position = snapPos;
        _ghostSr.color            = valid ? _validColor : _invalidColor;

        // Highlight
        if (_gridOverlay != null)
        {
            Vector3 hlPos = snapPos;
            hlPos.z = -0.4f;
            _gridOverlay.transform.position = hlPos;
            _tileHighlightSr.color = valid
                ? new Color(0f, 1f, 0f, 0.18f)
                : new Color(1f, 0f, 0f, 0.18f);
        }

        // Ghost road preview für House/Toolshed/Mill/Farm/Dairy
        if (SelectedType == BuildingType.House || SelectedType == BuildingType.Toolshed ||
            SelectedType == BuildingType.Mill  || SelectedType == BuildingType.Farm ||
            SelectedType == BuildingType.Dairy)
        {
            if (gridPos != _lastGhostGridPos)
            {
                _lastGhostGridPos = gridPos;
                ClearGhostRoad();
                if (valid)
                    BuildGhostRoad(gridPos);
            }
        }
        else
        {
            ClearGhostRoad();
        }
    }

    // ---- Ghost road preview ----

    private void ClearGhostRoad()
    {
        if (_ghostRoadParent != null) { Destroy(_ghostRoadParent); _ghostRoadParent = null; }
    }

    private void BuildGhostRoad(Vector2Int gridPos)
    {
        Vector2Int from = DoorExit(SelectedType, gridPos);

        // Build set of tiles occupied by the ghost building itself so BFS avoids them
        var (gw, gh) = FootprintFor(SelectedType);
        var ghostTiles = new HashSet<Vector2Int>();
        for (int dr = 0; dr < gh; dr++)
        for (int dc = 0; dc < gw; dc++)
            ghostTiles.Add(new Vector2Int(gridPos.x + dc, gridPos.y + dr));

        var path = ComputeRoadPath(from, out Vector2Int junction, ghostTiles);
        if (path.Count == 0) return;

        _ghostRoadParent = new GameObject("GhostRoad");

        var   roadSp   = Resources.Load<Sprite>("Sprites/Terrain/roads");
        float isoW     = _layout.isoHalfW * 2f;
        float isoH     = _layout.isoHalfH * 2f;
        var   tileVec  = new Vector2(isoW, isoH);

        var sp = roadSp ?? MakeSquareSprite();

        void SpawnGhostSprite(Vector2Int tile, bool flipY)
        {
            Vector3 pos = _layout.TileToWorld(tile.x, tile.y);
            pos.z = -0.3f;
            var go = new GameObject($"GhostRoad_{tile.x}_{tile.y}_{(flipY ? "R" : "C")}");
            go.transform.SetParent(_ghostRoadParent.transform);
            go.transform.position = pos;
            float scaleX = tileVec.x / sp.bounds.size.x;
            float scaleY = tileVec.y / sp.bounds.size.y;
            float sc     = Mathf.Max(scaleX, scaleY) * 1.005f;
            go.transform.localScale = new Vector3(sc, sc, 1f);
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = sp;
            sr.sortingOrder = _layout.SortOrder(tile.x, tile.y) + 2;
            sr.color        = new Color(0.9f, 0.8f, 0.3f, 0.6f);
            if (roadSp != null) sr.flipY = flipY;
        }

        for (int i = 0; i < path.Count; i++)
        {
            var (tile, flipY) = path[i];
            if (_layout.GetTile(tile.x, tile.y) == TileType.Road) continue;
            SpawnGhostSprite(tile, flipY);
            // Door exit (i==0) is always a corner: also spawn the house-side direction
            if (i == 0)
                SpawnGhostSprite(tile, !flipY);
            // Mid-path corner: direction changes from previous tile
            else if (path[i - 1].flipY != flipY)
                SpawnGhostSprite(tile, !flipY);
        }

        // Splice preview at junction tile
        if (path.Count > 0)
        {
            var last = path[path.Count - 1].tile;
            bool cs  = Mathf.Abs(junction.x - last.x) >= Mathf.Abs(junction.y - last.y);
            SpawnGhostSprite(junction, cs);
        }
    }

    private List<(Vector2Int tile, bool flipY)> ComputeRoadPath(Vector2Int from, out Vector2Int junction, HashSet<Vector2Int> extraBlocked = null)
    {
        var result = new List<(Vector2Int, bool)>();
        junction = from;
        if (_layout == null) return result;

        // Find nearest road tile (Manhattan distance)
        Vector2Int best     = from;
        float      bestDist = float.MaxValue;
        for (int row = 0; row < _layout.rows; row++)
        for (int col = 0; col < _layout.cols; col++)
        {
            if (_layout.GetTile(col, row) != TileType.Road) continue;
            float d = Mathf.Abs(col - from.x) + Mathf.Abs(row - from.y);
            if (d < bestDist) { bestDist = d; best = new Vector2Int(col, row); }
        }
        junction = best;
        if (best == from) return result;

        // BFS — avoids occupied building tiles, allows crossing grass and existing roads
        var visited = new Dictionary<Vector2Int, Vector2Int>();  // tile → parent
        var queue   = new Queue<Vector2Int>();
        var dirs    = new[] {
            new Vector2Int(1, 0), new Vector2Int(-1, 0),
            new Vector2Int(0, 1), new Vector2Int(0, -1),
        };

        visited[from] = from;
        queue.Enqueue(from);
        bool found = false;

        while (queue.Count > 0 && !found)
        {
            var cur = queue.Dequeue();
            foreach (var dir in dirs)
            {
                var next = cur + dir;
                if (next.x < 0 || next.x >= _layout.cols || next.y < 0 || next.y >= _layout.rows) continue;
                if (visited.ContainsKey(next)) continue;

                var tileType  = _layout.GetTile(next.x, next.y);
                bool isRoad      = tileType == TileType.Road;
                bool isFreeGrass = tileType == TileType.Grass && !_occupiedTiles.Contains(next)
                                    && (extraBlocked == null || !extraBlocked.Contains(next));
                if (!isRoad && !isFreeGrass) continue;

                visited[next] = cur;
                if (next == best) { found = true; break; }
                queue.Enqueue(next);
            }
        }

        if (!found) return result;

        // Reconstruct path from from→best (best excluded — it's the junction)
        var rawPath = new List<Vector2Int>();
        var step = best;
        while (step != from) { rawPath.Add(step); step = visited[step]; }
        rawPath.Add(from);
        rawPath.Reverse();  // rawPath[0] == from, rawPath[last] == best

        for (int i = 0; i < rawPath.Count - 1; i++)
        {
            var tile = rawPath[i];
            var next = rawPath[i + 1];
            bool flipY = (next.x - tile.x) != 0;  // col step (NE-SW) → flipY; row step (NW-SE) → no flip
            result.Add((tile, flipY));
        }
        return result;
    }

    // ---- Grid overlay (single cursor tile) ----

    private void CreateGridOverlay()
    {
        if (_layout == null) return;

        _gridOverlay = new GameObject("BuildTileHighlight");

        var (ogw, ogh) = FootprintFor(SelectedType);
        float isoW = (ogw + ogh) * _layout.isoHalfW;
        float isoH = (ogw + ogh) * _layout.isoHalfH;
        var   sp   = MakeSquareSprite();

        _tileHighlightSr               = _gridOverlay.AddComponent<SpriteRenderer>();
        _tileHighlightSr.sprite        = sp;
        _tileHighlightSr.sortingOrder  = 45;
        _tileHighlightSr.color         = new Color(0f, 1f, 0f, 0.18f);

        float scaleX = isoW / sp.bounds.size.x;
        float scaleY = isoH / sp.bounds.size.y;
        float s      = Mathf.Max(scaleX, scaleY) * 0.97f;
        _gridOverlay.transform.localScale = new Vector3(s, s, 1f);
    }

    // ---- Placement — creates ConstructionSite ----

    private void PlaceBuilding(Vector2Int gridPos, BuildingType type)
    {
        if (_layout == null) return;

        var (w, h) = FootprintFor(type);
        bool isCoop = (type == BuildingType.ChickenCoop);
        var   center = isCoop
            ? _layout.TileToWorld(gridPos.x, gridPos.y)
            : _layout.BuildingToWorld(gridPos.x, gridPos.y, w, h);
        center.z = -0.1f;
        float isoW   = (w + h) * _layout.isoHalfW;
        float isoH   = (w + h) * _layout.isoHalfH;
        var   size   = new Vector2(isoW, isoH);
        int   bSort  = _layout.BuildingSortOrder(gridPos.x, gridPos.y, w, h);

        var root = new GameObject($"{type}_{gridPos.x}_{gridPos.y}");
        root.transform.position = center;

        var boxCol = root.AddComponent<BoxCollider2D>();
        boxCol.size = size * 0.9f;

        // Visual child — sprite renderer used by ConstructionSite
        var visual = new GameObject("Visual");
        visual.transform.SetParent(root.transform);
        visual.transform.localPosition = Vector3.zero;

        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sortingOrder = bSort;

        var sp = Resources.Load<Sprite>(SpritePath(type));
        if (sp != null)
        {
            sr.sprite = sp;
            float scale = Mathf.Min(size.x / sp.bounds.size.x, size.y / sp.bounds.size.y);
            visual.transform.localScale = new Vector3(scale, scale, 1f);
        }
        else
        {
            sr.sprite = MakeSquareSprite();
            sr.color  = new Color(0.6f, 0.8f, 0.3f, 1f);
            visual.transform.localScale = new Vector3(size.x * 0.9f, size.y * 0.9f, 1f);
        }

        // ConstructionSite handles the rest (props, stages, final activation)
        // ChickenCoop wordt direct geplaatst, geen bouw nodig
        if (type == BuildingType.ChickenCoop)
        {
            // Spawn chicken direct op het gebouw
            var chickenGo = new GameObject("Chicken");
            chickenGo.transform.SetParent(root.transform);
            chickenGo.transform.localPosition = Vector3.zero;
            var csr = chickenGo.AddComponent<SpriteRenderer>();
            var csp = Resources.Load<Sprite>("Sprites/Units/Chicken");
            if (csp != null)
            {
                csr.sprite = csp;
                float cScale = size.x * 0.09f / csp.bounds.size.x;
                chickenGo.transform.localScale = new Vector3(cScale, cScale, 1f);
            }
            csr.sortingOrder = bSort + 1;
            chickenGo.AddComponent<ChickenAnimator>();
        }
        else
        {
            var site = root.AddComponent<ConstructionSite>();
            site.buildingType = type;
            site.InitWorkCells(gridPos, _layout);

            // House/Toolshed/Mill/Farm: verbind met dichtstbijzijnde weg via deur-positie
            if (type == BuildingType.House)
            {
                // Deur op SW → exit tile één stap in -col richting
                SpawnRoadToNearestRoad(DoorExit(type, gridPos));
            }
            else if (type == BuildingType.Toolshed)
            {
                // Deur op SE → exit tile één stap in -row richting (rechtsonder)
                SpawnRoadToNearestRoad(DoorExit(type, gridPos));
            }
            else if (type == BuildingType.Mill || type == BuildingType.Farm || type == BuildingType.Dairy)
            {
                // Deur op SW → exit tile één stap links
                SpawnRoadToNearestRoad(DoorExit(type, gridPos));
            }
        }

        Debug.Log($"[BuildManager] {type} geplaatst op ({gridPos.x},{gridPos.y})");
    }

    // ---- Validation ----

    private bool IsValidPlacement(Vector2Int gridPos, bool log = false)
    {
        if (_layout == null) return false;
        var (fpW, fpH) = FootprintFor(SelectedType);
        for (int dr = 0; dr < fpH; dr++)
        for (int dc = 0; dc < fpW; dc++)
        {
            int c = gridPos.x + dc, r = gridPos.y + dr;
            if (c < 0 || c >= _layout.cols || r < 0 || r >= _layout.rows)
            {
                if (log) Debug.Log($"[Build] ({gridPos.x},{gridPos.y})+({dc},{dr}) = ({c},{r}) — buiten grid");
                return false;
            }
            var tile = _layout.GetTile(c, r);
            if (tile != TileType.Grass)
            {
                if (log) Debug.Log($"[Build] ({c},{r}) — geen gras ({tile})");
                return false;
            }
            if (_occupiedTiles.Contains(new Vector2Int(c, r)))
            {
                if (log)
                {
                    // Find which layout building owns this tile
                    string owner = "onbekend";
                    foreach (var b in _layout.buildings)
                    {
                        if (c >= b.position.x && c < b.position.x + b.size.x &&
                            r >= b.position.y && r < b.position.y + b.size.y)
                        {
                            owner = $"{(BuildingType)b.type} op ({b.position.x},{b.position.y}) size {b.size.x}×{b.size.y}";
                            break;
                        }
                    }
                    Debug.Log($"[Build] ({c},{r}) — bezet door {owner}");
                }
                return false;
            }
        }
        return true;
    }

    private bool HasAdjacentRoad(Vector2Int p)
    {
        return _layout.GetTile(p.x - 1, p.y) == TileType.Road
            || _layout.GetTile(p.x + 1, p.y) == TileType.Road
            || _layout.GetTile(p.x, p.y - 1) == TileType.Road
            || _layout.GetTile(p.x, p.y + 1) == TileType.Road;
    }

    /// <summary>
    /// Loopt van gridPos axis-aligned richting de dichtstbijzijnde road tile.
    /// Per stap: kies de richting (col of row) die de manhattan-afstand het meest verkleint.
    /// Spawnt een roadsprite op elk grastile tussenin en markeert het als Road in de terrain-array.
    /// </summary>
    private void SpawnRoadToNearestRoad(Vector2Int from)
    {
        if (_layout == null) return;

        // Vind dichtstbijzijnde road tile
        Vector2Int best     = from;
        float      bestDist = float.MaxValue;
        for (int row = 0; row < _layout.rows; row++)
        for (int col = 0; col < _layout.cols; col++)
        {
            if (_layout.GetTile(col, row) != TileType.Road) continue;
            float d = Mathf.Abs(col - from.x) + Mathf.Abs(row - from.y);
            if (d < bestDist) { bestDist = d; best = new Vector2Int(col, row); }
        }
        if (best == from) return;

        var   roadSp    = Resources.Load<Sprite>("Sprites/Terrain/roads");
        float isoW      = _layout.isoHalfW * 2f;
        float isoH      = _layout.isoHalfH * 2f;
        var   tileVec   = new Vector2(isoW, isoH);
        var   roadParent = GameObject.Find("Terrain")?.transform ?? transform;

        // Overall direction determines flip
        var path = ComputeRoadPath(from, out Vector2Int junction);
        if (path.Count == 0) return;

        // Start met het deur-exittile zelf (from), daarna loop naar doel
        for (int i = 0; i < path.Count; i++)
        {
            var (cur, flipY) = path[i];
            if (_layout.GetTile(cur.x, cur.y) != TileType.Grass) continue;
            _layout.terrain[cur.y * _layout.cols + cur.x] = TileType.Road;
            _occupiedTiles.Add(cur);
            SpawnRoadTile(cur, roadSp, tileVec, roadParent, flipY);
            // Door exit (i==0) is always a corner: also spawn the house-side direction
            if (i == 0)
                SpawnRoadTile(cur, roadSp, tileVec, roadParent, !flipY);
            // Mid-path corner: direction changes from previous tile
            else if (path[i - 1].flipY != flipY)
                SpawnRoadTile(cur, roadSp, tileVec, roadParent, !flipY);
        }

        // Splice at junction: add approach-direction sprite to the existing road tile at best
        if (path.Count > 0)
        {
            var last = path[path.Count - 1].tile;
            bool cs  = Mathf.Abs(junction.x - last.x) >= Mathf.Abs(junction.y - last.y);
            SpawnRoadTile(junction, roadSp, tileVec, roadParent, cs);
        }
    }

    private void SpawnRoadTile(Vector2Int tile, Sprite roadSp, Vector2 tileVec, Transform parent, bool flipY)
    {
        var center = _layout.TileToWorld(tile.x, tile.y);
        center.z   = 0.01f;
        int sOrder = _layout.SortOrder(tile.x, tile.y) + 1;

        if (roadSp != null)
        {
            float scaleX = tileVec.x / roadSp.bounds.size.x;
            float scaleY = tileVec.y / roadSp.bounds.size.y;
            float sc     = Mathf.Max(scaleX, scaleY) * 1.005f;

            // Always draw col-axis sprite
            if (!flipY)
            {
                var go = new GameObject($"Road_{tile.x}_{tile.y}");
                go.transform.SetParent(parent);
                go.transform.position   = center;
                go.transform.localScale = new Vector3(sc, sc, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = roadSp; sr.sortingOrder = sOrder;
            }
            // Always draw row-axis sprite
            if (flipY)
            {
                var go = new GameObject($"RoadR_{tile.x}_{tile.y}");
                go.transform.SetParent(parent);
                go.transform.position   = center;
                go.transform.localScale = new Vector3(sc, sc, 1f);
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = roadSp; sr.sortingOrder = sOrder; sr.flipY = true;
            }
        }
        else
        {
            var go = new GameObject($"Road_{tile.x}_{tile.y}");
            go.transform.SetParent(parent);
            go.transform.position = center;
            var sr   = go.AddComponent<SpriteRenderer>();
            sr.color        = new Color(0.7f, 0.6f, 0.4f);
            sr.sortingOrder = sOrder;
            go.transform.localScale = new Vector3(tileVec.x * 0.95f, tileVec.y * 0.95f, 1f);
        }
    }

    private void InitOccupiedTiles()
    {
        if (_layout == null) return;

        for (int row = 0; row < _layout.rows; row++)
        for (int col = 0; col < _layout.cols; col++)
        {
            if (_layout.GetTile(col, row) != TileType.Grass)
                _occupiedTiles.Add(new Vector2Int(col, row));
        }

        foreach (var b in _layout.buildings)
        for (int dr = 0; dr < b.size.y; dr++)
        for (int dc = 0; dc < b.size.x; dc++)
            _occupiedTiles.Add(new Vector2Int(b.position.x + dc, b.position.y + dr));
    }

    // ---- Helpers ----

    private static (int w, int h) FootprintFor(BuildingType type) => type switch
    {
        BuildingType.ChickenCoop => (1, 1),
        BuildingType.Mill        => (3, 2),
        BuildingType.Farm        => (3, 3),
        BuildingType.Dairy       => (3, 3),
        _                        => (2, 2),
    };

    private static Vector2Int DoorExit(BuildingType type, Vector2Int gridPos) => type switch
    {
        BuildingType.Toolshed => new Vector2Int(gridPos.x,     gridPos.y - 1), // SE: step -row
        BuildingType.Mill     => new Vector2Int(gridPos.x + 1, gridPos.y - 1), // SE corner: +col -row
        _                     => new Vector2Int(gridPos.x - 1, gridPos.y),     // SW: step -col
    };

    private int CostFor(BuildingType type)
    {
        if (GameManager.Instance != null)
        {
            var cfg = GameManager.Instance.config;
            switch (type)
            {
                case BuildingType.House:       return cfg.costHouse;
                case BuildingType.Toolshed:    return cfg.costToolshed;
                case BuildingType.ChickenCoop: return cfg.costChickenCoop;
                case BuildingType.Mill:        return cfg.costMill;
                case BuildingType.Dairy:       return cfg.costDairy;
            }
        }
        return 999;
    }

    private static string SpritePath(BuildingType type) => type switch
    {
        BuildingType.House       => "Sprites/Buildings/House",
        BuildingType.Toolshed    => "Sprites/Buildings/Toolshed",
        BuildingType.ChickenCoop => "Sprites/Buildings/ChickenCoop",
        _                        => $"Sprites/Buildings/{type}",
    };

    private static Sprite _squareSprite;
    private static Sprite MakeSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        _squareSprite = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _squareSprite;
    }
}
