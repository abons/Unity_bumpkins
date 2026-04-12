using UnityEngine;

/// <summary>
/// Reads a MapLayoutData asset and spawns tile + building GameObjects in the scene.
/// Attach to an empty "Map" GameObject. Wire up prefabs in the inspector.
/// </summary>
public class GridMapBuilder : MonoBehaviour
{
    [Header("Layout")]
    public MapLayoutData layout;

    [Header("Tile Prefabs (SpriteRenderer, 1×1 unit)")]
    public GameObject grassPrefab;
    public GameObject roadPrefab;
    public GameObject farmPlotPrefab;
    public GameObject rockPrefab;
    public GameObject woodPrefab;
    public GameObject waterPrefab;

    [Header("Building Prefabs")]
    public GameObject millPrefab;
    public GameObject cowPrefab;
    public GameObject campfirePrefab;
    public GameObject rockpilePrefab;
    public GameObject woodpilePrefab;
    public GameObject dairyPrefab;
    public GameObject chickenCoopPrefab;
    public GameObject housePrefab;

    [Header("Terrain Tint Colors")]
    public Color grassTint = Color.white;                       // natural green (no tint)

    void Awake()
    {
        // Resolve layout early (Awake) so other scripts can read it in their Start()
        var ui = FindFirstObjectByType<UIManager>();

        // A menu-scene selection overrides the serialized Inspector default.
        if (!string.IsNullOrEmpty(MapSelection.SelectedLayoutName))
        {
            if (ui != null && ui.maps != null)
            {
                foreach (var m in ui.maps)
                {
                    if (m != null && m.name == MapSelection.SelectedLayoutName)
                    {
                        layout = m;
                        break;
                    }
                }
            }
            if (layout == null || layout.name != MapSelection.SelectedLayoutName)
                Debug.LogWarning($"[GridMapBuilder] MapSelection '{MapSelection.SelectedLayoutName}' not found in UIManager.maps; falling back to serialized default.");
        }

        // If still no layout (no selection and nothing wired in Inspector), use maps[0].
        if (layout == null && ui != null && ui.maps != null && ui.maps.Length > 0)
            layout = ui.maps[0];
    }

    void Start()
    {
        if (layout == null) { Debug.LogError("[GridMapBuilder] No layout assigned!"); return; }
        BuildTerrain();
        BuildBuildings();
        PatchDoorExitRoads();
        SpawnEnemies();
        NavGrid.Build(layout);
        FindFirstObjectByType<CameraController>()?.AdaptToLayout(layout);
    }

    /// <summary>Tear down the current map and build a new one at runtime.</summary>
    public void LoadMap(MapLayoutData newLayout)
    {
        if (newLayout == null) { Debug.LogError("[GridMapBuilder] LoadMap: newLayout is null"); return; }
        layout = newLayout;

        // Destroy all child GameObjects (terrain, buildings, enemies)
        for (int i = transform.childCount - 1; i >= 0; i--)
            Destroy(transform.GetChild(i).gameObject);

        // Destroy any loose enemy GameObjects spawned at root level
        foreach (var t in System.Enum.GetValues(typeof(EnemyType)))
        {
            var go = GameObject.Find(t.ToString());
            if (go != null) Destroy(go);
        }

        BuildTerrain();
        BuildBuildings();
        PatchDoorExitRoads();
        SpawnEnemies();
        NavGrid.Build(layout);
        FindFirstObjectByType<CameraController>()?.AdaptToLayout(layout);
    }

    private void SpawnEnemies()
    {
        if (layout.enemies == null) return;
        foreach (var entry in layout.enemies)
        {
            var go = new GameObject(entry.type.ToString());
            go.transform.position = layout.TileToWorld(entry.position.x, entry.position.y);
            var sr = go.AddComponent<SpriteRenderer>();
            var sp = Resources.Load<Sprite>(SpritePath(entry.type));
            if (sp != null) { sr.sprite = sp; go.transform.localScale = new Vector3(3f, 3f, 1f); }
            sr.sortingOrder = 10;
            go.AddComponent<BoxCollider2D>();
            AddEnemyController(go, entry.type);
        }
    }

