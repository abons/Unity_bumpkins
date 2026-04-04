using UnityEngine;

/// <summary>
/// Procedurele vuuranimatie op het kampvuur:
/// flames.png overlay + scale/kleur flicker.
/// </summary>
public class CampfireAnimator : MonoBehaviour
{
    [Header("Scale flicker")]
    public float baseScale    = 1.0f;
    public float flickerAmp   = 0.10f;
    public float flickerSpeed = 9f;

    [Header("Kleur puls")]
    public Color colorA = new Color(1.0f, 0.55f, 0.05f);
    public Color colorB = new Color(1.0f, 0.85f, 0.1f);
    public float colorSpeed = 6f;

    private SpriteRenderer _baseSr;
    private SpriteRenderer _flameSr;
    private Vector3        _baseLocalScale;
    private Vector3        _flameLocalScale;

    [Header("Flip animatie")]
    public float flipInterval = 0.4f;   // seconden tussen mogelijke flips

    private float _flipTimer;
    private float _offset1, _offset2, _offset3;

    void Start()
    {
        _baseSr         = GetComponent<SpriteRenderer>();
        _baseLocalScale = transform.localScale;

        _offset1 = Random.Range(0f, Mathf.PI * 2f);
        _offset2 = Random.Range(0f, Mathf.PI * 2f);
        _offset3 = Random.Range(0f, Mathf.PI * 2f);
        _flipTimer = flipInterval + Random.Range(0f, flipInterval);

        // Flames overlay als child
        var sp = Resources.Load<Sprite>("Sprites/Effects/Flames");
        if (sp != null)
        {
            var flameGo = new GameObject("Flames");
            flameGo.transform.SetParent(transform);
            flameGo.transform.localPosition = new Vector3(0f, 0f, 0f);

            _flameSr = flameGo.AddComponent<SpriteRenderer>();
            _flameSr.sprite = sp;
            _flameSr.sortingOrder = (_baseSr != null ? _baseSr.sortingOrder : 0) + 1;

            // Schaal flames zodat ze qua breedte passen op het campfire sprite
            float targetW = _baseLocalScale.x * 0.267f;
            float flameScale = targetW / sp.bounds.size.x;
            flameGo.transform.localScale = new Vector3(flameScale, flameScale, 1f);
            _flameLocalScale = flameGo.transform.localScale;
        }
    }

    void Update()
    {
        float t = Time.time;

        float flicker = Mathf.Sin(t * flickerSpeed        + _offset1) * 0.5f
                      + Mathf.Sin(t * flickerSpeed * 1.7f + _offset2) * 0.3f
                      + Mathf.Sin(t * flickerSpeed * 2.3f + _offset3) * 0.2f;

        float scaleF = baseScale + flicker * flickerAmp;
        float ct = (Mathf.Sin(t * colorSpeed + _offset1) + 1f) * 0.5f;
        var   col = Color.Lerp(colorA, colorB, ct);

        // Basis campfire sprite: subtiele tint
        if (_baseSr != null)
            _baseSr.color = Color.Lerp(Color.white, colorB, ct * 0.3f);

        // Flames overlay: flicker in scale + kleur + flipX
        if (_flameSr != null)
        {
            _flameSr.color = col;
            _flameSr.transform.localScale = new Vector3(
                _flameLocalScale.x * (scaleF - flickerAmp * 0.3f),
                _flameLocalScale.y * (scaleF + flickerAmp * 0.5f),
                1f);

            // Willekeurige flipX voor organisch vlameffect
            _flipTimer -= Time.deltaTime;
            if (_flipTimer <= 0f)
            {
                _flameSr.flipX = Random.value > 0.5f;
                _flipTimer = flipInterval + Random.Range(-0.1f, 0.2f);
            }
        }
    }
}
