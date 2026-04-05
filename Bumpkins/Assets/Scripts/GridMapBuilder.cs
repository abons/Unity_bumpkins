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
    public GameObject townHallPrefab;
    public GameObject millPrefab;
    public GameObject farmPrefab;
    public GameObject cowPrefab;
    public GameObject campfirePrefab;
    public GameObject rockpilePrefab;
    public GameObject woodpilePrefab;
    public GameObject bakeryPrefab;
    public GameObject dairyPrefab;
    public GameObject chickenCoopPrefab;
    public GameObject housePrefab;

    void Start()
    {
        if (layout == null) { Debug.LogError("[GridMapBuilder] No layout assigned!"); return; }
        BuildTerrain();
        BuildBuildings();
        SpawnWolf();
        SpawnWasp();
        SpawnBat();
        SpawnOgre();
        SpawnZombie();
        SpawnGiant();
        SpawnBloodWasp();
    }

    private void SpawnBloodWasp()
    {
        var go = new GameObject("BloodWasp");
        go.transform.position = layout.TileToWorld(44, 33);
        var sr = go.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/bloodwsp");
        if (sp != null) { sr.sprite = sp; go.transform.localScale = new Vector3(3f, 3f, 1f); }
        sr.sortingOrder = 10;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<BloodWaspController>();
        Debug.Log("[BloodWasp] Blood Wasp gespawnd op de kaart");
    }

    private void SpawnBat()
    {
        var go = new GameObject("Bat");
        go.transform.position = layout.TileToWorld(38, 2);
        var sr = go.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/bat");
        if (sp != null) { sr.sprite = sp; go.transform.localScale = new Vector3(3f, 3f, 1f); }
        sr.sortingOrder = 10;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<BatController>();
        Debug.Log("[Bat] Bat gespawnd op de kaart");
    }

    private void SpawnOgre()
    {
        var go = new GameObject("Ogre");
        go.transform.position = layout.TileToWorld(45, 30);
        var sr = go.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/ogrestil");
        if (sp != null) { sr.sprite = sp; go.transform.localScale = new Vector3(3f, 3f, 1f); }
        sr.sortingOrder = 10;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<OgreController>();
        Debug.Log("[Ogre] Ogre gespawnd op de kaart");
    }

    private void SpawnZombie()
    {
        var go = new GameObject("Zombie");
        go.transform.position = layout.TileToWorld(2, 30);
        var sr = go.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/zombie");
        if (sp != null) { sr.sprite = sp; go.transform.localScale = new Vector3(3f, 3f, 1f); }
        sr.sortingOrder = 10;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<ZombieController>();
        Debug.Log("[Zombie] Zombie gespawnd op de kaart");
    }

    private void SpawnGiant()
    {
        var go = new GameObject("Giant");
        go.transform.position = layout.TileToWorld(24, 2);
        var sr = go.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/gianstil");
        if (sp != null) { sr.sprite = sp; go.transform.localScale = new Vector3(3f, 3f, 1f); }
        sr.sortingOrder = 10;
        go.AddComponent<BoxCollider2D>();
        go.AddComponent<GiantController>();
        Debug.Log("[Giant] Giant gespawnd op de kaart");
    }

    private void SpawnWasp()
    {
        var waspGo = new GameObject("Wasp");
        waspGo.transform.position = layout.TileToWorld(10, 2);

        var sr = waspGo.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/wasp");
        if (sp != null)
        {
            sr.sprite = sp;
            float scale = 3f;
            waspGo.transform.localScale = new Vector3(scale, scale, 1f);
        }
        sr.sortingOrder = 10;

        waspGo.AddComponent<BoxCollider2D>();
        waspGo.AddComponent<WaspController>();
        Debug.Log("[Wasp] Wasp gespawnd op de kaart");
    }

    private void SpawnWolf()
    {
        var wolfGo = new GameObject("Wolf");
        wolfGo.transform.position = layout.TileToWorld(2, 18);

        var sr = wolfGo.AddComponent<SpriteRenderer>();
        var sp = Resources.Load<Sprite>("Sprites/Animals/wolfstil");
        if (sp != null)
        {
            sr.sprite = sp;
            float scale = 3f;
            wolfGo.transform.localScale = new Vector3(scale, scale, 1f);
        }
        sr.sortingOrder = 10;

        wolfGo.AddComponent<BoxCollider2D>();
        wolfGo.AddComponent<WolfController>();
        Debug.Log("[Wolf] Wolf gespawnd op de kaart");
    }

    // ---- Terrain ----
    private void BuildTerrain()
    {
        var parent = new GameObject("Terrain").transform;
        parent.SetParent(transform);

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
                    // Skip all 4 grid corners — no single rotation fits both edges
                    bool isCorner = (row == 0 || row == layout.rows - 1) &&
                                    (col == 0 || col == layout.cols - 1);
                    if (isCorner) { continue; }

                    float isoAngle = Mathf.Atan2(layout.isoHalfH, layout.isoHalfW) * Mathf.Rad2Deg;
                    float zRot = row == 0               ? -153f
                               : row == layout.rows - 1 ?   28f
                               : col == 0               ? 180f - isoAngle
                               :                              - isoAngle;

                    float W = layout.isoHalfW, H = layout.isoHalfH;
                    Vector3 backOffset = col == 0               ? new Vector3(-W * (2f/3f),  H * Mathf.Sqrt(2f), 0f)
                                       : col == layout.cols - 1 ? new Vector3( W * (2f/3f), -H * Mathf.Sqrt(2f), 0f)
                                       : row == 0               ? new Vector3( W * (2f/3f),  H * Mathf.Sqrt(2f), 0f)
                                       :                          new Vector3(-W * (2f/3f), -H * Mathf.Sqrt(2f), 0f);

                    // Back layer: sandhill +180° shifted outward — water side covers the grass zone
                    var waterBack = MakeSpriteFill("Terrain/sandhill", center + backOffset, tileVec, parent, sOrder + 1)
                                 ?? MakePlaceholder(TileColor(TileType.Water), center + backOffset, tileVec, parent, sOrder + 1);
                    waterBack.transform.rotation = Quaternion.Euler(0f, 0f, zRot + 180f);
                    waterBack.name = $"Tile_{col}_{row}_water_back";

                    // Front layer: sandhill at correct rotation — shore transition on top
                    var waterGo = MakeSpriteFill("Terrain/sandhill", center, tileVec, parent, sOrder + 2)
                               ?? MakePlaceholder(TileColor(TileType.Water), center, tileVec, parent, sOrder + 2);
                    waterGo.transform.rotation = Quaternion.Euler(0f, 0f, zRot);
                    waterGo.name = $"Tile_{col}_{row}_{tile}";
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
                    }
                    go.name = $"Tile_{col}_{row}_{tile}";
                }
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
            visual.transform.localPosition = Vector3.zero;

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
                var spritePath = b.type == BuildingType.Bakery
                    ? "Sprites/Buildings/Mill"
                    : b.type == BuildingType.Woodpile
                    ? "Sprites/Buildings/Logpile"
                    : $"Sprites/Buildings/{b.type}";
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
                var csp = Resources.Load<Sprite>("Sprites/Units/Chicken");
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
                tag.isHouse = (b.type == BuildingType.House);
            }

            // Molen wieken + deur animatie + drop-off node
            if (b.type == BuildingType.Mill)
            {
                visual.AddComponent<MillAnimator>();
                var dropOff = root.AddComponent<DropOffNode>();
                dropOff.dropOffType = DropOffNode.DropOffType.Bakery;
            }

            // ProductionNode op WheatField en Cow
            if (b.type == BuildingType.Farm)
            {
                var dropOff = root.AddComponent<DropOffNode>();
                dropOff.dropOffType = DropOffNode.DropOffType.Dairy;
            }
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
                var csp = Resources.Load<Sprite>("Sprites/Units/cow_se");
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
        var sp = Resources.Load<Sprite>($"Sprites/{resourceName}");
        if (sp == null) return null;

        var go = new GameObject(resourceName);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        float sprW   = sp.bounds.size.x;
        float sprH   = sp.bounds.size.y;
        float scaleX = size.x / sprW;
        float scaleY = size.y / sprH;
        float scale  = Mathf.Max(scaleX, scaleY) * 1.005f; // fill + overscale
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite       = sp;
        sr.sortingOrder = sortOrder;
        return go;
    }

    private GameObject MakeSprite(string resourceName, Vector3 pos, Vector2 size, Transform parent, int sortOrder = 1)
    {
        var sp = Resources.Load<Sprite>($"Sprites/{resourceName}");
        if (sp == null) return null;

        var go = new GameObject(resourceName);
        go.transform.SetParent(parent);
        go.transform.position = pos;

        // Schaal sprite zodat hij past binnen het footprint, aspect ratio behouden
        float sprW = sp.bounds.size.x;
        float sprH = sp.bounds.size.y;
        float scaleX = size.x / sprW;
        float scaleY = size.y / sprH;
        float scale  = Mathf.Min(scaleX, scaleY); // fit, niet fill
        go.transform.localScale = new Vector3(scale, scale, 1f);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = sp;
        sr.sortingOrder = sortOrder;
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
    private static string TileResourceName(TileType t) => t switch
    {
        TileType.Grass    => "Terrain/Grass",
        TileType.Road     => "Terrain/roads",
        TileType.FarmPlot => "crops",
        TileType.Rock     => "Terrain/rock01",
        TileType.Wood     => "logs",
        TileType.Water    => "Terrain/sandhill",
        _                 => null,
    };

    // ---- Placeholder colors ----
    private static Color TileColor(TileType t) => t switch
    {
        TileType.Grass    => new Color(0.4f, 0.7f, 0.3f),
        TileType.Road     => new Color(0.7f, 0.6f, 0.4f),
        TileType.FarmPlot => new Color(0.6f, 0.4f, 0.2f),
        TileType.Rock     => new Color(0.5f, 0.5f, 0.5f),
        TileType.Wood     => new Color(0.4f, 0.25f, 0.1f),
        TileType.Water    => new Color(0.2f, 0.5f, 0.9f),
        _                 => new Color(0.4f, 0.7f, 0.3f),
    };

    private static Color BuildingColor(BuildingType t) => t switch
    {
        BuildingType.TownHall    => new Color(0.8f, 0.2f, 0.2f),
        BuildingType.Mill        => new Color(0.8f, 0.6f, 0.1f),
        BuildingType.Farm        => new Color(0.5f, 0.8f, 0.2f),
        BuildingType.Cow         => new Color(0.9f, 0.8f, 0.5f),
        BuildingType.Campfire    => new Color(1.0f, 0.4f, 0.0f),
        BuildingType.Rockpile    => new Color(0.55f, 0.55f, 0.55f),
        BuildingType.Woodpile    => new Color(0.45f, 0.28f, 0.1f),
        BuildingType.Bakery      => new Color(0.9f, 0.7f, 0.3f),
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
        _                 => grassPrefab,
    };

    private GameObject BuildingPrefab(BuildingType t) => t switch
    {
        BuildingType.TownHall    => townHallPrefab,
        BuildingType.Mill        => millPrefab,
        BuildingType.Farm        => farmPrefab,
        BuildingType.Cow         => cowPrefab,
        BuildingType.Campfire    => campfirePrefab,
        BuildingType.Rockpile    => rockpilePrefab,
        BuildingType.Woodpile    => woodpilePrefab,
        BuildingType.Bakery      => bakeryPrefab,
        BuildingType.Dairy       => dairyPrefab,
        BuildingType.ChickenCoop => chickenCoopPrefab,
        BuildingType.House       => housePrefab,
        _                        => null,
    };
}