    private static string SpritePath(EnemyType t) => t switch
    {
        EnemyType.Wolf      => $"{GraphicsQuality.SpritePath}/Animals/wolfstil",
        EnemyType.Wasp      => $"{GraphicsQuality.SpritePath}/Animals/wasp",
        EnemyType.Bat       => $"{GraphicsQuality.SpritePath}/Animals/bat",
        EnemyType.Ogre      => $"{GraphicsQuality.SpritePath}/Animals/ogrestil",
        EnemyType.Zombie    => $"{GraphicsQuality.SpritePath}/Animals/zombie",
        EnemyType.Giant     => $"{GraphicsQuality.SpritePath}/Animals/gianstil",
        EnemyType.BloodWasp => $"{GraphicsQuality.SpritePath}/Animals/bloodwsp",
        _                   => "",
    };

    private static void AddEnemyController(GameObject go, EnemyType t)
    {
        switch (t)
        {
            case EnemyType.Wolf:      go.AddComponent<WolfController>();      break;
            case EnemyType.Wasp:      go.AddComponent<WaspController>();      break;
            case EnemyType.Bat:       go.AddComponent<BatController>();       break;
            case EnemyType.Ogre:      go.AddComponent<OgreController>();      break;
            case EnemyType.Zombie:    go.AddComponent<ZombieController>();    break;
            case EnemyType.Giant:     go.AddComponent<GiantController>();     break;
            case EnemyType.BloodWasp: go.AddComponent<BloodWaspController>(); break;
        }
    }

