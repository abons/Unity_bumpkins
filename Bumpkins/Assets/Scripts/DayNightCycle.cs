using UnityEngine;

public enum Season { Spring, Summer, Fall, Winter }

/// <summary>
/// Day/night cycle via GL full-screen quad (no Canvas, no gizmo outlines).
///
/// Setup: add this component to any persistent GameObject — no other setup needed.
///
/// Phases (looping):
///   [Day] ─── dusk blend ──▶ [Night] ─── dawn blend ──▶ [Day] …
///
/// Other systems can read IsNight, NightBlend, or CurrentSeason.
/// </summary>
public class DayNightCycle : MonoBehaviour
{
    public static DayNightCycle Instance { get; private set; }

    Material _glMaterial;
    Color    _currentOverlay = Color.clear;

    float _cycleTime;

    /// <summary>True while the cycle is closer to night than to day.</summary>
    public bool IsNight { get; private set; }

    /// <summary>Smooth blend: 0 = full day, 1 = full night.</summary>
    public float NightBlend { get; private set; }

    /// <summary>Current season. Advances every completed day cycle.</summary>
    public Season CurrentSeason { get; private set; } = Season.Spring;

    int _completedCycles = 0;

    // ── Debug hooks (set by DayNightDebugWindow) ────────────────────────────
    [System.NonSerialized] public float DebugSpeedMultiplier = 1f;
    [System.NonSerialized] public bool  DebugPaused          = false;

    /// <summary>Expose internal time for saving.</summary>
    public float SavedCycleTime        => _cycleTime;
    public int   SavedCompletedCycles  => _completedCycles;

    /// <summary>Restore cycle time and season from a save file.</summary>
    public void LoadState(float cycleTime, int completedCycles, Season season)
    {
        _cycleTime       = cycleTime;
        _completedCycles = completedCycles;
        CurrentSeason    = season;
    }

    public void DebugJumpToDay()   { var cfg = GameManager.Instance?.config; if (cfg != null) _cycleTime = 0f; }
    public void DebugJumpToNight() { var cfg = GameManager.Instance?.config; if (cfg != null) _cycleTime = cfg.dayDuration; }
    public void DebugNextSeason()  { _completedCycles++; CurrentSeason = (Season)(_completedCycles % 4); _cycleTime = 0f; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        // Minimal unlit transparent material for GL drawing
        _glMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        _glMaterial.hideFlags = HideFlags.HideAndDontSave;
        _glMaterial.SetInt("_SrcBlend",  (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        _glMaterial.SetInt("_DstBlend",  (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        _glMaterial.SetInt("_Cull",      (int)UnityEngine.Rendering.CullMode.Off);
        _glMaterial.SetInt("_ZWrite",    0);
        _glMaterial.SetInt("_ZTest",     (int)UnityEngine.Rendering.CompareFunction.Always);
    }

    void OnDestroy()
    {
        if (_glMaterial != null) Destroy(_glMaterial);
    }

    void Update()
    {
        var cfg = GameManager.Instance != null ? GameManager.Instance.config : null;
        if (cfg == null) return;

        // Advance cycle time
        float total = cfg.dayDuration + cfg.nightDuration;
        if (!DebugPaused)
        {
            float prev = _cycleTime;
            _cycleTime += Time.deltaTime * DebugSpeedMultiplier;
            if (_cycleTime >= total)
            {
                _cycleTime -= total;
                _completedCycles++;
                CurrentSeason = (Season)(_completedCycles % 4);
            }
        }

        // Compute raw blend (0 = day, 1 = night)
        float dd        = Mathf.Max(0.01f, cfg.dawnDuskDuration);
        float duskStart = cfg.dayDuration - dd;
        float dawnStart = total - dd;

        float raw;
        if      (_cycleTime < duskStart)       raw = 0f;
        else if (_cycleTime < cfg.dayDuration) raw = (_cycleTime - duskStart) / dd;
        else if (_cycleTime < dawnStart)       raw = 1f;
        else                                   raw = (total - _cycleTime) / dd;

        NightBlend = Mathf.SmoothStep(0f, 1f, raw);
        IsNight    = raw >= 0.5f;

        Color nightTint = cfg.nightLightColor;
        nightTint.a     = NightBlend * (1f - cfg.nightLightIntensity);
        _currentOverlay = nightTint;
    }

    void OnPostRender()
    {
        DrawOverlay();
    }

    // Called when attached to a non-camera GameObject — find main camera
    void OnRenderObject()
    {
        if (Camera.current != Camera.main) return;
        DrawOverlay();
    }

    void DrawOverlay()
    {
        if (_glMaterial == null || _currentOverlay.a <= 0f) return;

        _glMaterial.SetPass(0);
        GL.PushMatrix();
        GL.LoadOrtho();
        GL.Begin(GL.QUADS);
        GL.Color(_currentOverlay);
        GL.Vertex3(0f, 0f, 0f);
        GL.Vertex3(0f, 1f, 0f);
        GL.Vertex3(1f, 1f, 0f);
        GL.Vertex3(1f, 0f, 0f);
        GL.End();
        GL.PopMatrix();
    }
}
