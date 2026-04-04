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

        // Campfire staat op iso-grid (13,11) → world ≈ (1.0, 6.1)
        // Bumpkins spawnen iets voor het kampvuur (lagere y = dichter bij camera)
        var male = SpawnBumpkin("Bumpkin_Male",   new Vector3(2.1f, 17.1f, 0f), BumpkinController.BumpkinType.Male,   new Color(0.3f, 0.6f, 1f));
                   SpawnBumpkin("Bumpkin_Female", new Vector3(3.9f, 16.8f, 0f), BumpkinController.BumpkinType.Female, new Color(1f, 0.6f, 0.8f));

        // Pre-select male bumpkin
        SelectionManager.Instance?.Select(male);
    }

    private BumpkinController SpawnBumpkin(string goName, Vector3 pos, BumpkinController.BumpkinType type, Color color)
    {
        var go = new GameObject(goName);
        go.transform.position = pos;

        // Visual — probeer echte sprite, anders placeholder kleur
        var sr = go.AddComponent<SpriteRenderer>();
        var spriteName = type == BumpkinController.BumpkinType.Male ? "Units/m_still" : "Units/f_still";
        var sp = Resources.Load<Sprite>($"Sprites/{spriteName}");
        sr.sprite       = sp != null ? sp : MakeSquareSprite();
        sr.color        = sp != null ? Color.white : color;
        sr.sortingOrder = 10;
        go.transform.localScale = new Vector3(3.0f, 3.0f, 1f);

        // Collider for click detection
        var col = go.AddComponent<CircleCollider2D>();
        col.radius = 0.3f;

        // Logic
        var bc = go.AddComponent<BumpkinController>();
        bc.bumpkinType = type;

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