    // ---- Terrain ----
    private void BuildTerrain()
    {
        var parent = new GameObject("Terrain").transform;
        parent.SetParent(transform);

        var waterEdgeSprite = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Terrain/Water");
        var waterEdgeMat    = Resources.Load<Material>("Materials/WaterEdge");
        var sandEdgeSprite  = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Terrain/Sand");
        var sandEdgeMat     = Resources.Load<Material>("Materials/SandEdge");

        for (int row = 0; row < layout.rows; row++)
        for (int col = 0; col < layout.cols; col++)
        {
            var tile    = layout.GetTile(col, row);
            var center  = layout.TileToWorld(col, row);
            var tileVec = new Vector2(layout.isoHalfW * 2f, layout.isoHalfH * 2f);
            int sOrder  = layout.SortOrder(col, row);

            // Altijd eerst gras spawnen als achtergrond — fill zodat er geen gaps zijn
            var grassGo = MakeSpriteFill("Terrain/Grass", center, tileVec, parent, sOrder)
                       ?? MakePlaceholder(TileColor(TileType.Grass), center, tileVec, parent, sOrder);
            grassGo.name = $"Grass_{col}_{row}";

            // Road: road sprite boven het gras, met juiste rotatie/flip
            if (tile == TileType.Road)
            {
                bool hasRowNeighbor = layout.GetTile(col - 1, row) == TileType.Road
                                   || layout.GetTile(col + 1, row) == TileType.Road;
                bool hasColNeighbor = layout.GetTile(col, row - 1) == TileType.Road
                                   || layout.GetTile(col, row + 1) == TileType.Road;

                // Col-axis sprite (standaard, geen flip)
                if (hasColNeighbor || (!hasRowNeighbor && !hasColNeighbor))
                {
                    var roadGo = MakeSpriteFill("Terrain/roads", center, tileVec, parent, sOrder + 1);
                    if (roadGo != null) roadGo.name = $"Road_{col}_{row}";
                    else { var ph = MakePlaceholder(TileColor(TileType.Road), center, tileVec, parent, sOrder + 1); ph.name = $"Road_{col}_{row}"; }
                }

                // Row-axis sprite (flipY) — ook tekenen bij corners/kruisingen
                if (hasRowNeighbor)
                {
                    var roadGoR = MakeSpriteFill("Terrain/roads", center, tileVec, parent, sOrder + 1);
                    if (roadGoR != null)
                    {
                        roadGoR.name = $"RoadR_{col}_{row}";
                        if (roadGoR.TryGetComponent<SpriteRenderer>(out var rsr)) rsr.flipY = true;
                    }
                }
            }
            else if (tile != TileType.Grass)
            {
                // Andere niet-gras tiles (Rock, Water etc.) boven het gras
                if (tile == TileType.Water)
                {
                    // Sea tile — Water sprite everywhere; WaterEdge shader overlays on adjacent Sand tiles
                    // handle the organic noise transition. No sandhill sprites used.
                    var seaGo = MakeSpriteFill("Terrain/Water", center, tileVec, parent, sOrder + 2)
                             ?? MakePlaceholder(TileColor(TileType.Water), center, tileVec, parent, sOrder + 2);
                    seaGo.name = $"Tile_{col}_{row}_sea";
                }
                else if (tile == TileType.Sand)
                {
                    // Beach strip — dedicated Sand sprite
                    var sandGo = MakeSpriteFill("Terrain/Sand", center, tileVec, parent, sOrder + 1)
                              ?? MakePlaceholder(TileColor(TileType.Sand), center, tileVec, parent, sOrder + 1);
                    sandGo.name = $"Tile_{col}_{row}_sand";

                    // Directional WaterEdge overlays — shader clips the Water sprite with smooth noise
                    void SpawnEdge(int dir, string dirLabel)
                    {
                        if (waterEdgeSprite == null || waterEdgeMat == null) return;
                        var ov = new GameObject($"Tile_{col}_{row}_waterEdge_{dirLabel}");
                        ov.transform.SetParent(parent);
                        ov.transform.position = center;
                        float sprW  = waterEdgeSprite.bounds.size.x;
                        float sprH  = waterEdgeSprite.bounds.size.y;
                        float scale = Mathf.Max(tileVec.x / sprW, tileVec.y / sprH);
                        ov.transform.localScale = new Vector3(scale, scale, 1f);
                        var sr = ov.AddComponent<SpriteRenderer>();
                        sr.sprite         = waterEdgeSprite;
                        sr.sharedMaterial = waterEdgeMat;
                        sr.sortingOrder   = sOrder + 2;
                        var mpb = new MaterialPropertyBlock();
                        mpb.SetFloat("_Direction", dir);
                        sr.SetPropertyBlock(mpb);
                    }
                    if (layout.GetTile(col + 1, row) == TileType.Water) SpawnEdge(0, "colP1");
                    if (layout.GetTile(col - 1, row) == TileType.Water) SpawnEdge(1, "colM1");
                    if (layout.GetTile(col, row + 1) == TileType.Water) SpawnEdge(2, "rowP1");
                    if (layout.GetTile(col, row - 1) == TileType.Water) SpawnEdge(3, "rowM1");
                }
                else
                {
                    var prefab = TilePrefab(tile);
                    GameObject go;
                    if (prefab != null)
                    {
                        go = Instantiate(prefab, center, Quaternion.identity, parent);
                        if (go.TryGetComponent<SpriteRenderer>(out var psr)) psr.sortingOrder = sOrder + 1;
                    }
                    else
                    {
                        var resName = TileResourceName(tile);
                        go = (resName != null ? MakeSprite(resName, center, tileVec, parent, sOrder + 1) : null)
                          ?? MakePlaceholder(TileColor(tile), center, tileVec, parent, sOrder + 1);

                        // Attach seasonal switcher to tree tiles
                        var (summerPath, winterPath) = SeasonalPaths(tile);
                        if (summerPath != null && go != null)
                        {
                            var seasonal = go.AddComponent<SeasonalTreeTile>();
                            seasonal.SummerResourcePath = summerPath;
                            seasonal.WinterResourcePath = winterPath;
                        }
                    }
                    go.name = $"Tile_{col}_{row}_{tile}";
                }
            }
            else // tile == TileType.Grass — SandEdge overlays on grass tiles adjacent to sand
            {
                void SpawnSandEdge(int dir, string dirLabel)
                {
                    if (sandEdgeSprite == null || sandEdgeMat == null) return;
                    var ov = new GameObject($"Tile_{col}_{row}_sandEdge_{dirLabel}");
                    ov.transform.SetParent(parent);
                    ov.transform.position = center;
                    float sprW  = sandEdgeSprite.bounds.size.x;
                    float sprH  = sandEdgeSprite.bounds.size.y;
                    float scale = Mathf.Max(tileVec.x / sprW, tileVec.y / sprH);
                    ov.transform.localScale = new Vector3(scale, scale, 1f);
                    var sr = ov.AddComponent<SpriteRenderer>();
                    sr.sprite         = sandEdgeSprite;
                    sr.sharedMaterial = sandEdgeMat;
                    sr.sortingOrder   = sOrder + 1;
                    var mpb = new MaterialPropertyBlock();
                    mpb.SetFloat("_Direction", dir);
                    sr.SetPropertyBlock(mpb);
                }
                if (layout.GetTile(col + 1, row) == TileType.Sand) SpawnSandEdge(0, "colP1");
                if (layout.GetTile(col - 1, row) == TileType.Sand) SpawnSandEdge(1, "colM1");
                if (layout.GetTile(col, row + 1) == TileType.Sand) SpawnSandEdge(2, "rowP1");
                if (layout.GetTile(col, row - 1) == TileType.Sand) SpawnSandEdge(3, "rowM1");
            }
        }
    }

