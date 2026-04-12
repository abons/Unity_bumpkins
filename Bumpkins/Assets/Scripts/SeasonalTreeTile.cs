using UnityEngine;

/// <summary>
/// Attached to tree/grass tile GameObjects. Swaps the SpriteRenderer sprite
/// between the summer and winter variant when the season changes.
/// Uses the Bumpkins/SpriteAdditiveTint shader for Fall so warm colour can be
/// added on top of the deep-green sprite (multiplicative tint alone only darkens).
/// </summary>
public class SeasonalTreeTile : MonoBehaviour
{
    [System.NonSerialized] public string SummerResourcePath;
    [System.NonSerialized] public string WinterResourcePath;

    /// <summary>When true, scale uses Mathf.Max (fill). When false, Mathf.Min (fit).</summary>
    [System.NonSerialized] public bool FillMode = false;

    /// <summary>Tile size in world units — used to recompute scale on sprite swap.</summary>
    [System.NonSerialized] public Vector2 TileSize;

    /// <summary>
    /// Additive colour added on top of the sprite in Fall.
    /// RGB = the colour to add; A = intensity (0 = none, 1 = full add).
    /// Leave alpha at 0 to disable fall tinting.
    /// </summary>
    [System.NonSerialized] public Color FallAddColor = Color.clear;

    SpriteRenderer  _sr;
    Material        _additiveMat;   // instance of SpriteAdditiveTint shader
    Material        _defaultMat;    // original material
    Season          _lastSeason = (Season)(-1);

    static readonly int AddColorProp = Shader.PropertyToID("_AddColor");

    void Awake()
    {
        _sr         = GetComponent<SpriteRenderer>();
        _defaultMat = _sr != null ? _sr.sharedMaterial : null;
    }

    void OnDestroy()
    {
        if (_additiveMat != null) Destroy(_additiveMat);
    }

    void Update()
    {
        var cycle = DayNightCycle.Instance;
        if (cycle == null) return;

        var season = cycle.CurrentSeason;
        if (season == _lastSeason) return;

        _lastSeason = season;
        RefreshSprite(season);
    }

    void RefreshSprite(Season season)
    {
        if (_sr == null) return;

        var path = season == Season.Winter ? WinterResourcePath : SummerResourcePath;
        var sp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/{path}");
        if (sp != null) _sr.sprite = sp;

        // Apply additive fall tint via shader; restore default material otherwise
        if (season == Season.Fall && FallAddColor.a > 0f)
        {
            if (_additiveMat == null)
            {
                var shader = Shader.Find("Bumpkins/SpriteAdditiveTint");
                if (shader != null)
                {
                    _additiveMat = new Material(shader);
                    if (_defaultMat != null)
                        _additiveMat.mainTexture = _defaultMat.mainTexture;
                }
            }
            if (_additiveMat != null)
            {
                _additiveMat.SetColor(AddColorProp, FallAddColor);
                _sr.material = _additiveMat;
            }
        }
        else
        {
            _sr.color = Color.white;
            if (_defaultMat != null) _sr.material = _defaultMat;
        }

        // Recompute scale if tile size is known
        if (sp != null && TileSize.x > 0 && TileSize.y > 0)
        {
            float sprW = sp.bounds.size.x;
            float sprH = sp.bounds.size.y;
            float scaleX = TileSize.x / sprW;
            float scaleY = TileSize.y / sprH;
            float scale  = FillMode ? Mathf.Max(scaleX, scaleY) : Mathf.Min(scaleX, scaleY);
            transform.localScale = new Vector3(scale, scale, 1f);
        }
    }

    /// <summary>Called at runtime by the debug window to preview a new fall colour immediately.</summary>
    public void PreviewFallColor(Color c)
    {
        FallAddColor = c;
        if (DayNightCycle.Instance != null && DayNightCycle.Instance.CurrentSeason == Season.Fall)
        {
            _lastSeason = (Season)(-1); // force refresh
            RefreshSprite(Season.Fall);
        }
    }
}
