using UnityEngine;

/// <summary>
/// Kip-animatie: loopt heen en weer, legt na eggInterval een ei.
/// Het ei blijft zichtbaar totdat CollectEgg() wordt aangeroepen (door bumpkin/game).
/// </summary>
public class ChickenAnimator : MonoBehaviour
{
    [Header("Sprites")]
    private Sprite _sprChicken;
    private Sprite _sprEgg;

    [Header("Walk timing")]
    public float walkInterval = 1.2f;
    public float flipDuration = 0.15f;

    [Header("Bob")]
    public float bobAmplitude = 0.02f;
    public float bobSpeed     = 4f;

    [Header("Egg production")]
    public float eggInterval  = 10f;  // seconden tot eerste/volgend ei

    private SpriteRenderer _sr;
    private SpriteRenderer _eggSr;      // apart ei-object naast de kip
    private Vector3        _basePos;
    private float          _walkTimer;
    private float          _flipTimer;
    private float          _eggTimer;
    private bool           _facingRight = true;
    private bool           _hasEgg      = false;

    public bool HasEgg => _hasEgg;

    void Start()
    {
        _sr      = GetComponent<SpriteRenderer>();
        _basePos = transform.localPosition;

        // Desync meerdere kippen
        _walkTimer = Random.Range(0f, walkInterval);
        _eggTimer  = eggInterval + Random.Range(0f, 3f);

        _sprChicken = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/Chicken");
        _sprEgg     = Resources.Load<Sprite>($"{GraphicsQuality.SpritePath}/Units/ChickenEgg");

        if (_sr != null && _sprChicken != null)
            _sr.sprite = _sprChicken;

        // Maak apart ei-GameObject naast de kip (begint verborgen)
        // Sibling van de kip (child van parent) zodat het niet mee-bobt
        if (_sprEgg != null)
        {
            var eggGo = new GameObject("Egg");
            eggGo.transform.SetParent(transform.parent);
            eggGo.transform.localScale = transform.localScale * 0.25f;
            _eggSr = eggGo.AddComponent<SpriteRenderer>();
            _eggSr.sprite       = _sprEgg;
            _eggSr.sortingOrder = _sr != null ? _sr.sortingOrder + 1 : 1;
            _eggSr.enabled      = false;
        }
    }

    void Update()
    {
        // Bob op en neer
        float bob = Mathf.Sin(Time.time * bobSpeed) * bobAmplitude;
        transform.localPosition = _basePos + new Vector3(0f, bob, 0f);

        // Ei volgt de basispos van de kip (zonder bob)
        if (_eggSr != null)
        {
            Vector3 eggBase = transform.parent != null
                ? transform.parent.TransformPoint(_basePos + new Vector3(0.1f, 0f, 0f))
                : _basePos + new Vector3(0.1f, 0f, 0f);
            _eggSr.transform.position = eggBase;
        }

        // Flip timer
        if (_flipTimer > 0f)
        {
            _flipTimer -= Time.deltaTime;
            if (_flipTimer <= 0f)
                _sr.flipX = _facingRight;
        }

        // Ei timer — alleen aftellen als er nog geen ei ligt
        if (!_hasEgg)
        {
            _eggTimer -= Time.deltaTime;
            if (_eggTimer <= 0f)
                LayEgg();
        }

        // Walk cyclus — kip beweegt niet als er een ei is
        if (!_hasEgg)
        {
            _walkTimer -= Time.deltaTime;
            if (_walkTimer <= 0f)
            {
                _walkTimer   = walkInterval + Random.Range(-0.3f, 0.3f);
                _facingRight = !_facingRight;
                _sr.flipX    = !_facingRight;
                _flipTimer   = flipDuration;
            }
        }
    }

    private void LayEgg()
    {
        _hasEgg = true;
        if (_eggSr != null) _eggSr.enabled = true;   // ei verschijnt naast kip
        Debug.Log($"[ChickenAnimator] {transform.parent?.name} legt een ei!");
    }

    /// <summary>Roep aan vanuit bumpkin/game logic om het ei te verzamelen.</summary>
    public void CollectEgg()
    {
        if (!_hasEgg) return;
        _hasEgg   = false;
        _eggTimer = eggInterval;
        if (_eggSr != null) _eggSr.enabled = false;  // ei verdwijnt
        Debug.Log($"[ChickenAnimator] Ei verzameld van {transform.parent?.name}");
    }
}