    // ---- Buildings ----
    private void BuildBuildings()
    {
        if (layout.buildings == null) return;
        var parent = new GameObject("Buildings").transform;
        parent.SetParent(transform);

        // Unlock buildings based on existing map buildings
        var gm = GameManager.Instance;
        if (gm != null)
        {
            foreach (var b in layout.buildings)
            {
                if (b.type == BuildingType.Toolshed || b.type == BuildingType.Mill)
                    gm.UnlockMill();
                if (b.type == BuildingType.Mill)
                    gm.UnlockDairy();
            }
        }

        foreach (var b in layout.buildings)
        {
            var center = layout.BuildingToWorld(b.position.x, b.position.y, b.size.x, b.size.y);
            // Iso footprint in screen space
            float isoW = (b.size.x + b.size.y) * layout.isoHalfW;
            float isoH = (b.size.x + b.size.y) * layout.isoHalfH;
            var   size = new Vector2(isoW, isoH);
            int   bSort = layout.BuildingSortOrder(b.position.x, b.position.y, b.size.x, b.size.y);

            // Root: scale 1 → collider size = world size
            var root = new GameObject(b.type == BuildingType.Cow
                ? $"Cow_{b.position.x}_{b.position.y}"
                : $"{b.type}_{b.position.x}_{b.position.y}");
            root.transform.SetParent(parent);
            root.transform.position = center;

            var col = root.AddComponent<BoxCollider2D>();
            col.size = size * 0.9f;

            // Visual child
            var visual = new GameObject("Visual");
            visual.transform.SetParent(root.transform);
            visual.transform.localPosition = b.type == BuildingType.House ? new Vector3(0.18f, -0.86f, 0f) : Vector3.zero;

            var prefab = BuildingPrefab(b.type);
            if (prefab != null)
            {
                var go = Instantiate(prefab, visual.transform);
                go.transform.localPosition = Vector3.zero;
                go.transform.localScale    = new Vector3(size.x, size.y, 1f);
            }
            else if (b.type != BuildingType.Cow) // koe heeft eigen CowSprite child
            {
                var sr = visual.AddComponent<SpriteRenderer>();
                sr.sortingOrder = bSort;
                var spritePath = b.type == BuildingType.Woodpile
                    ? $"{GraphicsQuality.SpritePath}/Buildings/Logpile"
                    : $"{GraphicsQuality.SpritePath}/Buildings/{b.type}";
                var sp = Resources.Load<Sprite>(spritePath);
                if (sp != null)
                {
                    sr.sprite = sp;
                    float scale = Mathf.Min(size.x / sp.bounds.size.x, size.y / sp.bounds.size.y);
                    // Per-type schaal-multiplier
                    if (b.type == BuildingType.Campfire)    scale *= 0.4f;
                    visual.transform.localScale = new Vector3(scale, scale, 1f);
                }
                else
                {
                    sr.sprite = GetSquareSprite();
                    sr.color  = BuildingColor(b.type);
                    visual.transform.localScale = new Vector3(size.x * 0.95f, size.y * 0.95f, 1f);
                }
            }

            // Extra overlay voor ChickenCoop: spawn een kip bovenop
            if (b.type == BuildingType.ChickenCoop)
            {
                var chickenGo = new GameObject("Chicken");
                chickenGo.transform.SetParent(root.transform);
                chickenGo.transform.localPosition = new Vector3(0f, 0f, 0f);
                var csr = chickenGo.AddComponent<SpriteRenderer>();
                var csp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/Chicken");
                if (csp != null)
                {
                    csr.sprite = csp;
                    // Kip = 9% van de breedte van het gebouw
                    float cScale = size.x * 0.09f / csp.bounds.size.x;
                    chickenGo.transform.localScale = new Vector3(cScale, cScale, 1f);
                }
                csr.sortingOrder = bSort + 1;
                chickenGo.AddComponent<ChickenAnimator>();
            }

            // Vuuranimatie op kampvuur
            if (b.type == BuildingType.Campfire)
                visual.AddComponent<CampfireAnimator>();

            // Gebouwen die bumpkins als idle-doel mogen bezoeken
            if (b.type == BuildingType.House || b.type == BuildingType.Toolshed)
            {
                var tag = root.AddComponent<BuildingTag>();
                tag.isHouse    = (b.type == BuildingType.House);
                tag.isToolshed = (b.type == BuildingType.Toolshed);
                // Door exit tile (mirrors PatchDoorExitRoads / BuildManager.DoorExit)
                Vector2 exitWorld = b.type == BuildingType.House
                    ? layout.TileToWorld(b.position.x - 1, b.position.y)   // SW
                    : layout.TileToWorld(b.position.x,     b.position.y - 1); // SE
                tag.doorOffset = exitWorld - (Vector2)center;
            }

            // Huis deur animatie
            if (b.type == BuildingType.House)
                visual.AddComponent<HouseAnimator>();

            // Toolshed deur animatie
            if (b.type == BuildingType.Toolshed)
                visual.AddComponent<ToolshedAnimator>();

            // Molen wieken + deur animatie + drop-off node
            if (b.type == BuildingType.Mill)
            {
                visual.AddComponent<MillAnimator>();
                var dropOff = root.AddComponent<DropOffNode>();
                dropOff.dropOffType = DropOffNode.DropOffType.Mill;
                // Target the walkable tile just outside the SE face of the mill footprint.
                // DoorExit = (col+1, row-1). Offset from building center to that tile:
                Vector2 doorExitWorld = layout.TileToWorld(b.position.x + 1, b.position.y - 1);
                dropOff.doorOffset = doorExitWorld - (Vector2)center;
            }

            // Dairy deur animatie
            if (b.type == BuildingType.Dairy)
            {
                visual.AddComponent<DairyAnimator>();
                var dairyDrop = root.AddComponent<DropOffNode>();
                dairyDrop.dropOffType = DropOffNode.DropOffType.Dairy;
                // Door exit = SW tile (col-1, row)
                Vector2 dairyExit = layout.TileToWorld(b.position.x - 1, b.position.y);
                dairyDrop.doorOffset = dairyExit - (Vector2)center;
            }

            // ProductionNode op WheatField en Cow
            if (b.type == BuildingType.WheatField)
            {
                var node = root.AddComponent<ProductionNode>();
                node.nodeType = ProductionNode.NodeType.WheatField;
                node.visualSpriteRenderer = visual.GetComponent<SpriteRenderer>();
                // Voorkant van het iso-gebouw = offset naar onderen (dichtstbij camera)
                // Voor een wxh footprint: y-offset = (w+h)/2 * isoHalfH
                node.workOffset = new Vector2(0f, -(b.size.x + b.size.y) * 0.5f * layout.isoHalfH);
            }
            if (b.type == BuildingType.Cow)
            {
                var node = root.AddComponent<ProductionNode>();
                node.nodeType = ProductionNode.NodeType.Cow;
                node.workOffset = new Vector2(0f, -(b.size.x + b.size.y) * 0.5f * layout.isoHalfH);

                // Koe-sprite beweegt los als dier (geen pen aanwezig)
                var cowGo = new GameObject("CowSprite");
                cowGo.transform.SetParent(root.transform);
                cowGo.transform.localPosition = Vector3.zero;
                var csr = cowGo.AddComponent<SpriteRenderer>();
                var csp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/cow_se");
                if (csp != null)
                {
                    csr.sprite = csp;
                    float cScale = Mathf.Min(size.x / csp.bounds.size.x, size.y / csp.bounds.size.y) * 0.175f;
                    cowGo.transform.localScale = new Vector3(cScale, cScale, 1f);
                }
                csr.sortingOrder = bSort + 1;
                var anim = cowGo.AddComponent<CowAnimator>();
                anim.wanderBounds = new Vector2(size.x * 0.35f, size.y * 0.35f);
            }
        }
    }

