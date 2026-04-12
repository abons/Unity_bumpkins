using UnityEngine;

/// <summary>
/// Attached to tree tile GameObjects. Swaps the SpriteRenderer sprite
/// between the summer and winter variant when the season changes.
/// </summary>
public class SeasonalTreeTile : MonoBehaviour
{
    [System.NonSerialized] public string SummerResourcePath;
    [System.NonSerialized] public string WinterResourcePath;

    SpriteRenderer _sr;
    Season         _lastSeason = (Season)(-1); // force refresh on first Update

    void Awake()
    {
        _sr = GetComponent<SpriteRenderer>();
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
        var path = season == Season.Winter ? WinterResourcePath : SummerResourcePath;
        var sp = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/{path}");
        if (sp != null && _sr != null)
            _sr.sprite = sp;
    }
}
