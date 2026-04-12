using UnityEngine;

/// <summary>
/// Tijdelijk test-script: spawnt automatisch 2 bumpkins (male + female) als placeholders.
/// Attach to any GameObject in de scene (bijv. een lege "GameSetup").
/// Verwijder dit script later als je echte prefabs hebt.
/// </summary>
public class TestBumpkinSetup : MonoBehaviour
{
    void Awake()
    {
        // GameManager
        if (GameManager.Instance == null)
        {
            var gm = new GameObject("GameManager");
            gm.AddComponent<GameManager>();
        }

        // UIManager
        if (UIManager.Instance == null)
        {
            var ui = new GameObject("UIManager");
            ui.AddComponent<UIManager>();
        }

        // SelectionManager
        if (SelectionManager.Instance == null)
        {
            var sm = new GameObject("SelectionManager");
            sm.AddComponent<SelectionManager>();
        }

        // BuildManager
        if (BuildManager.Instance == null)
        {
            var bldMgr = new GameObject("BuildManager");
            bldMgr.AddComponent<BuildManager>();
        }
    }

    void Start()
    {
        var builder = FindFirstObjectByType<GridMapBuilder>();
        Vector3 campfireWorld = new Vector3(4.5f, 36.0f, 0f); // fallback
        if (builder?.layout != null && builder.layout.buildings != null)
        {
            foreach (var b in builder.layout.buildings)
            {
                if (b.type == BuildingType.Campfire)
                {
                    var wp = builder.layout.TileToWorld(b.position.x, b.position.y);
                    campfireWorld = new Vector3(wp.x, wp.y, 0f);
                    break;
                }
            }
        }

        float hw = builder?.layout?.isoHalfW ?? 0.5f;
        float hh = builder?.layout?.isoHalfH ?? 0.256f;

        // Apply map startGold if set
        var layout = builder?.layout;
        if (layout != null && layout.startGold > 0 && GameManager.Instance != null)
            GameManager.Instance.SetGold(layout.startGold);

        // Determine spawn list — use layout list or fall back to 1 male + 1 female
        BumpkinSpawnEntry[] spawnList;
        if (layout?.startingBumpkins != null && layout.startingBumpkins.Length > 0)
            spawnList = layout.startingBumpkins;
        else
            spawnList = new BumpkinSpawnEntry[]
            {
                new BumpkinSpawnEntry { type = BumpkinSpawnType.Male },
                new BumpkinSpawnEntry { type = BumpkinSpawnType.Female },
            };

        // Arrange in a small arc around the campfire
        BumpkinController firstMale = null;
        for (int i = 0; i < spawnList.Length; i++)
        {
            float angle = (spawnList.Length == 1) ? -Mathf.PI * 0.5f
                        : Mathf.PI + i * Mathf.PI / Mathf.Max(1, spawnList.Length - 1);
            float radius = hw * 2f;
            var pos = campfireWorld + new Vector3(Mathf.Cos(angle) * radius, Mathf.Sin(angle) * radius * 0.5f, 0f);

            bool isMale  = spawnList[i].type == BumpkinSpawnType.Male  || spawnList[i].type == BumpkinSpawnType.Boy;
            bool isChild = spawnList[i].type == BumpkinSpawnType.Boy   || spawnList[i].type == BumpkinSpawnType.Girl;
            var bType = isMale ? BumpkinController.BumpkinType.Male : BumpkinController.BumpkinType.Female;
            var color = isMale ? new Color(0.3f, 0.6f, 1f) : new Color(1f, 0.6f, 0.8f);
            string label = spawnList[i].type.ToString();

            var bc = SpawnBumpkin($"Bumpkin_{label}_{i}", pos, bType, color, isChild);
            if (firstMale == null && isMale && !isChild) firstMale = bc;
        }

        // Pre-select first adult male (or first bumpkin if none)
        SelectionManager.Instance?.Select(firstMale);

        // Apply save data if a load was requested (overwrites bumpkins/resources above)
        SaveSystem.ApplyPendingLoad();
    }

    private BumpkinController SpawnBumpkin(string goName, Vector3 pos, BumpkinController.BumpkinType type, Color color, bool isChild = false)
    {
        return SpawnBumpkinPublic(goName, pos, type, isChild);
    }

    public BumpkinController SpawnBumpkinPublic(string goName, Vector3 pos, BumpkinController.BumpkinType type, bool isChild = false)
    {
        var go = new GameObject(goName);
        go.transform.position = pos;

        // Visual — probeer echte sprite, anders placeholder kleur
        var sr = go.AddComponent<SpriteRenderer>();
        var spriteName = type == BumpkinController.BumpkinType.Male ? "Units/m_still" : "Units/f_still";
        var sp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/{spriteName}");
        sr.sprite       = sp != null ? sp : MakeSquareSprite();
        sr.color        = Color.white;
        sr.sortingOrder = 10;
        go.transform.localScale = isChild ? new Vector3(2f, 2f, 1f) : new Vector3(3f, 3f, 1f);

        // Collider for click detection
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;

        // Logic
        var bc = go.AddComponent<BumpkinController>();
        bc.bumpkinType = type;
        bc.isChild = isChild;

        go.AddComponent<BumpkinClick>();
        go.AddComponent<BumpkinAnimator>();

        Debug.Log($"[TestBumpkinSetup] Spawned {goName} at {pos}");
        return bc;
    }

    private static Sprite _sq;
    private static Sprite MakeSquareSprite()
    {
        if (_sq != null) return _sq;
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, Color.white);
        t.Apply();
        _sq = Sprite.Create(t, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f);
        return _sq;
    }
}