    // ---- Post-load road patch ----

    /// <summary>
    /// Applies flipY=true to the door-exit road sprites of any pre-placed House buildings
    /// loaded from the map layout asset.  BuildManager does this for runtime-placed houses;
    /// this method handles the map-load path where SpawnRoadToNearestRoad is never called.
    /// </summary>
    private void PatchDoorExitRoads()
    {
        if (layout.buildings == null) return;
        var terrainParent = transform.Find("Terrain");
        if (terrainParent == null) return;

        foreach (var b in layout.buildings)
        {
            if (b.type != BuildingType.House) continue;

            // House door exit: SW step = (col - 1, row)
            int exitCol = b.position.x - 1;
            int exitRow = b.position.y;

            var rt = terrainParent.Find($"Road_{exitCol}_{exitRow}");
            if (rt  != null && rt .TryGetComponent<SpriteRenderer>(out var sr )) { sr .flipY = true; }
            var rtr = terrainParent.Find($"RoadR_{exitCol}_{exitRow}");
            if (rtr != null && rtr.TryGetComponent<SpriteRenderer>(out var rsr)) { rsr.flipY = true; }
        }

        // Dairy door exit: SW step = (col - 1, row) — column-axis (NE-SW), needs flipY=true
        foreach (var b in layout.buildings)
        {
            if (b.type != BuildingType.Dairy) continue;

            int exitCol = b.position.x - 1;
            int exitRow = b.position.y;

            var rt = terrainParent.Find($"Road_{exitCol}_{exitRow}");
            if (rt  != null && rt .TryGetComponent<SpriteRenderer>(out var sr )) sr .flipY = true;
            var rtr = terrainParent.Find($"RoadR_{exitCol}_{exitRow}");
            if (rtr != null && rtr.TryGetComponent<SpriteRenderer>(out var rsr)) rsr.flipY = true;
        }
    }

    // ---- Placeholder factory ----
    private GameObject MakePlaceholder(Color color, Vector3 pos, Vector2 size, Transform parent, int sortOrder = 0)
    {
        var go = new GameObject();
        go.transform.SetParent(parent);
        go.transform.position = pos;
        go.transform.localScale = new Vector3(size.x * 0.95f, size.y * 0.95f, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = GetSquareSprite();
        sr.color  = color;
        sr.sortingOrder = sortOrder;
        return go;
    }

    /// Sprite geschaald met fill + 1% overscale om gaps te voorkomen (voor terrein tiles).
    private GameObject MakeSpriteFill(string resourceName, Vector3 pos, Vector2 size, Transform parent, int sortOrder = 1)
    {
        var sp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/{resourceName}");
        if (sp == null) return null;

        var go = new GameObject(resourceName);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        // Schaal sprite zodat hij past binnen het footprint, aspect ratio behouden
        float sprW = sp.bounds.size.x;
        float sprH = sp.bounds.size.y;
        float scaleX = size.x / sprW;
        float scaleY = size.y / sprH;
        float scale  = Mathf.Max(scaleX, scaleY); // fill: geen gaps
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = sortOrder;
        return go;
    }

    private GameObject MakeSprite(string resourceName, Vector3 pos, Vector2 size, Transform parent, int sortOrder = 1)
    {
        var sp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/{resourceName}");
        if (sp == null) return null;

        var go = new GameObject(resourceName);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        float sprW = sp.bounds.size.x;
        float sprH = sp.bounds.size.y;
        float scaleX = size.x / sprW;
        float scaleY = size.y / sprH;
        float scale = Mathf.Min(scaleX, scaleY);
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr2 = go.AddComponent<SpriteRenderer>();
        sr2.sprite = sp;
        sr2.sortingOrder = sortOrder;
        return go;
    }

    private static Sprite _squareSprite;
    private static Sprite GetSquareSprite()
    {
        if (_squareSprite != null) return _squareSprite;
        var tex = new Texture2D(1, 1);
        tex.SetPixel(0, 0, Color.white);
        tex.Apply();
        _squareSprite = Sprite.Create(tex, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _squareSprite;
    }

    // ---- Resource names ----
    private static string TileResourceName(TileType t)
    {
        bool isWinter = DayNightCycle.Instance != null
                     && DayNightCycle.Instance.CurrentSeason == Season.Winter;
        return t switch
        {
            TileType.Grass    => "Terrain/Grass",
            TileType.Road     => "Terrain/roads",
            TileType.FarmPlot => "crops",
            TileType.Rock     => "Terrain/rock01",
            TileType.Wood     => "logs",
            TileType.Water    => "Terrain/Water",
            TileType.Sand     => "Terrain/Sand",
            TileType.Tree1    => isWinter ? "Terrain/tree05" : "Terrain/tree01",
            TileType.Tree2    => isWinter ? "Terrain/tree04" : "Terrain/tree02",
            TileType.Tree10   => isWinter ? "Terrain/tree11" : "Terrain/tree10",
            _                 => null,
        };
    }

    // Returns (summerPath, winterPath) for tree tiles, or (null, null) for non-tree tiles.
    private static (string summer, string winter) SeasonalPaths(TileType t) => t switch
    {
        TileType.Tree1  => ("Terrain/tree01", "Terrain/tree05"),
        TileType.Tree2  => ("Terrain/tree02", "Terrain/tree04"),
        TileType.Tree10 => ("Terrain/tree10", "Terrain/tree11"),
        _               => (null, null),
    };

    // ---- Placeholder colors ----
    private static Color TileColor(TileType t) => t switch
    {
        TileType.Grass    => new Color(0.4f, 0.7f, 0.3f),
        TileType.Road     => new Color(0.7f, 0.6f, 0.4f),
        TileType.FarmPlot => new Color(0.6f, 0.4f, 0.2f),
        TileType.Rock     => new Color(0.5f, 0.5f, 0.5f),
        TileType.Wood     => new Color(0.4f, 0.25f, 0.1f),
        TileType.Water    => new Color(0.25f, 0.35f, 0.75f),
        TileType.Sand     => new Color(0.80f, 0.70f, 0.45f),
        TileType.Tree1    => new Color(0.2f, 0.5f, 0.15f),
        TileType.Tree2    => new Color(0.2f, 0.5f, 0.15f),
        TileType.Tree10   => new Color(0.2f, 0.5f, 0.15f),
        _                 => new Color(0.4f, 0.7f, 0.3f),
    };

    private static Color BuildingColor(BuildingType t) => t switch
    {
        BuildingType.Mill        => new Color(0.8f, 0.6f, 0.1f),
        BuildingType.Cow         => new Color(0.9f, 0.8f, 0.5f),
        BuildingType.Campfire    => new Color(1.0f, 0.4f, 0.0f),
        BuildingType.Rockpile    => new Color(0.55f, 0.55f, 0.55f),
        BuildingType.Woodpile    => new Color(0.45f, 0.28f, 0.1f),
        BuildingType.Dairy       => new Color(0.9f, 0.9f, 0.9f),
        BuildingType.ChickenCoop => new Color(1.0f, 0.9f, 0.5f),
        BuildingType.House       => new Color(0.6f, 0.4f, 0.8f),
        BuildingType.WheatField  => new Color(0.9f, 0.8f, 0.2f),
        BuildingType.Toolshed    => new Color(0.5f, 0.35f, 0.15f),
        _                        => Color.magenta,
    };

    // ---- Helpers ----
    private GameObject TilePrefab(TileType t) => t switch
    {
        TileType.Grass    => grassPrefab,
        TileType.Road     => roadPrefab,
        TileType.FarmPlot => farmPlotPrefab,
        TileType.Rock     => rockPrefab,
        TileType.Wood     => woodPrefab,
        TileType.Water    => waterPrefab,
        TileType.Tree1    => null,
        TileType.Tree2    => null,
        TileType.Tree10   => null,
        _                 => grassPrefab,
    };

    private GameObject BuildingPrefab(BuildingType t) => t switch
    {
        BuildingType.Mill        => millPrefab,
        BuildingType.Cow         => cowPrefab,
        BuildingType.Campfire    => campfirePrefab,
        BuildingType.Rockpile    => rockpilePrefab,
        BuildingType.Woodpile    => woodpilePrefab,
        BuildingType.Dairy       => dairyPrefab,
        BuildingType.ChickenCoop => chickenCoopPrefab,
        BuildingType.House       => housePrefab,
        _                        => null,
    };
}
